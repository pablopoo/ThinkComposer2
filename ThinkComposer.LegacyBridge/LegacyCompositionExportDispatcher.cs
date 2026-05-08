using System;
using System.IO;
using Instrumind.ThinkComposer.Core.Rendering;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionExportDispatcher
{
    public static CompositionDocumentFileKind ExportToFile(string legacyDocumentPath, string outputPath)
    {
        var outputKind = CompositionDocumentFileKindDetector.FromPath(outputPath);

        switch (outputKind)
        {
            case CompositionDocumentFileKind.Snapshot:
                LegacyCompositionSnapshotExporter.ExportToFile(legacyDocumentPath, outputPath);
                return outputKind;
            case CompositionDocumentFileKind.ModernDocument:
                LegacyCompositionDocumentExporter.ExportToFile(legacyDocumentPath, outputPath);
                return outputKind;
            default:
                throw new InvalidOperationException($"Unsupported export output extension: {Path.GetExtension(outputPath)}");
        }
    }
}
