using Microsoft.UI.Xaml;
using Windows.UI;

namespace Instrumind.ThinkComposer.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        RootFrame.Navigate(typeof(MainPage));

        var initialTheme = ElementTheme.Light;
        if (RootFrame.Content is MainPage page)
        {
            page.AppThemeChanged += (_, theme) => ApplyTheme(theme);
            initialTheme = page.CurrentTheme;
        }

        ApplyTheme(initialTheme);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        WindowRoot.RequestedTheme = theme;
        RootFrame.RequestedTheme = theme;

        var titleBar = AppWindow.TitleBar;
        if (theme == ElementTheme.Dark)
        {
            titleBar.ButtonForegroundColor = Rgb(204, 204, 204);
            titleBar.ButtonInactiveForegroundColor = Rgb(120, 120, 120);
            titleBar.ButtonBackgroundColor = Transparent();
            titleBar.ButtonInactiveBackgroundColor = Transparent();
            titleBar.ButtonHoverBackgroundColor = Rgb(51, 51, 51);
            titleBar.ButtonPressedBackgroundColor = Rgb(62, 62, 66);
            return;
        }

        titleBar.ButtonForegroundColor = Rgb(36, 36, 36);
        titleBar.ButtonInactiveForegroundColor = Rgb(96, 96, 96);
        titleBar.ButtonBackgroundColor = Transparent();
        titleBar.ButtonInactiveBackgroundColor = Transparent();
        titleBar.ButtonHoverBackgroundColor = Rgb(229, 229, 229);
        titleBar.ButtonPressedBackgroundColor = Rgb(214, 214, 214);
    }

    private static Color Rgb(byte red, byte green, byte blue)
    {
        return Color.FromArgb(255, red, green, blue);
    }

    private static Color Transparent()
    {
        return Color.FromArgb(0, 0, 0, 0);
    }
}
