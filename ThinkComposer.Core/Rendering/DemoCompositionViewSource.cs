using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class DemoCompositionViewSource
{
    public static CompositionViewSnapshot CreateSnapshot()
    {
        CompositionNodeView[] nodes =
        [
            new("customer", "Customer Need", new TcPoint(120, 132), new TcSize(164, 82)),
            new("capability", "Business Capability", new TcPoint(420, 190), new TcSize(184, 86)),
            new("service", "System Service", new TcPoint(690, 104), new TcSize(176, 88))
        ];

        CompositionConnectorView[] connectors =
        [
            new("customer-capability", "customer", "capability"),
            new("capability-service", "capability", "service")
        ];

        return new CompositionViewSnapshot("composition-1", "Composition 1", nodes, connectors);
    }
}
