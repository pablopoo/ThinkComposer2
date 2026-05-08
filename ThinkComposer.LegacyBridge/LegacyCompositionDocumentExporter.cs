using Instrumind.ThinkComposer.Core.Rendering;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionDocumentExporter
{
    public static CompositionDocumentSnapshot ExportToFile(string legacyDocumentPath, string documentPath)
    {
        var document = LegacyCompositionDocumentLoader.LoadFromFile(legacyDocumentPath);
        CompositionDocumentSnapshotXmlStore.Save(document, documentPath);
        return document;
    }
}
