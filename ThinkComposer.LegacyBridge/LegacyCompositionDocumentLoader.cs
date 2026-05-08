using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using Instrumind.Common;
using Instrumind.Common.EntityBase;
using Instrumind.Common.Visualization;
using Instrumind.ThinkComposer.Composer;
using Instrumind.ThinkComposer.Core.Rendering;
using Instrumind.ThinkComposer.MetaModel;
using Instrumind.ThinkComposer.MetaModel.Configurations;
using Instrumind.ThinkComposer.MetaModel.GraphMetaModel;
using Instrumind.ThinkComposer.MetaModel.InformationMetaModel;
using Instrumind.ThinkComposer.MetaModel.VisualMetaModel;
using Instrumind.ThinkComposer.Model;
using Instrumind.ThinkComposer.Model.GraphModel;
using Instrumind.ThinkComposer.Model.InformationModel;
using Instrumind.ThinkComposer.Model.VisualModel;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionDocumentLoader
{
    private static readonly Uri CompositionPartUri = new("/Composition.bin", UriKind.Relative);
    private static readonly Uri DomainPartUri = new("/Domain.bin", UriKind.Relative);

    public static CompositionDocumentSnapshot LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Legacy document path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Legacy document file was not found.", filePath);
        }

        var packageBytes = File.ReadAllBytes(filePath);
        var content = LoadPackageContent(filePath, out var packageTitle, out var packageId);

        if (content is Composition composition)
        {
            return FromComposition(PrepareComposition(composition), packageTitle, packageId, filePath, packageBytes);
        }

        if (content is Domain domain)
        {
            TryApplyModelFixes(domain);

            if (domain.OwnerComposition is not null)
            {
                return FromComposition(PrepareComposition(domain.OwnerComposition), packageTitle ?? domain.Name, packageId, filePath, packageBytes);
            }

            return FromDomain(domain, packageTitle, packageId, filePath, packageBytes);
        }

        throw new InvalidOperationException($"Unsupported legacy document content: {content.GetType().FullName}");
    }

    private static CompositionDocumentSnapshot FromComposition(
        Composition composition,
        string? packageTitle,
        Guid? packageId,
        string filePath,
        byte[] packageBytes)
    {
        var viewSnapshot = LegacyCompositionSnapshotMapper.FromComposition(composition);
        if (!string.IsNullOrWhiteSpace(packageTitle))
        {
            viewSnapshot = viewSnapshot with { Title = packageTitle! };
        }

        var visualObjects = ReadViewObjects(composition.ActiveView ?? composition.RootView).ToArray();
        var symbolIdeas = visualObjects
            .OfType<VisualSymbol>()
            .GroupBy(symbol => symbol.GlobalId.ToString())
            .ToDictionary(group => group.Key, group => group.First().OwnerRepresentation.RepresentedIdea, StringComparer.Ordinal);

        var connectorRelationships = visualObjects
            .OfType<VisualConnector>()
            .GroupBy(connector => connector.GlobalId.ToString())
            .ToDictionary(group => group.Key, group => group.First().OwnerRelationshipRepresentation.RepresentedRelationship, StringComparer.Ordinal);

        var ideas = viewSnapshot.Nodes
            .Select(node => MapIdea(node, symbolIdeas.TryGetValue(node.Id, out var idea) ? idea : null))
            .ToArray();

        var relationships = viewSnapshot.Connectors
            .Select(connector => MapRelationship(
                connector,
                connectorRelationships.TryGetValue(connector.Id, out var relationship) ? relationship : null))
            .ToArray();

        return new CompositionDocumentSnapshot(
            Id: (packageId ?? composition.GlobalId).ToString(),
            Title: viewSnapshot.Title,
            Domain: MapDomain(composition.CompositionDefinitor),
            Ideas: ideas,
            Relationships: relationships,
            Views:
            [
                new CompositionViewLayerSnapshot(
                    Id: composition.ActiveView?.GlobalId.ToString() ?? "view.default",
                    Name: composition.ActiveView?.Name ?? viewSnapshot.Title,
                    Nodes: viewSnapshot.Nodes,
                    Connectors: viewSnapshot.Connectors)
            ],
            Extensions: BuildPackageExtensions(filePath, packageBytes));
    }

    private static CompositionDocumentSnapshot FromDomain(
        Domain domain,
        string? packageTitle,
        Guid? packageId,
        string filePath,
        byte[] packageBytes)
    {
        return new CompositionDocumentSnapshot(
            Id: (packageId ?? domain.GlobalId).ToString(),
            Title: packageTitle ?? domain.Name,
            Domain: MapDomain(domain),
            Extensions: BuildPackageExtensions(filePath, packageBytes));
    }

    private static CompositionDomainSnapshot MapDomain(Domain domain)
    {
        return new CompositionDomainSnapshot(
            Id: domain.GlobalId.ToString(),
            Name: domain.Name,
            Summary: domain.Summary,
            ConceptDefinitions: domain.ConceptDefinitions.Select(definition => MapIdeaDefinition(definition, "Concept")).ToArray(),
            RelationshipDefinitions: domain.RelationshipDefinitions.Select(definition => MapIdeaDefinition(definition, "Relationship")).ToArray(),
            MarkerDefinitions: domain.MarkerDefinitions.Select(MapMarkerDefinition).ToArray(),
            TableDefinitions: domain.TableDefinitions.Select(MapTableDefinition).ToArray(),
            ExternalLanguages: domain.ExternalLanguages.Select(MapExternalLanguage).ToArray(),
            Templates: MapTemplates(domain).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.domain.techName", domain.TechName),
                new CompositionExtensionSnapshot("legacy.domain.modelRevision", domain.ModelRevision.ToString())
            ]);
    }

    private static CompositionDefinitionSnapshot MapIdeaDefinition(IdeaDefinition definition, string kind)
    {
        return new CompositionDefinitionSnapshot(
            Id: definition.GlobalId.ToString(),
            Name: definition.Name,
            Kind: kind,
            Summary: definition.Summary,
            Style: MapSymbolStyle(definition.DefaultSymbolFormat),
            Details: definition.DetailDesignators.Select(MapDetailDesignator).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", definition.TechName),
                new CompositionExtensionSnapshot("legacy.metaId", definition.MetaId.ToString()),
                new CompositionExtensionSnapshot("legacy.representativeShape", definition.RepresentativeShape)
            ]);
    }

    private static CompositionDefinitionSnapshot MapMarkerDefinition(MarkerDefinition definition)
    {
        return new CompositionDefinitionSnapshot(
            Id: MarkerId(definition),
            Name: definition.Name,
            Kind: "Marker",
            Summary: definition.Summary,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", definition.TechName),
                new CompositionExtensionSnapshot("legacy.clusterKey", definition.ClusterKey)
            ]);
    }

    private static CompositionDefinitionSnapshot MapTableDefinition(TableDefinition definition)
    {
        return new CompositionDefinitionSnapshot(
            Id: definition.GlobalId.ToString(),
            Name: definition.Name,
            Kind: "Table",
            Summary: definition.Summary,
            Details: definition.FieldDefinitions
                .OrderBy(field => field.StorageIndex)
                .Select(MapFieldDefinition)
                .ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", definition.TechName),
                new CompositionExtensionSnapshot("legacy.metaId", definition.MetaId.ToString())
            ]);
    }

    private static CompositionDefinitionSnapshot MapExternalLanguage(ExternalLanguageDeclaration language)
    {
        return new CompositionDefinitionSnapshot(
            Id: language.GlobalId.ToString(),
            Name: language.Name,
            Kind: "ExternalLanguage",
            Summary: language.Summary,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", language.TechName),
                new CompositionExtensionSnapshot("legacy.metaId", language.MetaId.ToString())
            ]);
    }

    private static CompositionDetailSnapshot MapDetailDesignator(DetailDesignator designator)
    {
        return new CompositionDetailSnapshot(
            Id: designator.GlobalId.ToString(),
            Kind: designator.GetType().Name,
            Name: designator.Name,
            Value: designator.TechName,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.summary", designator.Summary)
            ]);
    }

    private static CompositionDetailSnapshot MapFieldDefinition(FieldDefinition field)
    {
        return new CompositionDetailSnapshot(
            Id: field.GlobalId.ToString(),
            Kind: "FieldDefinition",
            Name: field.Name,
            Value: field.FieldType?.Name ?? field.FieldType?.ToString() ?? string.Empty,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", field.TechName),
                new CompositionExtensionSnapshot("legacy.summary", field.Summary),
                new CompositionExtensionSnapshot("legacy.storageIndex", field.StorageIndex.ToString()),
                new CompositionExtensionSnapshot("legacy.isRequired", field.IsRequired.ToString())
            ]);
    }

    private static CompositionIdeaSnapshot MapIdea(CompositionNodeView node, Idea? idea)
    {
        if (idea is null)
        {
            return new CompositionIdeaSnapshot(node.Id, node.Text);
        }

        return new CompositionIdeaSnapshot(
            Id: node.Id,
            Name: node.Text,
            DefinitionId: idea.IdeaDefinitor?.GlobalId.ToString() ?? string.Empty,
            Summary: idea.Summary,
            Details: idea.Details.Select(MapContainedDetail).ToArray(),
            Markers: idea.Markings.Select(marker => MarkerId(marker.Definitor)).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", idea.TechName),
                new CompositionExtensionSnapshot("legacy.type", idea.GetType().Name)
            ]);
    }

    private static CompositionRelationshipSnapshot MapRelationship(CompositionConnectorView connector, Relationship? relationship)
    {
        if (relationship is null)
        {
            return new CompositionRelationshipSnapshot(connector.Id, connector.Text, connector.SourceId, connector.TargetId);
        }

        return new CompositionRelationshipSnapshot(
            Id: connector.Id,
            Name: relationship.Name,
            SourceIdeaId: connector.SourceId,
            TargetIdeaId: connector.TargetId,
            DefinitionId: relationship.RelationshipDefinitor?.Value?.GlobalId.ToString() ?? string.Empty,
            Details: relationship.Details.Select(MapContainedDetail).ToArray(),
            Markers: relationship.Markings.Select(marker => MarkerId(marker.Definitor)).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", relationship.TechName),
                new CompositionExtensionSnapshot("legacy.type", relationship.GetType().Name)
            ]);
    }

    private static CompositionDetailSnapshot MapContainedDetail(ContainedDetail detail)
    {
        var value = detail is Table table ? table.RecordsLabel : detail.ToString();
        return new CompositionDetailSnapshot(
            Id: detail.Designation?.GlobalId.ToString() ?? Guid.NewGuid().ToString(),
            Kind: detail.Kind?.TechName ?? detail.GetType().Name,
            Name: detail.Designation?.Name ?? detail.GetType().Name,
            Value: value,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.type", detail.GetType().Name),
                new CompositionExtensionSnapshot("legacy.techName", detail.Designation?.TechName ?? string.Empty)
            ]);
    }

    private static CompositionStyleSnapshot MapSymbolStyle(VisualSymbolFormat? format)
    {
        if (format is null)
        {
            return new CompositionStyleSnapshot();
        }

        return new CompositionStyleSnapshot(
            Fill: format.MainBackground?.ToString() ?? string.Empty,
            Stroke: format.LineBrush?.ToString() ?? string.Empty,
            StrokeThickness: format.LineThickness,
            StrokeDash: format.LineDash?.ToString() ?? string.Empty);
    }

    private static string MarkerId(MarkerDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.TechName)
            ? definition.Name
            : definition.TechName;
    }

    private static IEnumerable<CompositionExtensionSnapshot> MapTemplates(Domain domain)
    {
        foreach (var template in domain.OutputTemplatesForConcepts)
        {
            yield return MapTemplate("concept", template);
        }

        foreach (var template in domain.OutputTemplatesForRelationships)
        {
            yield return MapTemplate("relationship", template);
        }
    }

    private static CompositionExtensionSnapshot MapTemplate(string scope, TextTemplate template)
    {
        var language = template.Language?.TechName ?? "default";
        return new CompositionExtensionSnapshot(
            $"legacy.template.{scope}.{language}",
            template.Text ?? string.Empty,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = template.Language?.Name ?? string.Empty,
                ["extendsBaseTemplate"] = template.ExtendsBaseTemplate.ToString()
            });
    }

    private static IReadOnlyList<CompositionExtensionSnapshot> BuildPackageExtensions(string filePath, byte[] packageBytes)
    {
        return
        [
            new CompositionExtensionSnapshot("legacy.source.extension", Path.GetExtension(filePath).ToLowerInvariant()),
            new CompositionExtensionSnapshot("legacy.source.fileName", Path.GetFileName(filePath)),
            new CompositionExtensionSnapshot("legacy.package.base64", Convert.ToBase64String(packageBytes))
        ];
    }

    private static IEnumerable<object> ReadViewObjects(View? view)
    {
        if (view is null)
        {
            yield break;
        }

        foreach (var child in view.ViewChildren)
        {
            if (child?.Key is not null)
            {
                yield return child.Key;
            }
        }
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
}
