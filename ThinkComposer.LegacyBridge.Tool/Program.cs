using System;
using Instrumind.ThinkComposer.LegacyBridge;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ThinkComposer.LegacyBridge.Tool <input.tdom|input.tcom> <output.tcview|output.tcdoc>");
    return 2;
}

try
{
    var outputKind = LegacyCompositionExportDispatcher.ExportToFile(args[0], args[1]);
    Console.WriteLine($"Exported {outputKind} to '{args[1]}'.");
    return 0;
}
catch (Exception problem)
{
    Console.Error.WriteLine(problem);
    return 1;
}
