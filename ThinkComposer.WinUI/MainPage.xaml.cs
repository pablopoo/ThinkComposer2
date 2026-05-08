using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Instrumind.ThinkComposer.WinUI;

public sealed partial class MainPage : Page
{
    private static readonly GridLength ExplorerWidth = new(248);
    private static readonly GridLength InspectorWidth = new(286);
    private static readonly GridLength BottomHeight = new(148);

    private bool _isExplorerVisible = true;
    private bool _isInspectorVisible = true;
    private bool _isBottomVisible = true;
    private bool _isDarkTheme;

    public MainPage()
    {
        InitializeComponent();
    }

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
        RootPage.RequestedTheme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
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
}
