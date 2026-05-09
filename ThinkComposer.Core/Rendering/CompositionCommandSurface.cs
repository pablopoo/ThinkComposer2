namespace Instrumind.ThinkComposer.Core.Rendering;

[Flags]
public enum CompositionCommandSurface
{
    None = 0,
    TopBar = 1,
    CanvasContextMenu = 2,
    Inspector = 4,
    ViewMenu = 8,
    CommandPalette = 16,
    DomainStudio = 32
}
