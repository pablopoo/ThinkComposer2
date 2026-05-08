using System.Text;
using System.Text.RegularExpressions;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentFileGenerator
{
    private const string FileNameDirective = "%%:FILENAME=";
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}", RegexOptions.Compiled);

    public static CompositionFileGenerationResult Generate(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var files = new List<CompositionGeneratedFile>();
        var definitions = BuildDefinitionsIndex(document.Domain);
        var ideas = document.Ideas.ToDictionary(idea => idea.Id, StringComparer.Ordinal);
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conceptTemplates = GetTemplates(document.Domain, "concept");
        var relationshipTemplates = GetTemplates(document.Domain, "relationship");

        foreach (var idea in document.Ideas)
        {
            var definition = FindDefinition(definitions, idea.DefinitionId);
            var values = CreateBaseValues(document, idea.Id, idea.Name, idea.Summary, definition);
            AddDetails(values, "Details", idea.Details);

            foreach (var template in conceptTemplates)
            {
                files.Add(RenderFile(template, values, usedPaths));
            }
        }

        foreach (var relationship in document.Relationships)
        {
            var definition = FindDefinition(definitions, relationship.DefinitionId);
            var source = FindIdea(ideas, relationship.SourceIdeaId);
            var target = FindIdea(ideas, relationship.TargetIdeaId);
            var values = CreateBaseValues(document, relationship.Id, relationship.Name, string.Empty, definition);
            values["Source.Id"] = source?.Id ?? relationship.SourceIdeaId;
            values["Source.Name"] = source?.Name ?? relationship.SourceIdeaId;
            values["Target.Id"] = target?.Id ?? relationship.TargetIdeaId;
            values["Target.Name"] = target?.Name ?? relationship.TargetIdeaId;
            AddDetails(values, "Details", relationship.Details);

            foreach (var template in relationshipTemplates)
            {
                files.Add(RenderFile(template, values, usedPaths));
            }
        }

        return new CompositionFileGenerationResult(files);
    }

    private static CompositionGeneratedFile RenderFile(
        CompositionExtensionSnapshot template,
        IReadOnlyDictionary<string, string> values,
        ISet<string> usedPaths)
    {
        var fileNameTemplate = string.Empty;
        var builder = new StringBuilder();
        using var reader = new StringReader(template.Value);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith(FileNameDirective, StringComparison.OrdinalIgnoreCase))
            {
                fileNameTemplate = trimmed.Substring(FileNameDirective.Length).Trim();
                continue;
            }

            if (trimmed.StartsWith("%%:", StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine(line);
        }

        var renderedFileName = string.IsNullOrWhiteSpace(fileNameTemplate)
            ? $"{Resolve(values, "Name")}.txt"
            : RenderText(fileNameTemplate, values);
        var relativePath = MakeUniquePath(SanitizeRelativePath(renderedFileName), usedPaths);
        usedPaths.Add(relativePath);
        return new CompositionGeneratedFile(relativePath, RenderText(builder.ToString(), values).TrimEnd());
    }

    private static IReadOnlyList<CompositionExtensionSnapshot> GetTemplates(CompositionDomainSnapshot domain, string scope)
    {
        var legacyPrefix = $"legacy.template.{scope}.";
        var modernPrefix = $"template.{scope}.";
        return domain.Templates
            .Where(template =>
                template.Key.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase) ||
                template.Key.StartsWith(modernPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(template => !string.IsNullOrWhiteSpace(template.Value))
            .ToArray();
    }

    private static string RenderText(string template, IReadOnlyDictionary<string, string> values)
    {
        return PlaceholderRegex.Replace(template, match => Resolve(values, match.Groups[1].Value));
    }

    private static string Resolve(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static Dictionary<string, string> CreateBaseValues(
        CompositionDocumentSnapshot document,
        string id,
        string name,
        string summary,
        CompositionDefinitionSnapshot? definition)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Document.Id"] = document.Id,
            ["Document.Title"] = document.Title,
            ["Domain.Id"] = document.Domain.Id,
            ["Domain.Name"] = document.Domain.Name,
            ["Id"] = id,
            ["Name"] = name,
            ["Summary"] = summary,
            ["Definition.Id"] = definition?.Id ?? string.Empty,
            ["Definition.Name"] = definition?.Name ?? string.Empty,
            ["Definition.Kind"] = definition?.Kind ?? string.Empty
        };
    }

    private static void AddDetails(
        IDictionary<string, string> values,
        string prefix,
        IReadOnlyList<CompositionDetailSnapshot> details)
    {
        foreach (var detail in details)
        {
            if (string.IsNullOrWhiteSpace(detail.Name))
            {
                continue;
            }

            values[$"{prefix}.{detail.Name}"] = detail.Value;
        }
    }

    private static IReadOnlyDictionary<string, CompositionDefinitionSnapshot> BuildDefinitionsIndex(CompositionDomainSnapshot domain)
    {
        return domain.ConceptDefinitions
            .Concat(domain.RelationshipDefinitions)
            .Concat(domain.MarkerDefinitions)
            .Concat(domain.TableDefinitions)
            .Concat(domain.ExternalLanguages)
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    private static CompositionDefinitionSnapshot? FindDefinition(
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions,
        string definitionId)
    {
        return definitions.TryGetValue(definitionId, out var definition) ? definition : null;
    }

    private static CompositionIdeaSnapshot? FindIdea(
        IReadOnlyDictionary<string, CompositionIdeaSnapshot> ideas,
        string ideaId)
    {
        return ideas.TryGetValue(ideaId, out var idea) ? idea : null;
    }

    private static string SanitizeRelativePath(string relativePath)
    {
        var segments = relativePath
            .Replace('\\', '/')
            .Split(['/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeFileNameSegment)
            .Where(segment => segment.Length > 0)
            .ToArray();
        return segments.Length == 0 ? "Generated.txt" : Path.Combine(segments);
    }

    private static string SanitizeFileNameSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(segment.Select(character =>
            invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return sanitized is "." or ".." ? "_" : sanitized;
    }

    private static string MakeUniquePath(string relativePath, ISet<string> usedPaths)
    {
        if (!usedPaths.Contains(relativePath))
        {
            return relativePath;
        }

        var directory = Path.GetDirectoryName(relativePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var extension = Path.GetExtension(relativePath);
        for (var index = 2; ; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName}-{index}{extension}");
            if (!usedPaths.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
