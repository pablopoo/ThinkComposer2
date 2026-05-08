using System;
using Instrumind.ThinkComposer.LegacyBridge;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ThinkComposer.LegacyBridge.Tool <input.tdom|input.tcom> <output.tcview>");
    return 2;
}

try
{
    var snapshot = LegacyCompositionSnapshotExporter.ExportToFile(args[0], args[1]);
    Console.WriteLine($"Exported '{snapshot.Title}' to '{args[1]}' ({snapshot.Nodes.Count} nodes, {snapshot.Connectors.Count} connectors).");
    return 0;
}
catch (Exception problem)
{
    Console.Error.WriteLine(problem.Message);
    return 1;
}
