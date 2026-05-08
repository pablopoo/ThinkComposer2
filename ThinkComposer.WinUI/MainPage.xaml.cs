using Instrumind.ThinkComposer.Core.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Instrumind.ThinkComposer.WinUI;

public sealed partial class MainPage : Page
{
    private static readonly GridLength ExplorerWidth = new(248);
    private static readonly GridLength InspectorWidth = new(320);
    private static readonly GridLength BottomHeight = new(148);

    private bool _isExplorerVisible = true;
    private bool _isInspectorVisible = true;
    private bool _isBottomVisible = true;
    private bool _isDarkTheme;

    public MainPage()
    {
        InitializeComponent();
        LoadStartupSnapshot();
    }

    public event EventHandler<ElementTheme>? AppThemeChanged;

    private void ExplorerButton_Click(object sender, RoutedEventArgs e)
    {
        _isExplorerVisible = !_isExplorerVisible;
        ApplyPanelState();
    }

    private void InspectorButton_Click(object sender, RoutedEventArgs e)
    {
        _isInspectorVisible = !_isInspectorVisible;
        ApplyPanelState();
    }

    private void BottomButton_Click(object sender, RoutedEventArgs e)
    {
        _isBottomVisible = !_isBottomVisible;
        ApplyPanelState();
    }

    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        var showPanels = !(_isExplorerVisible || _isInspectorVisible || _isBottomVisible);
        _isExplorerVisible = showPanels;
        _isInspectorVisible = showPanels;
        _isBottomVisible = showPanels;
        ApplyPanelState();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        var theme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        RootPage.RequestedTheme = theme;
        AppThemeChanged?.Invoke(this, theme);
    }

    private void ApplyPanelState()
    {
        ExplorerColumn.Width = _isExplorerVisible ? ExplorerWidth : new GridLength(0);
        InspectorColumn.Width = _isInspectorVisible ? InspectorWidth : new GridLength(0);
        BottomRow.Height = _isBottomVisible ? BottomHeight : new GridLength(0);

        ExplorerPanel.Visibility = _isExplorerVisible ? Visibility.Visible : Visibility.Collapsed;
        InspectorPanel.Visibility = _isInspectorVisible ? Visibility.Visible : Visibility.Collapsed;
        BottomPanel.Visibility = _isBottomVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadStartupSnapshot()
    {
        var snapshotPath = FindStartupSnapshotPath();
        if (snapshotPath is null)
        {
            return;
        }

        try
        {
            var snapshot = CompositionViewSnapshotXmlStore.Load(snapshotPath);
            CanvasView.LoadSnapshot(snapshot);
            CompositionTitleText.Text = snapshot.Title;
            StatusContextText.Text = $"Loaded {Path.GetFileName(snapshotPath)}";
            MessagesText.Text =
                $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
                $"Canvas renderer: loaded .tcview snapshot{Environment.NewLine}" +
                $"Document: {snapshot.Title}{Environment.NewLine}" +
                $"Nodes: {snapshot.Nodes.Count}, connectors: {snapshot.Connectors.Count}";
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
                $"Canvas renderer: Core snapshot DTOs{Environment.NewLine}" +
                $"Could not load snapshot: {problem.Message}";
        }
    }

    private static string? FindStartupSnapshotPath()
    {
        var commandLineSnapshot = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(IsExistingSnapshotPath);

        if (commandLineSnapshot is not null)
        {
            return Path.GetFullPath(commandLineSnapshot);
        }

        return EnumerateAncestorDirectories(AppContext.BaseDirectory)
            .Concat(EnumerateAncestorDirectories(Environment.CurrentDirectory))
            .SelectMany(directory => new[]
            {
                Path.Combine(directory, "docs", "generated", "All-Purpose.tcview"),
                Path.Combine(directory, "PredefinedContent", "All-Purpose.tcview")
            })
            .FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> EnumerateAncestorDirectories(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            yield return directory.FullName;
        }
    }

    private static bool IsExistingSnapshotPath(string path)
    {
        return string.Equals(Path.GetExtension(path), ".tcview", StringComparison.OrdinalIgnoreCase)
            && File.Exists(path);
    }
}
