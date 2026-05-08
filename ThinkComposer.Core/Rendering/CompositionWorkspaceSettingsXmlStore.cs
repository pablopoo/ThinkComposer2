using System.Xml.Linq;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionWorkspaceSettingsXmlStore
{
    public static CompositionWorkspaceSettings LoadOrDefault(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return CompositionWorkspaceSettings.Default;
        }

        try
        {
            var root = XDocument.Load(filePath).Root;
            if (root is null)
            {
                return CompositionWorkspaceSettings.Default;
            }

            return new CompositionWorkspaceSettings(
                Enum.TryParse(root.Attribute("theme")?.Value, ignoreCase: true, out CompositionWorkspaceTheme theme)
                    ? theme
                    : CompositionWorkspaceSettings.Default.Theme,
                ReadBool(root, "explorer", CompositionWorkspaceSettings.Default.IsExplorerVisible),
                ReadBool(root, "inspector", CompositionWorkspaceSettings.Default.IsInspectorVisible),
                ReadBool(root, "bottom", CompositionWorkspaceSettings.Default.IsBottomVisible));
        }
        catch
        {
            return CompositionWorkspaceSettings.Default;
        }
    }

    public static void Save(CompositionWorkspaceSettings settings, string filePath)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Settings path is required.", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        new XDocument(
            new XElement(
                "CompositionWorkspaceSettings",
                new XAttribute("theme", settings.Theme),
                new XAttribute("explorer", settings.IsExplorerVisible),
                new XAttribute("inspector", settings.IsInspectorVisible),
                new XAttribute("bottom", settings.IsBottomVisible)))
            .Save(filePath);
    }

    private static bool ReadBool(XElement element, string attributeName, bool defaultValue)
    {
        return bool.TryParse(element.Attribute(attributeName)?.Value, out var value) ? value : defaultValue;
    }
}
