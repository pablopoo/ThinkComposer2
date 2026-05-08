using System.Text;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotPreviewTextBuilder
{
    public static string Build(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var builder = new StringBuilder();
        builder.AppendLine(snapshot.Title);
        builder.AppendLine($"{snapshot.Nodes.Count} concepts");
        builder.AppendLine($"{snapshot.Connectors.Count} relationships");
        builder.AppendLine();

        builder.AppendLine($"Concepts ({snapshot.Nodes.Count})");
        foreach (var node in snapshot.Nodes.OrderBy(node => node.Text, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {GetText(node.Text, node.Id)}");
        }

        builder.AppendLine();
        builder.AppendLine($"Relationships ({snapshot.Connectors.Count})");
        foreach (var connector in snapshot.Connectors.OrderBy(connector => connector.Text, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {GetText(connector.Text, connector.Id)}: {connector.SourceId} -> {connector.TargetId}");
        }

        return builder.ToString();
    }

    private static string GetText(string text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}
