using System.Net;
using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentReportHtmlExporter
{
    public static string Export(CompositionDocumentSnapshot document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var definitions = BuildDefinitionsIndex(document.Domain);
        var ideas = document.Ideas.ToDictionary(idea => idea.Id, StringComparer.Ordinal);
        var builder = new StringBuilder();

        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine($"  <title>{Encode(document.Title)} - ThinkComposer Report</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    :root { color-scheme: light; --border: #d8dee8; --muted: #5f6b7a; --fill: #f6f8fb; --text: #1f2328; --accent: #0969da; }");
        builder.AppendLine("    body { margin: 32px; font-family: Segoe UI, Arial, sans-serif; color: var(--text); background: #fff; }");
        builder.AppendLine("    h1 { margin: 0 0 8px; font-size: 26px; font-weight: 600; }");
        builder.AppendLine("    h2 { margin: 28px 0 12px; font-size: 18px; font-weight: 600; border-bottom: 1px solid var(--border); padding-bottom: 6px; }");
        builder.AppendLine("    h3 { margin: 0 0 8px; font-size: 14px; font-weight: 600; }");
        builder.AppendLine("    .summary { color: var(--muted); margin-bottom: 18px; }");
        builder.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; }");
        builder.AppendLine("    .card { border: 1px solid var(--border); border-radius: 8px; padding: 12px; break-inside: avoid; background: #fff; }");
        builder.AppendLine("    .meta { color: var(--muted); font-size: 12px; margin: 2px 0; }");
        builder.AppendLine("    .detail { margin-top: 8px; padding-top: 8px; border-top: 1px solid var(--border); font-size: 12px; }");
        builder.AppendLine("    .view svg { width: 100%; height: auto; border: 1px solid var(--border); background: #fff; }");
        builder.AppendLine("    ul { margin: 6px 0 0 18px; padding: 0; }");
        builder.AppendLine("    li { margin: 3px 0; }");
        builder.AppendLine("    @media print { body { margin: 16px; } .card { page-break-inside: avoid; } }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine($"  <h1>{Encode(document.Title)}</h1>");
        builder.AppendLine($"  <div class=\"summary\">Domain: {Encode(document.Domain.Name)} | Concepts: {document.Ideas.Count} | Relationships: {document.Relationships.Count} | Views: {document.Views.Count}</div>");

        AppendDomainSection(builder, document.Domain);
        AppendViewsSection(builder, document);
        AppendIdeasSection(builder, document, definitions);
        AppendRelationshipsSection(builder, document, definitions, ideas);

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static void AppendDomainSection(StringBuilder builder, CompositionDomainSnapshot domain)
    {
        builder.AppendLine("  <h2>Domain</h2>");
        builder.AppendLine("  <div class=\"card\">");
        builder.AppendLine($"    <h3>{Encode(domain.Name)}</h3>");
        if (!string.IsNullOrWhiteSpace(domain.Summary))
        {
            builder.AppendLine($"    <div class=\"meta\">{Encode(domain.Summary)}</div>");
        }

        AppendDefinitionList(builder, "Concept definitions", domain.ConceptDefinitions);
        AppendDefinitionList(builder, "Relationship definitions", domain.RelationshipDefinitions);
        AppendDefinitionList(builder, "Link-role variants", domain.LinkRoleDefinitions);
        AppendDefinitionList(builder, "Marker definitions", domain.MarkerDefinitions);
        AppendDefinitionList(builder, "Table definitions", domain.TableDefinitions);
        AppendDefinitionList(builder, "External languages", domain.ExternalLanguages);
        builder.AppendLine("  </div>");
    }

    private static void AppendViewsSection(StringBuilder builder, CompositionDocumentSnapshot document)
    {
        builder.AppendLine("  <h2>Views</h2>");
        if (document.Views.Count == 0)
        {
            builder.AppendLine("  <div class=\"card meta\">No views.</div>");
            return;
        }

        foreach (var view in document.Views)
        {
            var snapshot = new CompositionViewSnapshot(view.Id, view.Name, view.Nodes, view.Connectors);
            builder.AppendLine("  <div class=\"card view\">");
            builder.AppendLine($"    <h3>{Encode(view.Name)}</h3>");
            builder.AppendLine(CompositionSnapshotSvgExporter.Export(snapshot));
            builder.AppendLine("  </div>");
        }
    }

    private static void AppendIdeasSection(
        StringBuilder builder,
        CompositionDocumentSnapshot document,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions)
    {
        builder.AppendLine("  <h2>Concepts</h2>");
        builder.AppendLine("  <div class=\"grid\">");
        foreach (var idea in document.Ideas)
        {
            builder.AppendLine("    <div class=\"card\">");
            builder.AppendLine($"      <h3>{Encode(idea.Name)}</h3>");
            builder.AppendLine($"      <div class=\"meta\">Definition: {Encode(FindDefinitionName(definitions, idea.DefinitionId))}</div>");
            AppendSummary(builder, idea.Summary);
            AppendMarkers(builder, idea.Markers, definitions);
            AppendDetails(builder, idea.Details);
            builder.AppendLine("    </div>");
        }
        builder.AppendLine("  </div>");
    }

    private static void AppendRelationshipsSection(
        StringBuilder builder,
        CompositionDocumentSnapshot document,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions,
        IReadOnlyDictionary<string, CompositionIdeaSnapshot> ideas)
    {
        builder.AppendLine("  <h2>Relationships</h2>");
        builder.AppendLine("  <div class=\"grid\">");
        foreach (var relationship in document.Relationships)
        {
            builder.AppendLine("    <div class=\"card\">");
            builder.AppendLine($"      <h3>{Encode(relationship.Name)}</h3>");
            builder.AppendLine($"      <div class=\"meta\">Definition: {Encode(FindDefinitionName(definitions, relationship.DefinitionId))}</div>");
            if (!string.IsNullOrWhiteSpace(relationship.LinkRoleId))
            {
                builder.AppendLine($"      <div class=\"meta\">Link role: {Encode(FindDefinitionName(definitions, relationship.LinkRoleId))}</div>");
            }
            builder.AppendLine($"      <div class=\"meta\">Source: {Encode(FindIdeaName(ideas, relationship.SourceIdeaId))}</div>");
            builder.AppendLine($"      <div class=\"meta\">Target: {Encode(FindIdeaName(ideas, relationship.TargetIdeaId))}</div>");
            AppendMarkers(builder, relationship.Markers, definitions);
            AppendDetails(builder, relationship.Details);
            builder.AppendLine("    </div>");
        }
        builder.AppendLine("  </div>");
    }

    private static void AppendDefinitionList(
        StringBuilder builder,
        string title,
        IReadOnlyList<CompositionDefinitionSnapshot> definitions)
    {
        if (definitions.Count == 0)
        {
            return;
        }

        builder.AppendLine($"    <div class=\"detail\"><strong>{Encode(title)}</strong><ul>");
        foreach (var definition in definitions)
        {
            builder.AppendLine($"      <li>{Encode(definition.Name)} <span class=\"meta\">{Encode(definition.Kind)}</span></li>");
        }
        builder.AppendLine("    </ul></div>");
    }

    private static void AppendSummary(StringBuilder builder, string summary)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            builder.AppendLine($"      <div class=\"meta\">{Encode(summary)}</div>");
        }
    }

    private static void AppendMarkers(
        StringBuilder builder,
        IReadOnlyList<string> markers,
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions)
    {
        if (markers.Count == 0)
        {
            return;
        }

        builder.AppendLine("      <div class=\"detail\"><strong>Markers</strong><ul>");
        foreach (var marker in markers)
        {
            builder.AppendLine($"        <li>{Encode(FindDefinitionName(definitions, marker))}</li>");
        }
        builder.AppendLine("      </ul></div>");
    }

    private static void AppendDetails(StringBuilder builder, IReadOnlyList<CompositionDetailSnapshot> details)
    {
        if (details.Count == 0)
        {
            return;
        }

        builder.AppendLine("      <div class=\"detail\"><strong>Details</strong><ul>");
        foreach (var detail in details)
        {
            var value = string.IsNullOrWhiteSpace(detail.Value) ? detail.Kind : detail.Value;
            builder.AppendLine($"        <li>{Encode(detail.Name)}: {Encode(value)}</li>");
        }
        builder.AppendLine("      </ul></div>");
    }

    private static IReadOnlyDictionary<string, CompositionDefinitionSnapshot> BuildDefinitionsIndex(CompositionDomainSnapshot domain)
    {
        return domain.ConceptDefinitions
            .Concat(domain.RelationshipDefinitions)
            .Concat(domain.LinkRoleDefinitions)
            .Concat(domain.MarkerDefinitions)
            .Concat(domain.TableDefinitions)
            .Concat(domain.ExternalLanguages)
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    private static string FindDefinitionName(
        IReadOnlyDictionary<string, CompositionDefinitionSnapshot> definitions,
        string definitionId)
    {
        return definitions.TryGetValue(definitionId, out var definition)
            ? definition.Name
            : definitionId;
    }

    private static string FindIdeaName(IReadOnlyDictionary<string, CompositionIdeaSnapshot> ideas, string ideaId)
    {
        return ideas.TryGetValue(ideaId, out var idea) ? idea.Name : ideaId;
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
