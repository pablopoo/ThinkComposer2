namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentFactory
{
    public static CompositionViewSnapshot CreateEmpty(string title)
    {
        return new CompositionViewSnapshot(
            Guid.NewGuid().ToString(),
            string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim(),
            Array.Empty<CompositionNodeView>(),
            Array.Empty<CompositionConnectorView>());
    }
}
