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
    private CompositionSnapshotIndex? _snapshotIndex;

    public MainPage()
    {
        InitializeComponent();
        CanvasView.SelectedNodeChanged += CanvasView_SelectedNodeChanged;
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
        DispatcherQueue.TryEnqueue(() => CanvasView.FitSnapshotToViewport());
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
            ApplySnapshot(snapshot, snapshotPath);
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
                $"Canvas renderer: Core snapshot DTOs{Environment.NewLine}" +
                $"Could not load snapshot: {problem.Message}";
        }
    }

    private void ApplySnapshot(CompositionViewSnapshot snapshot, string snapshotPath)
    {
        _snapshotIndex = CompositionSnapshotIndex.FromSnapshot(snapshot);

        UpdateExplorer(snapshot);
        CanvasView.LoadSnapshot(snapshot);
        ApplySelectedNode(CanvasView.SelectedNode ?? _snapshotIndex.FirstNode);

        CompositionTitleText.Text = snapshot.Title;
        StatusContextText.Text = $"Loaded {Path.GetFileName(snapshotPath)}";
        MessagesText.Text =
            $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
            $"Canvas renderer: loaded .tcview snapshot{Environment.NewLine}" +
            $"Document: {snapshot.Title}{Environment.NewLine}" +
            $"Nodes: {snapshot.Nodes.Count}, connectors: {snapshot.Connectors.Count}";
    }

    private void UpdateExplorer(CompositionViewSnapshot snapshot)
    {
        ExplorerTree.RootNodes.Clear();

        var root = new TreeViewNode
        {
            Content = $"{snapshot.Title} ({snapshot.Nodes.Count})",
            IsExpanded = true
        };

        foreach (var node in snapshot.Nodes)
        {
            root.Children.Add(new TreeViewNode { Content = GetNodeTitle(node) });
        }

        ExplorerTree.RootNodes.Add(root);

        var relations = new TreeViewNode
        {
            Content = $"Relationships ({snapshot.Connectors.Count})",
            IsExpanded = true
        };
        relations.Children.Add(new TreeViewNode { Content = "Pointing to..." });
        relations.Children.Add(new TreeViewNode { Content = "Pointed by..." });
        ExplorerTree.RootNodes.Add(relations);
    }

    private void CanvasView_SelectedNodeChanged(object? sender, CompositionNodeView? node)
    {
        ApplySelectedNode(node);
    }

    private void ApplySelectedNode(CompositionNodeView? node)
    {
        if (node is null)
        {
            InspectorNameBox.Text = "No selection";
            InspectorXBox.Value = 0;
            InspectorYBox.Value = 0;
            InspectorWidthBox.Value = 0;
            InspectorHeightBox.Value = 0;
            OutgoingText.Text = "0";
            IncomingText.Text = "0";
            return;
        }

        InspectorNameBox.Text = GetNodeTitle(node);
        InspectorKindBox.SelectedIndex = 0;
        InspectorStatusBox.SelectedIndex = 0;
        InspectorXBox.Value = Math.Round(node.Position.X, 1);
        InspectorYBox.Value = Math.Round(node.Position.Y, 1);
        InspectorWidthBox.Value = Math.Round(node.Size.Width, 1);
        InspectorHeightBox.Value = Math.Round(node.Size.Height, 1);
        OutgoingText.Text = (_snapshotIndex?.CountOutgoing(node.Id) ?? 0).ToString();
        IncomingText.Text = (_snapshotIndex?.CountIncoming(node.Id) ?? 0).ToString();
    }

    private static string GetNodeTitle(CompositionNodeView node)
    {
        return string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text;
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
