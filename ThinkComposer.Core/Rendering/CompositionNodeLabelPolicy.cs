namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionNodeLabelPolicy
{
    private static readonly CompositionNodeLabelLayout Compact = new(
        ShowsSubtitle: false,
        AllowsWrapping: false,
        TitleFontSize: 11,
        BodyFontSize: 0,
        Padding: 6);

    private static readonly CompositionNodeLabelLayout Expanded = new(
        ShowsSubtitle: true,
        AllowsWrapping: true,
        TitleFontSize: 14,
        BodyFontSize: 12,
        Padding: 12);

    public static CompositionNodeLabelLayout ForNode(CompositionNodeView node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return node.Size.Height < 56 || node.Size.Width < 120 ? Compact : Expanded;
    }
}
