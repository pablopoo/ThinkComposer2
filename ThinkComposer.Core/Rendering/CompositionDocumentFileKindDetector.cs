namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentFileKindDetector
{
    public static CompositionDocumentFileKind FromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return CompositionDocumentFileKind.Unknown;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".tcview" => CompositionDocumentFileKind.Snapshot,
            ".tcdoc" => CompositionDocumentFileKind.ModernDocument,
            ".tdom" or ".tcom" => CompositionDocumentFileKind.LegacyPackage,
            _ => CompositionDocumentFileKind.Unknown
        };
    }
}
