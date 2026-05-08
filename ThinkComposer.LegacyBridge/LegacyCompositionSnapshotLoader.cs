using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using Instrumind.Common;
using Instrumind.Common.EntityBase;
using Instrumind.Common.Visualization;
using Instrumind.ThinkComposer.Composer;
using Instrumind.ThinkComposer.Core.Rendering;
using Instrumind.ThinkComposer.Definitor;
using Instrumind.ThinkComposer.MetaModel;
using Instrumind.ThinkComposer.Model;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionSnapshotLoader
{
    private static readonly Uri CompositionPartUri = new("/Composition.bin", UriKind.Relative);
    private static readonly Uri DomainPartUri = new("/Domain.bin", UriKind.Relative);

    public static CompositionViewSnapshot LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Legacy document path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Legacy document file was not found.", filePath);
        }

        var content = LoadPackageContent(filePath, out var packageTitle, out var packageId);

        if (content is Composition composition)
        {
            return FromComposition(PrepareComposition(composition), packageTitle);
        }

        if (content is Domain domain)
        {
            TryApplyModelFixes(domain);

            if (domain.OwnerComposition is null)
            {
                return new CompositionViewSnapshot(
                    ReadPackageId(packageId, domain.GlobalId),
                    packageTitle ?? domain.Name,
                    Array.Empty<CompositionNodeView>(),
                    Array.Empty<CompositionConnectorView>());
            }

            return FromComposition(PrepareComposition(domain.OwnerComposition), packageTitle ?? domain.Name);
        }

        throw new InvalidOperationException($"Unsupported legacy document content: {content.GetType().FullName}");
    }

    private static CompositionViewSnapshot FromComposition(Composition composition, string? packageTitle)
    {
        var snapshot = LegacyCompositionSnapshotMapper.FromComposition(composition);

        return string.IsNullOrWhiteSpace(packageTitle)
            ? snapshot
            : snapshot with { Title = packageTitle! };
    }

    private static Composition PrepareComposition(Composition composition)
    {
        composition.CompositionDefinitor.SetOwnerComposition(composition);
        TryApplyModelFixes(composition.CompositionDefinitor);
        return composition;
    }

    private static void TryApplyModelFixes(Domain domain)
    {
        try
        {
            ModelFixes.ApplyModelFixes(domain);
        }
        catch (NullReferenceException) when (domain.EditEngine is null)
        {
            // Read-only bridge has no active legacy edit engine to mark modified.
        }
    }

    private static object LoadPackageContent(string filePath, out string? packageTitle, out Guid? packageId)
    {
        using var package = Package.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        packageTitle = package.PackageProperties?.Title;
        packageId = Guid.TryParse(package.PackageProperties?.Identifier, out var id) ? id : null;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var partUri = extension switch
        {
            ".tcom" => CompositionPartUri,
            ".tdom" => DomainPartUri,
            _ => throw new InvalidOperationException($"Unsupported legacy document extension: {extension}")
        };

        var part = package.PartExists(partUri)
            ? package.GetPart(partUri)
            : package.GetParts().FirstOrDefault(candidate => candidate.Uri.ToString().EndsWith(".bin", StringComparison.OrdinalIgnoreCase));

        if (part is null)
        {
            throw new InvalidOperationException($"Legacy document package has no binary content part: {filePath}");
        }

        return BytesHandling.Deserialize<ISphereModel>(part.GetStream());
    }

    private static string ReadPackageId(Guid? packageId, Guid fallback)
    {
        return (packageId ?? fallback).ToString();
    }
}
