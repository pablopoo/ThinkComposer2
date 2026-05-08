using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionComplementRenderItem(
    string Key,
    string Title,
    string Body,
    string Kind,
    TcPoint Position,
    TcSize Size);
