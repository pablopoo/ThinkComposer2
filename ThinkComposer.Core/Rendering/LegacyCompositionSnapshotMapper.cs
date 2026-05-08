using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Instrumind.ThinkComposer.Core.Primitives;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class LegacyCompositionSnapshotMapper
{
    public static CompositionViewSnapshot FromComposition(object composition)
    {
        if (composition is null)
        {
            throw new ArgumentNullException(nameof(composition));
        }

        var view = ReadProperty(composition, "ActiveView")
            ?? ReadProperty(composition, "RootView")
            ?? throw new InvalidOperationException("Legacy composition has no active or root view.");

        var viewObjects = ReadViewKeys(view).ToList();
        var symbols = viewObjects
            .Where(item => IsOrInherits(item.GetType(), "VisualSymbol"))
            .ToList();

        var symbolIds = symbols.ToDictionary(
            symbol => symbol,
            ReadStableId,
            ObjectReferenceEqualityComparer.Instance);

        var nodes = symbols
            .Select(symbol => CreateNode(symbol, symbolIds[symbol]))
            .ToList();

        var connectors = viewObjects
            .Where(item => IsOrInherits(item.GetType(), "VisualConnector"))
            .Select(connector => TryCreateConnector(connector, symbolIds))
            .Where(connector => connector is not null)
            .Cast<CompositionConnectorView>()
            .ToList();

        return new CompositionViewSnapshot(
            ReadString(composition, "GlobalId") ?? "legacy-composition",
            ReadString(composition, "Name") ?? "Legacy Composition",
            nodes,
            connectors);
    }

    private static CompositionNodeView CreateNode(object symbol, string id)
    {
        var area = ReadProperty(symbol, "BaseArea")
            ?? throw new InvalidOperationException("Legacy visual symbol has no BaseArea.");

        var idea = ReadProperty(ReadProperty(symbol, "OwnerRepresentation"), "RepresentedIdea");
        var text = ReadString(idea, "Name")
            ?? ReadString(idea, "TechName")
            ?? id;

        return new CompositionNodeView(
            id,
            text,
            new TcPoint(ReadDouble(area, "X"), ReadDouble(area, "Y")),
            new TcSize(ReadDouble(area, "Width"), ReadDouble(area, "Height")));
    }

    private static CompositionConnectorView? TryCreateConnector(
        object connector,
        IReadOnlyDictionary<object, string> symbolIds)
    {
        var origin = ReadProperty(connector, "OriginSymbol");
        var target = ReadProperty(connector, "TargetSymbol");

        if (origin is null || target is null)
        {
            return null;
        }

        if (!symbolIds.TryGetValue(origin, out var sourceId) || !symbolIds.TryGetValue(target, out var targetId))
        {
            return null;
        }

        return new CompositionConnectorView(ReadStableId(connector), sourceId, targetId);
    }

    private static IEnumerable<object> ReadViewKeys(object view)
    {
        if (ReadProperty(view, "ViewChildren") is not IEnumerable children)
        {
            yield break;
        }

        foreach (var child in children)
        {
            var key = ReadProperty(child, "Key");
            if (key is not null)
            {
                yield return key;
            }
        }
    }

    private static object? ReadProperty(object? source, string propertyName)
    {
        return source?.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(source);
    }

    private static string? ReadString(object? source, string propertyName)
    {
        return ReadProperty(source, propertyName)?.ToString();
    }

    private static string ReadStableId(object source)
    {
        return ReadString(source, "GlobalId")
            ?? $"{source.GetType().Name}-{RuntimeHelpers.GetHashCode(source):x}";
    }

    private static double ReadDouble(object source, string propertyName)
    {
        var value = ReadProperty(source, propertyName)
            ?? throw new InvalidOperationException($"Legacy value has no {propertyName} property.");

        return Convert.ToDouble(value);
    }

    private static bool IsOrInherits(Type type, string typeName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == typeName)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ObjectReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ObjectReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
