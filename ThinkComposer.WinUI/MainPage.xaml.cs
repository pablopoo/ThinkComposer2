using Instrumind.ThinkComposer.Core.Primitives;
using Instrumind.ThinkComposer.Core.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

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
    private bool _isDirty;
    private string? _snapshotPath;
    private CompositionViewSnapshot? _currentSnapshot;
    private CompositionEditingSession? _editingSession;
    private CompositionSnapshotIndex? _snapshotIndex;
    private string? _selectedNodeId;
    private bool _isApplyingInspector;

    public MainPage()
    {
        InitializeComponent();
        CanvasView.SelectedNodeChanged += CanvasView_SelectedNodeChanged;
        CanvasView.NodeMoved += CanvasView_NodeMoved;
        CanvasView.NodeMoveCompleted += CanvasView_NodeMoveCompleted;
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null || string.IsNullOrWhiteSpace(_snapshotPath))
        {
            MessagesText.Text =
                $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
                "No snapshot is available to save.";
            return;
        }

        if (!_isDirty)
        {
            StatusContextText.Text = $"No changes {Path.GetFileName(_snapshotPath)}";
            return;
        }

        try
        {
            CompositionViewSnapshotXmlStore.Save(_currentSnapshot, _snapshotPath);
            _isDirty = false;
            StatusContextText.Text = $"Saved {Path.GetFileName(_snapshotPath)}";
            MessagesText.Text =
                $"Snapshot saved{Environment.NewLine}" +
                $"Document: {_currentSnapshot.Title}{Environment.NewLine}" +
                $"Path: {_snapshotPath}";
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"Could not save snapshot{Environment.NewLine}" +
                problem.Message;
        }
    }

    private void NewConceptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        var center = CanvasView.GetViewportCenter();
        var size = new TcSize(160, 70);
        var node = new CompositionNodeView(
            Guid.NewGuid().ToString(),
            "New Concept",
            new TcPoint(center.X - size.Width / 2, center.Y - size.Height / 2),
            size);
        ApplyEditedSnapshot(CompositionSnapshotEditor.CreateNode(_currentSnapshot, node), node.Id, fitToViewport: false);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null || string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return;
        }

        var remainingNodeId = _currentSnapshot.Nodes.FirstOrDefault(node => node.Id != _selectedNodeId)?.Id;
        ApplyEditedSnapshot(
            CompositionSnapshotEditor.DeleteNode(_currentSnapshot, _selectedNodeId),
            remainingNodeId,
            fitToViewport: false);
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSession is null || !_editingSession.CanUndo)
        {
            return;
        }

        ApplySessionSnapshot(_editingSession.Undo(), _selectedNodeId, markDirty: true, fitToViewport: false);
        SetStatusModified();
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSession is null || !_editingSession.CanRedo)
        {
            return;
        }

        ApplySessionSnapshot(_editingSession.Redo(), _selectedNodeId, markDirty: true, fitToViewport: false);
        SetStatusModified();
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
        _editingSession = new CompositionEditingSession(snapshot);
        _snapshotPath = snapshotPath;
        ApplySessionSnapshot(snapshot, selectedNodeId: null, markDirty: false, fitToViewport: true);

        CompositionTitleText.Text = snapshot.Title;
        StatusContextText.Text = $"Loaded {Path.GetFileName(snapshotPath)}";
        MessagesText.Text =
            $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
            $"Canvas renderer: loaded .tcview snapshot{Environment.NewLine}" +
            $"Document: {snapshot.Title}{Environment.NewLine}" +
            $"Nodes: {snapshot.Nodes.Count}, connectors: {snapshot.Connectors.Count}";
    }

    private void ApplySessionSnapshot(
        CompositionViewSnapshot snapshot,
        string? selectedNodeId,
        bool markDirty,
        bool fitToViewport)
    {
        _currentSnapshot = snapshot;
        _isDirty = markDirty;
        _snapshotIndex = CompositionSnapshotIndex.FromSnapshot(snapshot);

        UpdateExplorer(snapshot);
        CanvasView.LoadSnapshot(snapshot, selectedNodeId, fitToViewport);
        ApplySelectedNode(CanvasView.SelectedNode ?? _snapshotIndex.FirstNode);
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

    private void CanvasView_NodeMoved(object? sender, CompositionNodeView node)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        _currentSnapshot = CompositionSnapshotEditor.MoveNode(_currentSnapshot, node.Id, node.Position);
        _snapshotIndex = CompositionSnapshotIndex.FromSnapshot(_currentSnapshot);
        _isDirty = true;
        ApplySelectedNode(node);
        SetStatusModified();
    }

    private void CanvasView_NodeMoveCompleted(object? sender, CompositionNodeView node)
    {
        if (_editingSession is null || _currentSnapshot is null)
        {
            return;
        }

        _editingSession.Apply(_currentSnapshot);
    }

    private void InspectorNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyInspectorName();
    }

    private void InspectorNameBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
        {
            return;
        }

        ApplyInspectorName();
        e.Handled = true;
    }

    private void InspectorLayoutBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        ApplyInspectorLayout();
    }

    private void ApplySelectedNode(CompositionNodeView? node)
    {
        _isApplyingInspector = true;
        try
        {
        if (node is null)
        {
            _selectedNodeId = null;
            InspectorNameBox.Text = "No selection";
            InspectorNameBox.IsEnabled = false;
            InspectorXBox.IsEnabled = false;
            InspectorYBox.IsEnabled = false;
            InspectorWidthBox.IsEnabled = false;
            InspectorHeightBox.IsEnabled = false;
            InspectorXBox.Value = 0;
            InspectorYBox.Value = 0;
            InspectorWidthBox.Value = 0;
            InspectorHeightBox.Value = 0;
            OutgoingText.Text = "0";
            IncomingText.Text = "0";
            return;
        }

        _selectedNodeId = node.Id;
        InspectorNameBox.IsEnabled = true;
        InspectorXBox.IsEnabled = true;
        InspectorYBox.IsEnabled = true;
        InspectorWidthBox.IsEnabled = true;
        InspectorHeightBox.IsEnabled = true;
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
        finally
        {
            _isApplyingInspector = false;
        }
    }

    private void ApplyInspectorName()
    {
        if (_isApplyingInspector || _currentSnapshot is null || string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return;
        }

        var currentNode = FindNode(_selectedNodeId);
        var nextText = InspectorNameBox.Text.Trim();
        if (currentNode is null || string.Equals(currentNode.Text, nextText, StringComparison.Ordinal))
        {
            return;
        }

        ApplyEditedSnapshot(
            CompositionSnapshotEditor.RenameNode(_currentSnapshot, _selectedNodeId, nextText),
            _selectedNodeId,
            fitToViewport: false);
    }

    private void ApplyInspectorLayout()
    {
        if (_isApplyingInspector || _currentSnapshot is null || string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return;
        }

        if (double.IsNaN(InspectorXBox.Value) ||
            double.IsNaN(InspectorYBox.Value) ||
            double.IsNaN(InspectorWidthBox.Value) ||
            double.IsNaN(InspectorHeightBox.Value))
        {
            return;
        }

        var currentNode = FindNode(_selectedNodeId);
        if (currentNode is null)
        {
            return;
        }

        var position = new TcPoint(InspectorXBox.Value, InspectorYBox.Value);
        var size = new TcSize(InspectorWidthBox.Value, InspectorHeightBox.Value);
        var nextSnapshot = _currentSnapshot;

        if (!currentNode.Position.Equals(position))
        {
            nextSnapshot = CompositionSnapshotEditor.MoveNode(nextSnapshot, _selectedNodeId, position);
        }

        if (!currentNode.Size.Equals(size))
        {
            nextSnapshot = CompositionSnapshotEditor.ResizeNode(nextSnapshot, _selectedNodeId, size);
        }

        if (!ReferenceEquals(nextSnapshot, _currentSnapshot))
        {
            ApplyEditedSnapshot(nextSnapshot, _selectedNodeId, fitToViewport: false);
        }
    }

    private void ApplyEditedSnapshot(
        CompositionViewSnapshot snapshot,
        string? selectedNodeId,
        bool fitToViewport)
    {
        _editingSession?.Apply(snapshot);
        ApplySessionSnapshot(snapshot, selectedNodeId, markDirty: true, fitToViewport);
        SetStatusModified();
    }

    private CompositionNodeView? FindNode(string nodeId)
    {
        return _currentSnapshot?.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.Ordinal));
    }

    private void SetStatusModified()
    {
        if (!string.IsNullOrWhiteSpace(_snapshotPath))
        {
            StatusContextText.Text = $"Modified {Path.GetFileName(_snapshotPath)}";
        }
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
