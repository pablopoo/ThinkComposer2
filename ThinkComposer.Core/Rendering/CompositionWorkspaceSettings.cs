namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionWorkspaceSettings(
    CompositionWorkspaceTheme Theme,
    bool IsExplorerVisible,
    bool IsInspectorVisible,
    bool IsBottomVisible,
    IReadOnlyList<string> RecentFiles)
{
    public static CompositionWorkspaceSettings Default { get; } = new(
        CompositionWorkspaceTheme.Light,
        IsExplorerVisible: true,
        IsInspectorVisible: true,
        IsBottomVisible: true,
        RecentFiles: Array.Empty<string>());
}
