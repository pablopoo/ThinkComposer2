using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionNodeView(
    string Id,
    string Text,
    TcPoint Position,
    TcSize Size,
    CompositionStyleSnapshot? Style = null)
{
    public CompositionStyleSnapshot Style { get; init; } = Style ?? new CompositionStyleSnapshot();
}
