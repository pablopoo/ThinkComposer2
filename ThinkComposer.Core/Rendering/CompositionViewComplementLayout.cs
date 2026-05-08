using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionViewComplementLayout
{
    public static IReadOnlyList<CompositionComplementRenderItem> Build(
        IReadOnlyList<CompositionExtensionSnapshot> complements,
        IReadOnlyList<CompositionNodeView> nodes)
    {
        if (complements.Count == 0)
        {
            return Array.Empty<CompositionComplementRenderItem>();
        }

        var bounds = GetNodeBounds(nodes);
        var cardIndex = 0;
        var items = new List<CompositionComplementRenderItem>();
        foreach (var complement in complements)
        {
            var kind = Classify(complement.Key);
            if (kind == "Group")
            {
                items.Add(new CompositionComplementRenderItem(
                    complement.Key,
                    TitleFromKey(complement.Key),
                    complement.Value,
                    kind,
                    new TcPoint(bounds.Left - 28, bounds.Top - 28),
                    new TcSize(Math.Max(180, bounds.Width + 56), Math.Max(120, bounds.Height + 56))));
                continue;
            }

            items.Add(new CompositionComplementRenderItem(
                complement.Key,
                TitleFromKey(complement.Key),
                complement.Value,
                kind,
                new TcPoint(bounds.Right + 32, bounds.Top + (cardIndex * 92)),
                new TcSize(220, kind == "Legend" ? 104 : 84)));
            cardIndex++;
        }

        return items;
    }

    private static (double Left, double Top, double Right, double Bottom, double Width, double Height) GetNodeBounds(
        IReadOnlyList<CompositionNodeView> nodes)
    {
        if (nodes.Count == 0)
        {
            return (80, 80, 360, 240, 280, 160);
        }

        var left = nodes.Min(node => node.Position.X);
        var top = nodes.Min(node => node.Position.Y);
        var right = nodes.Max(node => node.Position.X + node.Size.Width);
        var bottom = nodes.Max(node => node.Position.Y + node.Size.Height);
        return (left, top, right, bottom, right - left, bottom - top);
    }

    private static string Classify(string key)
    {
        if (key.StartsWith("group.", StringComparison.OrdinalIgnoreCase))
        {
            return "Group";
        }

        if (key.IndexOf("legend", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Legend";
        }

        if (key.IndexOf("quote", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Quote";
        }

        return "Info";
    }

    private static string TitleFromKey(string key)
    {
        var title = key;
        var dot = title.LastIndexOf('.');
        if (dot >= 0 && dot + 1 < title.Length)
        {
            title = title.Substring(dot + 1);
        }

        title = title.Replace('-', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(title)
            ? "Complement"
            : char.ToUpperInvariant(title[0]) + title.Substring(1);
    }
}
