using System.Diagnostics;
using Instrumind.ThinkComposer.Core.Primitives;
using Instrumind.ThinkComposer.Core.Rendering;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Instrumind.ThinkComposer.WinUI;

public sealed partial class MainPage : Page
{
    private enum BottomPanelTab
    {
        Messages,
        Search,
        Preview,
        Diagnostics
    }

    private sealed record ExplorerTreeEntry(
        string Title,
        CompositionCommandEntryKind Kind,
        string? TargetId = null)
    {
        public override string ToString()
        {
            return Title;
        }
    }

    private static readonly GridLength ExplorerWidth = new(248);
    private static readonly GridLength InspectorWidth = new(320);
    private static readonly GridLength BottomHeight = new(148);

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ThinkComposer",
        "workspace-settings.xml");

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
    private string? _selectedConnectorId;
    private string? _pendingRelationshipSourceId;
    private bool _isApplyingInspector;
    private BottomPanelTab _bottomPanelTab = BottomPanelTab.Messages;
    private IReadOnlyList<CompositionCommandEntry> _commandEntries = Array.Empty<CompositionCommandEntry>();

    public MainPage()
    {
        InitializeComponent();
        CanvasView.SelectedNodeChanged += CanvasView_SelectedNodeChanged;
        CanvasView.SelectedConnectorChanged += CanvasView_SelectedConnectorChanged;
        CanvasView.NodeMoved += CanvasView_NodeMoved;
        CanvasView.NodeMoveCompleted += CanvasView_NodeMoveCompleted;
        LoadWorkspaceSettings();
        LoadStartupSnapshot();
        RefreshBottomPanelContent();
    }

    public event EventHandler<ElementTheme>? AppThemeChanged;

    public ElementTheme CurrentTheme => _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

    private void ExplorerButton_Click(object sender, RoutedEventArgs e)
    {
        _isExplorerVisible = !_isExplorerVisible;
        ApplyPanelState();
        SaveWorkspaceSettings();
    }

    private void InspectorButton_Click(object sender, RoutedEventArgs e)
    {
        _isInspectorVisible = !_isInspectorVisible;
        ApplyPanelState();
        SaveWorkspaceSettings();
    }

    private void BottomButton_Click(object sender, RoutedEventArgs e)
    {
        _isBottomVisible = !_isBottomVisible;
        ApplyPanelState();
        SaveWorkspaceSettings();
    }

    private void SearchRailButton_Click(object sender, RoutedEventArgs e)
    {
        ShowBottomTab(BottomPanelTab.Search);
        BottomSearchBox.Focus(FocusState.Programmatic);
    }

    private void MessagesTabButton_Click(object sender, RoutedEventArgs e)
    {
        ShowBottomTab(BottomPanelTab.Messages);
    }

    private void SearchTabButton_Click(object sender, RoutedEventArgs e)
    {
        ShowBottomTab(BottomPanelTab.Search);
        BottomSearchBox.Focus(FocusState.Programmatic);
    }

    private void PreviewTabButton_Click(object sender, RoutedEventArgs e)
    {
        ShowBottomTab(BottomPanelTab.Preview);
    }

    private void DiagnosticsTabButton_Click(object sender, RoutedEventArgs e)
    {
        ShowBottomTab(BottomPanelTab.Diagnostics);
    }

    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        var showPanels = !(_isExplorerVisible || _isInspectorVisible || _isBottomVisible);
        _isExplorerVisible = showPanels;
        _isInspectorVisible = showPanels;
        _isBottomVisible = showPanels;
        ApplyPanelState();
        SaveWorkspaceSettings();
        DispatcherQueue.TryEnqueue(() => CanvasView.FitSnapshotToViewport());
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        var theme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        RootPage.RequestedTheme = theme;
        AppThemeChanged?.Invoke(this, theme);
        SaveWorkspaceSettings();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            MessagesText.Text =
                $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
                "No snapshot is available to save.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_snapshotPath))
        {
            SaveAsButton_Click(sender, e);
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

    private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySnapshot(CompositionDocumentFactory.CreateEmpty("Untitled"), snapshotPath: null);
        StatusContextText.Text = "New document";
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializePicker(picker);
        picker.FileTypeFilter.Add(".tcview");
        picker.FileTypeFilter.Add(".tdom");
        picker.FileTypeFilter.Add(".tcom");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        await LoadDocumentFromPathAsync(file.Path);
    }

    private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        var picker = new FileSavePicker();
        InitializePicker(picker);
        picker.FileTypeChoices.Add("ThinkComposer view", [".tcview"]);
        picker.SuggestedFileName = string.IsNullOrWhiteSpace(_currentSnapshot.Title) ? "Untitled" : _currentSnapshot.Title;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        SaveSnapshotToPath(file.Path);
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        var picker = new FileSavePicker();
        InitializePicker(picker);
        picker.FileTypeChoices.Add("Printable HTML", [".html"]);
        picker.FileTypeChoices.Add("SVG image", [".svg"]);
        picker.SuggestedFileName = string.IsNullOrWhiteSpace(_currentSnapshot.Title) ? "Untitled" : _currentSnapshot.Title;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        try
        {
            var extension = Path.GetExtension(file.Path);
            var content = string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase)
                ? CompositionSnapshotSvgExporter.Export(_currentSnapshot)
                : CompositionSnapshotHtmlExporter.Export(_currentSnapshot);

            await FileIO.WriteTextAsync(file, content);
            StatusContextText.Text = $"Exported {Path.GetFileName(file.Path)}";
            MessagesText.Text =
                $"Document exported{Environment.NewLine}" +
                $"Path: {file.Path}";
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"Could not export document{Environment.NewLine}" +
                problem.Message;
        }
    }

    private async void PrintPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        try
        {
            var previewPath = Path.Combine(
                Path.GetTempPath(),
                $"thinkcomposer-print-preview-{Guid.NewGuid():N}.html");
            File.WriteAllText(previewPath, CompositionSnapshotHtmlExporter.Export(_currentSnapshot));

            var previewFile = await StorageFile.GetFileFromPathAsync(previewPath);
            var launched = await Launcher.LaunchFileAsync(previewFile);
            StatusContextText.Text = launched ? "Print preview opened" : "Print preview not opened";
            MessagesText.Text =
                $"Printable preview generated{Environment.NewLine}" +
                $"Path: {previewPath}";
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"Could not open print preview{Environment.NewLine}" +
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

    private void NewRelationshipButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            StatusContextText.Text = "Select a source concept first";
            return;
        }

        _pendingRelationshipSourceId = _selectedNodeId;
        StatusContextText.Text = "Select target concept";
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_selectedConnectorId))
        {
            ApplyEditedSnapshot(
                CompositionSnapshotEditor.DeleteConnector(_currentSnapshot, _selectedConnectorId),
                selectedNodeId: null,
                fitToViewport: false,
                selectedConnectorId: null);
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedNodeId))
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

        ApplySessionSnapshot(_editingSession.Undo(), _selectedNodeId, _selectedConnectorId, markDirty: true, fitToViewport: false);
        SetStatusModified();
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSession is null || !_editingSession.CanRedo)
        {
            return;
        }

        ApplySessionSnapshot(_editingSession.Redo(), _selectedNodeId, _selectedConnectorId, markDirty: true, fitToViewport: false);
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
        ApplyBottomTabState();
    }

    private void ShowBottomTab(BottomPanelTab tab, bool persist = true)
    {
        _bottomPanelTab = tab;
        _isBottomVisible = true;
        ApplyPanelState();
        RefreshBottomPanelContent();
        if (persist)
        {
            SaveWorkspaceSettings();
        }
    }

    private void ApplyBottomTabState()
    {
        MessagesPanelContent.Visibility = _bottomPanelTab == BottomPanelTab.Messages ? Visibility.Visible : Visibility.Collapsed;
        SearchPanelContent.Visibility = _bottomPanelTab == BottomPanelTab.Search ? Visibility.Visible : Visibility.Collapsed;
        PreviewPanelContent.Visibility = _bottomPanelTab == BottomPanelTab.Preview ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticsPanelContent.Visibility = _bottomPanelTab == BottomPanelTab.Diagnostics ? Visibility.Visible : Visibility.Collapsed;

        ApplyBottomTabButtonState(MessagesTabButton, _bottomPanelTab == BottomPanelTab.Messages);
        ApplyBottomTabButtonState(SearchTabButton, _bottomPanelTab == BottomPanelTab.Search);
        ApplyBottomTabButtonState(PreviewTabButton, _bottomPanelTab == BottomPanelTab.Preview);
        ApplyBottomTabButtonState(DiagnosticsTabButton, _bottomPanelTab == BottomPanelTab.Diagnostics);
    }

    private static void ApplyBottomTabButtonState(Button button, bool isSelected)
    {
        button.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
        button.Opacity = isSelected ? 1 : 0.72;
    }

    private void RefreshBottomPanelContent()
    {
        RefreshSearchResults();
        RefreshPreview();
        RefreshDiagnostics();
    }

    private void RefreshSearchResults()
    {
        if (SearchResultsList is null)
        {
            return;
        }

        SearchResultsList.ItemsSource = CompositionCommandCatalog.Search(_commandEntries, BottomSearchBox.Text, limit: 50);
    }

    private void RefreshPreview()
    {
        PreviewText.Text = _currentSnapshot is null
            ? "No document loaded."
            : CompositionSnapshotPreviewTextBuilder.Build(_currentSnapshot);
    }

    private void RefreshDiagnostics()
    {
        var selection = !string.IsNullOrWhiteSpace(_selectedNodeId)
            ? $"Node: {_selectedNodeId}"
            : !string.IsNullOrWhiteSpace(_selectedConnectorId)
                ? $"Relationship: {_selectedConnectorId}"
                : "None";

        DiagnosticsText.Text =
            $"Document: {_currentSnapshot?.Title ?? "(none)"}{Environment.NewLine}" +
            $"Path: {_snapshotPath ?? "(unsaved)"}{Environment.NewLine}" +
            $"Dirty: {_isDirty}{Environment.NewLine}" +
            $"Nodes: {_currentSnapshot?.Nodes.Count ?? 0}{Environment.NewLine}" +
            $"Relationships: {_currentSnapshot?.Connectors.Count ?? 0}{Environment.NewLine}" +
            $"Selection: {selection}{Environment.NewLine}" +
            $"Explorer: {_isExplorerVisible}{Environment.NewLine}" +
            $"Inspector: {_isInspectorVisible}{Environment.NewLine}" +
            $"Bottom panel: {_isBottomVisible}";
    }

    private void LoadWorkspaceSettings()
    {
        var settings = CompositionWorkspaceSettingsXmlStore.LoadOrDefault(_settingsPath);
        _isExplorerVisible = settings.IsExplorerVisible;
        _isInspectorVisible = settings.IsInspectorVisible;
        _isBottomVisible = settings.IsBottomVisible;
        _isDarkTheme = settings.Theme == CompositionWorkspaceTheme.Dark;
        RootPage.RequestedTheme = CurrentTheme;
        ApplyPanelState();
    }

    private void SaveWorkspaceSettings()
    {
        try
        {
            var settings = new CompositionWorkspaceSettings(
                _isDarkTheme ? CompositionWorkspaceTheme.Dark : CompositionWorkspaceTheme.Light,
                _isExplorerVisible,
                _isInspectorVisible,
                _isBottomVisible);
            CompositionWorkspaceSettingsXmlStore.Save(settings, _settingsPath);
        }
        catch (Exception problem)
        {
            Debug.WriteLine(problem);
        }
    }

    private void LoadStartupSnapshot()
    {
        var snapshotPath = FindStartupSnapshotPath();
        if (snapshotPath is null)
        {
            return;
        }

        LoadSnapshotFromPath(snapshotPath);
    }

    private void LoadSnapshotFromPath(string snapshotPath)
    {
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

    private async Task LoadDocumentFromPathAsync(string filePath)
    {
        switch (CompositionDocumentFileKindDetector.FromPath(filePath))
        {
            case CompositionDocumentFileKind.Snapshot:
                LoadSnapshotFromPath(filePath);
                return;
            case CompositionDocumentFileKind.LegacyPackage:
                await ImportLegacyPackageAsync(filePath);
                return;
            default:
                MessagesText.Text =
                    $"Unsupported document type{Environment.NewLine}" +
                    filePath;
                return;
        }
    }

    private async Task ImportLegacyPackageAsync(string legacyPath)
    {
        var toolPath = FindLegacyBridgeToolPath();
        if (toolPath is null)
        {
            MessagesText.Text =
                $"Could not import legacy document{Environment.NewLine}" +
                "Legacy bridge tool was not found.";
            return;
        }

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"thinkcomposer-import-{Path.GetFileNameWithoutExtension(legacyPath)}-{Guid.NewGuid():N}.tcview");
        var startInfo = new ProcessStartInfo(toolPath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(legacyPath);
        startInfo.ArgumentList.Add(outputPath);

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            MessagesText.Text = "Could not start legacy bridge tool.";
            return;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            MessagesText.Text =
                $"Could not import legacy document{Environment.NewLine}" +
                error.Trim();
            return;
        }

        LoadSnapshotFromPath(outputPath);
        StatusContextText.Text = $"Imported {Path.GetFileName(legacyPath)}";
        MessagesText.Text =
            $"Legacy document imported{Environment.NewLine}" +
            output.Trim();
    }

    private void SaveSnapshotToPath(string snapshotPath)
    {
        if (_currentSnapshot is null)
        {
            return;
        }

        try
        {
            CompositionViewSnapshotXmlStore.Save(_currentSnapshot, snapshotPath);
            _snapshotPath = snapshotPath;
            _isDirty = false;
            StatusContextText.Text = $"Saved {Path.GetFileName(snapshotPath)}";
            MessagesText.Text =
                $"Snapshot saved{Environment.NewLine}" +
                $"Document: {_currentSnapshot.Title}{Environment.NewLine}" +
                $"Path: {snapshotPath}";
        }
        catch (Exception problem)
        {
            MessagesText.Text =
                $"Could not save snapshot{Environment.NewLine}" +
                problem.Message;
        }
    }

    private static void InitializePicker(object picker)
    {
        var window = App.MainWindowInstance;
        if (window is null)
        {
            return;
        }

        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
    }

    private void ApplySnapshot(CompositionViewSnapshot snapshot, string? snapshotPath)
    {
        _editingSession = new CompositionEditingSession(snapshot);
        _snapshotPath = snapshotPath;
        ApplySessionSnapshot(snapshot, selectedNodeId: null, selectedConnectorId: null, markDirty: false, fitToViewport: true);

        CompositionTitleText.Text = snapshot.Title;
        StatusContextText.Text = snapshotPath is null ? "New document" : $"Loaded {Path.GetFileName(snapshotPath)}";
        MessagesText.Text =
            $"ThinkComposer WinUI shell loaded{Environment.NewLine}" +
            $"Canvas renderer: loaded snapshot{Environment.NewLine}" +
            $"Document: {snapshot.Title}{Environment.NewLine}" +
            $"Nodes: {snapshot.Nodes.Count}, connectors: {snapshot.Connectors.Count}";
        RefreshCommandCatalog();
        RefreshBottomPanelContent();
    }

    private void ApplySessionSnapshot(
        CompositionViewSnapshot snapshot,
        string? selectedNodeId,
        string? selectedConnectorId,
        bool markDirty,
        bool fitToViewport)
    {
        _currentSnapshot = snapshot;
        _isDirty = markDirty;
        _snapshotIndex = CompositionSnapshotIndex.FromSnapshot(snapshot);

        UpdateExplorer(snapshot);
        CanvasView.LoadSnapshot(snapshot, selectedNodeId, fitToViewport, selectedConnectorId);
        if (CanvasView.SelectedConnector is not null)
        {
            ApplySelectedConnector(CanvasView.SelectedConnector);
        }
        else
        {
            ApplySelectedNode(CanvasView.SelectedNode ?? _snapshotIndex.FirstNode);
        }
        RefreshCommandCatalog();
        RefreshBottomPanelContent();
    }

    private void UpdateExplorer(CompositionViewSnapshot snapshot)
    {
        ExplorerTree.RootNodes.Clear();

        var root = new TreeViewNode
        {
            Content = new ExplorerTreeEntry($"{snapshot.Title} ({snapshot.Nodes.Count})", CompositionCommandEntryKind.Command),
            IsExpanded = true
        };

        foreach (var node in snapshot.Nodes)
        {
            root.Children.Add(new TreeViewNode
            {
                Content = new ExplorerTreeEntry(GetNodeTitle(node), CompositionCommandEntryKind.Node, node.Id)
            });
        }

        ExplorerTree.RootNodes.Add(root);

        var relations = new TreeViewNode
        {
            Content = new ExplorerTreeEntry($"Relationships ({snapshot.Connectors.Count})", CompositionCommandEntryKind.Command),
            IsExpanded = true
        };
        foreach (var connector in snapshot.Connectors)
        {
            relations.Children.Add(new TreeViewNode
            {
                Content = new ExplorerTreeEntry(GetConnectorTitle(connector), CompositionCommandEntryKind.Connector, connector.Id)
            });
        }

        ExplorerTree.RootNodes.Add(relations);
    }

    private void ExplorerTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedNode?.Content is not ExplorerTreeEntry entry ||
            string.IsNullOrWhiteSpace(entry.TargetId))
        {
            return;
        }

        switch (entry.Kind)
        {
            case CompositionCommandEntryKind.Node:
                CanvasView.SelectNode(entry.TargetId);
                StatusContextText.Text = $"Selected {entry.Title}";
                break;
            case CompositionCommandEntryKind.Connector:
                CanvasView.SelectConnector(entry.TargetId);
                StatusContextText.Text = $"Selected {entry.Title}";
                break;
        }
    }

    private void CanvasView_SelectedNodeChanged(object? sender, CompositionNodeView? node)
    {
        if (TryCompletePendingRelationship(node))
        {
            return;
        }

        ApplySelectedNode(node);
    }

    private void CanvasView_SelectedConnectorChanged(object? sender, CompositionConnectorView? connector)
    {
        ApplySelectedConnector(connector);
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
            if (string.IsNullOrWhiteSpace(_selectedConnectorId))
            {
                _pendingRelationshipSourceId = null;
            }
            ObjectExpander.Header = "Selection";
            InspectorNameBox.Text = "No selection";
            InspectorNameBox.IsEnabled = false;
            InspectorXBox.IsEnabled = false;
            InspectorYBox.IsEnabled = false;
            InspectorWidthBox.IsEnabled = false;
            InspectorHeightBox.IsEnabled = false;
            InspectorRelationshipButton.IsEnabled = false;
            InspectorDeleteButton.IsEnabled = false;
            InspectorXBox.Value = 0;
            InspectorYBox.Value = 0;
            InspectorWidthBox.Value = 0;
            InspectorHeightBox.Value = 0;
            OutgoingLabel.Text = "Outgoing";
            IncomingLabel.Text = "Incoming";
            OutgoingText.Text = "0";
            IncomingText.Text = "0";
            return;
        }

        _selectedNodeId = node.Id;
        _selectedConnectorId = null;
        ObjectExpander.Header = "Concept";
        InspectorNameBox.IsEnabled = true;
        InspectorXBox.IsEnabled = true;
        InspectorYBox.IsEnabled = true;
        InspectorWidthBox.IsEnabled = true;
        InspectorHeightBox.IsEnabled = true;
        InspectorRelationshipButton.IsEnabled = true;
        InspectorDeleteButton.IsEnabled = true;
        InspectorNameBox.Text = GetNodeTitle(node);
        InspectorKindBox.SelectedIndex = 0;
        InspectorStatusBox.SelectedIndex = 0;
        InspectorXBox.Value = Math.Round(node.Position.X, 1);
        InspectorYBox.Value = Math.Round(node.Position.Y, 1);
        InspectorWidthBox.Value = Math.Round(node.Size.Width, 1);
        InspectorHeightBox.Value = Math.Round(node.Size.Height, 1);
        OutgoingLabel.Text = "Outgoing";
        IncomingLabel.Text = "Incoming";
        OutgoingText.Text = (_snapshotIndex?.CountOutgoing(node.Id) ?? 0).ToString();
        IncomingText.Text = (_snapshotIndex?.CountIncoming(node.Id) ?? 0).ToString();
        }
        finally
        {
            _isApplyingInspector = false;
            RefreshDiagnostics();
        }
    }

    private void ApplySelectedConnector(CompositionConnectorView? connector)
    {
        _isApplyingInspector = true;
        try
        {
            if (connector is null)
            {
                _selectedConnectorId = null;
                return;
            }

            _selectedNodeId = null;
            _selectedConnectorId = connector.Id;
            _pendingRelationshipSourceId = null;
            ObjectExpander.Header = "Relationship";
            InspectorNameBox.IsEnabled = true;
            InspectorXBox.IsEnabled = false;
            InspectorYBox.IsEnabled = false;
            InspectorWidthBox.IsEnabled = false;
            InspectorHeightBox.IsEnabled = false;
            InspectorRelationshipButton.IsEnabled = false;
            InspectorDeleteButton.IsEnabled = true;
            InspectorNameBox.Text = connector.Text;
            InspectorKindBox.SelectedIndex = 0;
            InspectorStatusBox.SelectedIndex = 0;
            InspectorXBox.Value = 0;
            InspectorYBox.Value = 0;
            InspectorWidthBox.Value = 0;
            InspectorHeightBox.Value = 0;
            OutgoingLabel.Text = "Source";
            IncomingLabel.Text = "Target";
            OutgoingText.Text = connector.SourceId;
            IncomingText.Text = connector.TargetId;
        }
        finally
        {
            _isApplyingInspector = false;
            RefreshDiagnostics();
        }
    }

    private void CommandSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        sender.ItemsSource = CompositionCommandCatalog.Search(_commandEntries, sender.Text);
    }

    private void CommandSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is CompositionCommandEntry entry)
        {
            sender.Text = entry.Title;
        }
    }

    private void CommandSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var entry = args.ChosenSuggestion as CompositionCommandEntry
            ?? CompositionCommandCatalog.Search(_commandEntries, args.QueryText, limit: 1).FirstOrDefault();

        if (entry is null)
        {
            return;
        }

        ExecuteCommandEntry(entry);
        sender.Text = string.Empty;
        sender.ItemsSource = null;
    }

    private void BottomSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshSearchResults();
    }

    private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is not CompositionCommandEntry entry)
        {
            return;
        }

        SearchResultsList.SelectedItem = null;
        ExecuteCommandEntry(entry);
    }

    private void ApplyInspectorName()
    {
        if (_isApplyingInspector || _currentSnapshot is null)
        {
            return;
        }

        var nextText = InspectorNameBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(_selectedConnectorId))
        {
            var currentConnector = FindConnector(_selectedConnectorId);
            if (currentConnector is null || string.Equals(currentConnector.Text, nextText, StringComparison.Ordinal))
            {
                return;
            }

            ApplyEditedSnapshot(
                CompositionSnapshotEditor.RenameConnector(_currentSnapshot, _selectedConnectorId, nextText),
                selectedNodeId: null,
                fitToViewport: false,
                selectedConnectorId: _selectedConnectorId);
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return;
        }

        var currentNode = FindNode(_selectedNodeId);
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
        bool fitToViewport,
        string? selectedConnectorId = null)
    {
        _editingSession?.Apply(snapshot);
        ApplySessionSnapshot(snapshot, selectedNodeId, selectedConnectorId, markDirty: true, fitToViewport);
        SetStatusModified();
    }

    private CompositionNodeView? FindNode(string nodeId)
    {
        return _currentSnapshot?.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.Ordinal));
    }

    private CompositionConnectorView? FindConnector(string connectorId)
    {
        return _currentSnapshot?.Connectors.FirstOrDefault(connector => string.Equals(connector.Id, connectorId, StringComparison.Ordinal));
    }

    private bool TryCompletePendingRelationship(CompositionNodeView? targetNode)
    {
        if (_currentSnapshot is null ||
            targetNode is null ||
            string.IsNullOrWhiteSpace(_pendingRelationshipSourceId))
        {
            return false;
        }

        if (string.Equals(_pendingRelationshipSourceId, targetNode.Id, StringComparison.Ordinal))
        {
            StatusContextText.Text = "Select a different target concept";
            return true;
        }

        var connector = new CompositionConnectorView(
            Guid.NewGuid().ToString(),
            _pendingRelationshipSourceId,
            targetNode.Id,
            "Relationship");
        _pendingRelationshipSourceId = null;
        ApplyEditedSnapshot(
            CompositionSnapshotEditor.CreateConnector(_currentSnapshot, connector),
            selectedNodeId: null,
            fitToViewport: false,
            selectedConnectorId: connector.Id);
        return true;
    }

    private void SetStatusModified()
    {
        if (!string.IsNullOrWhiteSpace(_snapshotPath))
        {
            StatusContextText.Text = $"Modified {Path.GetFileName(_snapshotPath)}";
        }

        RefreshDiagnostics();
    }

    private void ExecuteCommandEntry(CompositionCommandEntry entry)
    {
        switch (entry.Kind)
        {
            case CompositionCommandEntryKind.Node when !string.IsNullOrWhiteSpace(entry.TargetId):
                CanvasView.SelectNode(entry.TargetId);
                StatusContextText.Text = $"Selected {entry.Title}";
                return;
            case CompositionCommandEntryKind.Connector when !string.IsNullOrWhiteSpace(entry.TargetId):
                CanvasView.SelectConnector(entry.TargetId);
                StatusContextText.Text = $"Selected {entry.Title}";
                return;
            case CompositionCommandEntryKind.Command:
                ExecuteCommand(entry.Id);
                return;
        }
    }

    private void ExecuteCommand(string commandId)
    {
        switch (commandId)
        {
            case CompositionCommandIds.NewDocument:
                NewDocumentButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.Open:
                OpenButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.Save:
                SaveButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.SaveAs:
                SaveAsButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.ExportHtml:
                ExportButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.PrintPreview:
                PrintPreviewButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.NewConcept:
                NewConceptButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.NewRelationship:
                NewRelationshipButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.Delete:
                DeleteButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.Undo:
                UndoButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.Redo:
                RedoButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.FocusCanvas:
                FocusButton_Click(this, new RoutedEventArgs());
                break;
            case CompositionCommandIds.ToggleTheme:
                ThemeButton_Click(this, new RoutedEventArgs());
                break;
        }
    }

    private void RefreshCommandCatalog()
    {
        _commandEntries = CompositionCommandCatalog.ForSnapshot(_currentSnapshot);
    }

    private static string GetNodeTitle(CompositionNodeView node)
    {
        return string.IsNullOrWhiteSpace(node.Text) ? node.Id : node.Text;
    }

    private static string GetConnectorTitle(CompositionConnectorView connector)
    {
        return string.IsNullOrWhiteSpace(connector.Text) ? connector.Id : connector.Text;
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

    private static string? FindLegacyBridgeToolPath()
    {
        return EnumerateAncestorDirectories(AppContext.BaseDirectory)
            .Concat(EnumerateAncestorDirectories(Environment.CurrentDirectory))
            .Select(directory => Path.Combine(
                directory,
                "ThinkComposer.LegacyBridge.Tool",
                "bin",
                "x86",
                "Debug",
                "net48",
                "ThinkComposer.LegacyBridge.Tool.exe"))
            .FirstOrDefault(File.Exists);
    }
}
