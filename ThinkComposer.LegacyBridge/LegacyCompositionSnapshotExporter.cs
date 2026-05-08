using Instrumind.ThinkComposer.Core.Rendering;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionSnapshotExporter
{
    public static CompositionViewSnapshot ExportToFile(string legacyDocumentPath, string snapshotPath)
    {
        var snapshot = LegacyCompositionSnapshotLoader.LoadFromFile(legacyDocumentPath);
        CompositionViewSnapshotXmlStore.Save(snapshot, snapshotPath);
        return snapshot;
    }
}
