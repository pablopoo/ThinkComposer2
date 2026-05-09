using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using Instrumind.Common;
using Instrumind.Common.EntityBase;
using Instrumind.Common.Visualization;
using Instrumind.ThinkComposer.Composer;
using Instrumind.ThinkComposer.Core.Primitives;
using Instrumind.ThinkComposer.Core.Rendering;
using Instrumind.ThinkComposer.MetaModel;
using Instrumind.ThinkComposer.MetaModel.Configurations;
using Instrumind.ThinkComposer.MetaModel.GraphMetaModel;
using Instrumind.ThinkComposer.MetaModel.InformationMetaModel;
using Instrumind.ThinkComposer.MetaModel.VisualMetaModel;
using Instrumind.ThinkComposer.Model;
using Instrumind.ThinkComposer.Model.GraphModel;
using Instrumind.ThinkComposer.Model.InformationModel;
using Instrumind.ThinkComposer.Model.VisualModel;

namespace Instrumind.ThinkComposer.LegacyBridge;

public static class LegacyCompositionDocumentLoader
{
    private static readonly Uri CompositionPartUri = new("/Composition.bin", UriKind.Relative);
    private static readonly Uri DomainPartUri = new("/Domain.bin", UriKind.Relative);

    public static CompositionDocumentSnapshot LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Legacy document path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Legacy document file was not found.", filePath);
        }

        var packageBytes = File.ReadAllBytes(filePath);
        var content = LoadPackageContent(filePath, out var packageTitle, out var packageId);

        if (content is Composition composition)
        {
            return FromComposition(PrepareComposition(composition), packageTitle, packageId, filePath, packageBytes);
        }

        if (content is Domain domain)
        {
            TryApplyModelFixes(domain);

            if (domain.OwnerComposition is not null)
            {
                return FromComposition(PrepareComposition(domain.OwnerComposition), packageTitle ?? domain.Name, packageId, filePath, packageBytes);
            }

            return FromDomain(domain, packageTitle, packageId, filePath, packageBytes);
        }

        throw new InvalidOperationException($"Unsupported legacy document content: {content.GetType().FullName}");
    }

    private static CompositionDocumentSnapshot FromComposition(
        Composition composition,
        string? packageTitle,
        Guid? packageId,
        string filePath,
        byte[] packageBytes)
    {
        var viewSnapshot = LegacyCompositionSnapshotMapper.FromComposition(composition);
        if (!string.IsNullOrWhiteSpace(packageTitle))
        {
            viewSnapshot = viewSnapshot with { Title = packageTitle! };
        }

        var visualObjects = ReadViewObjects(composition.ActiveView ?? composition.RootView).ToArray();
        var symbolsById = visualObjects
            .OfType<VisualSymbol>()
            .GroupBy(symbol => symbol.GlobalId.ToString())
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var connectorRelationships = visualObjects
            .OfType<VisualConnector>()
            .GroupBy(connector => connector.GlobalId.ToString())
            .ToDictionary(group => group.Key, group => group.First().OwnerRelationshipRepresentation.RepresentedRelationship, StringComparer.Ordinal);

        var ideas = viewSnapshot.Nodes
            .Select(node => MapIdea(node, symbolsById.TryGetValue(node.Id, out var symbol) ? symbol : null))
            .ToArray();

        var relationships = viewSnapshot.Connectors
            .Select(connector => MapRelationship(
                connector,
                connectorRelationships.TryGetValue(connector.Id, out var relationship) ? relationship : null))
            .ToArray();

        return new CompositionDocumentSnapshot(
            Id: (packageId ?? composition.GlobalId).ToString(),
            Title: viewSnapshot.Title,
            Domain: MapDomain(composition.CompositionDefinitor),
            Ideas: ideas,
            Relationships: relationships,
            Views: MapViews(composition, viewSnapshot).ToArray(),
            Extensions: BuildPackageExtensions(filePath, packageBytes));
    }

    private static CompositionDocumentSnapshot FromDomain(
        Domain domain,
        string? packageTitle,
        Guid? packageId,
        string filePath,
        byte[] packageBytes)
    {
        return new CompositionDocumentSnapshot(
            Id: (packageId ?? domain.GlobalId).ToString(),
            Title: packageTitle ?? domain.Name,
            Domain: MapDomain(domain),
            Extensions: BuildPackageExtensions(filePath, packageBytes));
    }

    private static CompositionDomainSnapshot MapDomain(Domain domain)
    {
        return new CompositionDomainSnapshot(
            Id: domain.GlobalId.ToString(),
            Name: domain.Name,
            Summary: domain.Summary,
            ConceptDefinitions: domain.ConceptDefinitions.Select(definition => MapIdeaDefinition(definition, "Concept")).ToArray(),
            RelationshipDefinitions: domain.RelationshipDefinitions.Select(definition => MapIdeaDefinition(definition, "Relationship")).ToArray(),
            LinkRoleDefinitions: domain.LinkRoleVariants.Select(MapLinkRoleVariant).ToArray(),
            MarkerDefinitions: domain.MarkerDefinitions.Select(MapMarkerDefinition).ToArray(),
            TableDefinitions: domain.TableDefinitions.Select(MapTableDefinition).ToArray(),
            ExternalLanguages: domain.ExternalLanguages.Select(MapExternalLanguage).ToArray(),
            Templates: MapTemplates(domain).ToArray(),
            Extensions: BuildDomainExtensions(domain).ToArray());
    }

    private static CompositionDefinitionSnapshot MapIdeaDefinition(IdeaDefinition definition, string kind)
    {
        return new CompositionDefinitionSnapshot(
            Id: definition.GlobalId.ToString(),
            Name: definition.Name,
            Kind: kind,
            Summary: definition.Summary,
            Style: MapSymbolStyle(definition.DefaultSymbolFormat),
            Details: definition.DetailDesignators.Select(MapDetailDesignator).ToArray(),
            Extensions: BuildIdeaDefinitionExtensions(definition, kind).ToArray());
    }

    private static CompositionDefinitionSnapshot MapMarkerDefinition(MarkerDefinition definition)
    {
        return new CompositionDefinitionSnapshot(
            Id: MarkerId(definition),
            Name: definition.Name,
            Kind: "Marker",
            Summary: definition.Summary,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", definition.TechName),
                new CompositionExtensionSnapshot("legacy.clusterKey", definition.ClusterKey)
            ]);
    }

    private static CompositionDefinitionSnapshot MapTableDefinition(TableDefinition definition)
    {
        return new CompositionDefinitionSnapshot(
            Id: definition.GlobalId.ToString(),
            Name: definition.Name,
            Kind: "Table",
            Summary: definition.Summary,
            Details: definition.FieldDefinitions
                .OrderBy(field => field.StorageIndex)
                .Select(MapFieldDefinition)
                .ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", definition.TechName),
                new CompositionExtensionSnapshot("legacy.metaId", definition.MetaId.ToString())
            ]);
    }

    private static CompositionDefinitionSnapshot MapExternalLanguage(ExternalLanguageDeclaration language)
    {
        return new CompositionDefinitionSnapshot(
            Id: language.GlobalId.ToString(),
            Name: language.Name,
            Kind: "ExternalLanguage",
            Summary: language.Summary,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", language.TechName),
                new CompositionExtensionSnapshot("legacy.metaId", language.MetaId.ToString())
            ]);
    }

    private static CompositionDefinitionSnapshot MapLinkRoleVariant(SimplePresentationElement variant)
    {
        return new CompositionDefinitionSnapshot(
            Id: LinkRoleVariantId(variant),
            Name: variant.Name,
            Kind: "LinkRole",
            Summary: variant.Summary,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", variant.TechName)
            ]);
    }

    private static IEnumerable<CompositionExtensionSnapshot> BuildDomainExtensions(Domain domain)
    {
        yield return new CompositionExtensionSnapshot("legacy.domain.techName", domain.TechName);
        yield return new CompositionExtensionSnapshot("legacy.domain.modelRevision", domain.ModelRevision.ToString());
        yield return new CompositionExtensionSnapshot("legacy.domain.viewGridSize", domain.ViewGridSize.ToString());
        yield return new CompositionExtensionSnapshot("legacy.domain.currentExternalLanguage", domain.CurrentExternalLanguage?.TechName ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.domain.defaultTableDefinition", domain.DefaultTableDef?.GlobalId.ToString() ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.domain.defaultTableCategory", domain.DefaultTableDefCategory?.TechName ?? string.Empty);

        foreach (var cluster in domain.ConceptDefClusters)
        {
            yield return MapPresentationElement("legacy.conceptDefCluster", cluster);
        }

        foreach (var cluster in domain.RelationshipDefClusters)
        {
            yield return MapPresentationElement("legacy.relationshipDefCluster", cluster);
        }

        foreach (var cluster in domain.MarkerClusters)
        {
            yield return MapPresentationElement("legacy.markerCluster", cluster);
        }

        foreach (var complement in Domain.StandardComplementDefinitions)
        {
            yield return MapPresentationElement("legacy.complementDefinition", complement);
        }
    }

    private static CompositionExtensionSnapshot MapPresentationElement(string keyPrefix, FormalElement element)
    {
        return MapPresentationElement(keyPrefix, element.Name, element.TechName, element.Summary, element.GlobalId.ToString());
    }

    private static CompositionExtensionSnapshot MapPresentationElement(string keyPrefix, SimpleElement element)
    {
        return MapPresentationElement(keyPrefix, element.Name, element.TechName, element.Summary, string.Empty);
    }

    private static CompositionExtensionSnapshot MapPresentationElement(
        string keyPrefix,
        string name,
        string techName,
        string summary,
        string globalId)
    {
        var key = string.IsNullOrWhiteSpace(techName) ? name : techName;
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = name,
            ["techName"] = techName,
            ["summary"] = summary,
            ["globalId"] = globalId
        };

        return new CompositionExtensionSnapshot($"{keyPrefix}.{key}", name, properties);
    }

    private static IEnumerable<CompositionExtensionSnapshot> BuildIdeaDefinitionExtensions(
        IdeaDefinition definition,
        string kind)
    {
        yield return new CompositionExtensionSnapshot("legacy.techName", definition.TechName);
        yield return new CompositionExtensionSnapshot("legacy.metaId", definition.MetaId.ToString());
        yield return new CompositionExtensionSnapshot("legacy.representativeShape", definition.RepresentativeShape);
        yield return new CompositionExtensionSnapshot("legacy.ownerDefinitor", definition.OwnerDefinitor?.GlobalId.ToString() ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.ancestorDefinitor", definition.AncestorIdeaDef?.GlobalId.ToString() ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.clusterTechName", definition.Cluster?.TechName ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.clusterName", definition.Cluster?.Name ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.isComposable", definition.IsComposable.ToString());
        yield return new CompositionExtensionSnapshot("legacy.isVersionable", definition.IsVersionable.ToString());
        yield return new CompositionExtensionSnapshot("legacy.canAutomaticallyCreateRelatedConcepts", definition.CanAutomaticallyCreateRelatedConcepts.ToString());
        yield return new CompositionExtensionSnapshot("legacy.canGroupIntersectingObjects", definition.CanGroupIntersectingObjects.ToString());
        yield return new CompositionExtensionSnapshot("legacy.canAutomaticallyCreateGroupedConcepts", definition.CanAutomaticallyCreateGroupedConcepts.ToString());
        yield return new CompositionExtensionSnapshot("legacy.automaticGroupedConceptDef", definition.AutomaticGroupedConceptDef?.GlobalId.ToString() ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.hasGroupRegion", definition.HasGroupRegion.ToString());
        yield return new CompositionExtensionSnapshot("legacy.hasGroupLine", definition.HasGroupLine.ToString());
        yield return new CompositionExtensionSnapshot("legacy.preciseConnectByDefault", definition.PreciseConnectByDefault.ToString());
        yield return new CompositionExtensionSnapshot("legacy.defaultSymbolInitialWidth", definition.DefaultSymbolFormat?.InitialWidth.ToString() ?? string.Empty);
        yield return new CompositionExtensionSnapshot("legacy.defaultSymbolInitialHeight", definition.DefaultSymbolFormat?.InitialHeight.ToString() ?? string.Empty);

        if (definition is ConceptDefinition conceptDefinition)
        {
            yield return new CompositionExtensionSnapshot("legacy.ancestorConceptDefinition", conceptDefinition.AncestorConceptDef?.GlobalId.ToString() ?? string.Empty);
            yield return new CompositionExtensionSnapshot("legacy.automaticCreationConceptDef", conceptDefinition.AutomaticCreationConceptDef?.GlobalId.ToString() ?? string.Empty);
            yield return new CompositionExtensionSnapshot("legacy.automaticCreationRelationshipDef", conceptDefinition.AutomaticCreationRelationshipDef?.GlobalId.ToString() ?? string.Empty);
            yield return new CompositionExtensionSnapshot("legacy.automaticCreationPositioningMode", conceptDefinition.AutomaticCreationPositioningMode.ToString());
            yield return new CompositionExtensionSnapshot("legacy.automaticCreationPositioningIsRadialized", conceptDefinition.AutomaticCreationPositioningIsRadialized.ToString());
        }

        if (definition is RelationshipDefinition relationshipDefinition)
        {
            yield return new CompositionExtensionSnapshot("legacy.isDirectional", relationshipDefinition.IsDirectional.ToString());
            yield return new CompositionExtensionSnapshot("legacy.isSimple", relationshipDefinition.IsSimple.ToString());
            yield return new CompositionExtensionSnapshot("legacy.hideCentralSymbolWhenSimple", relationshipDefinition.HideCentralSymbolWhenSimple.ToString());
            yield return new CompositionExtensionSnapshot("legacy.showNameIfHidingCentralSymbol", relationshipDefinition.ShowNameIfHidingCentralSymbol.ToString());
            yield return new CompositionExtensionSnapshot("legacy.ancestorRelationshipDefinition", relationshipDefinition.AncestorRelationshipDef?.GlobalId.ToString() ?? string.Empty);
            yield return new CompositionExtensionSnapshot("legacy.originOrParticipantLinkRoleDefinition", relationshipDefinition.OriginOrParticipantLinkRoleDef?.GlobalId.ToString() ?? string.Empty);
            yield return new CompositionExtensionSnapshot("legacy.targetLinkRoleDefinition", relationshipDefinition.TargetLinkRoleDef?.GlobalId.ToString() ?? string.Empty);
        }

        foreach (var template in definition.OutputTemplates)
        {
            yield return MapTemplate($"{kind.ToLowerInvariant()}.definition", template);
        }
    }

    private static CompositionDetailSnapshot MapDetailDesignator(DetailDesignator designator)
    {
        return new CompositionDetailSnapshot(
            Id: designator.GlobalId.ToString(),
            Kind: designator.GetType().Name,
            Name: designator.Name,
            Value: designator.TechName,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.summary", designator.Summary)
            ]);
    }

    private static CompositionDetailSnapshot MapFieldDefinition(FieldDefinition field)
    {
        return new CompositionDetailSnapshot(
            Id: field.GlobalId.ToString(),
            Kind: "FieldDefinition",
            Name: field.Name,
            Value: field.FieldType?.Name ?? field.FieldType?.ToString() ?? string.Empty,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", field.TechName),
                new CompositionExtensionSnapshot("legacy.summary", field.Summary),
                new CompositionExtensionSnapshot("legacy.storageIndex", field.StorageIndex.ToString()),
                new CompositionExtensionSnapshot("legacy.isRequired", field.IsRequired.ToString())
            ]);
    }

    private static CompositionIdeaSnapshot MapIdea(CompositionNodeView node, VisualSymbol? symbol)
    {
        var idea = symbol?.OwnerRepresentation.RepresentedIdea;
        if (idea is null)
        {
            return new CompositionIdeaSnapshot(node.Id, node.Text);
        }

        return new CompositionIdeaSnapshot(
            Id: node.Id,
            Name: node.Text,
            DefinitionId: idea.IdeaDefinitor?.GlobalId.ToString() ?? string.Empty,
            ParentIdeaId: idea.OwnerContainer?.GlobalId.ToString() ?? string.Empty,
            ActiveViewId: idea.CompositeActiveView?.GlobalId.ToString() ?? string.Empty,
            IsComposite: idea.IsComposite,
            ShortcutTargetId: symbol?.OwnerRepresentation.IsShortcut == true
                ? idea.GlobalId.ToString()
                : string.Empty,
            Summary: idea.Summary,
            Details: idea.Details.Select(MapContainedDetail).ToArray(),
            Markers: idea.Markings.Select(marker => MarkerId(marker.Definitor)).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", idea.TechName),
                new CompositionExtensionSnapshot("legacy.type", idea.GetType().Name)
            ]);
    }

    private static IEnumerable<CompositionViewLayerSnapshot> MapViews(
        Composition composition,
        CompositionViewSnapshot rootSnapshot)
    {
        var rootView = composition.ActiveView ?? composition.RootView;
        yield return new CompositionViewLayerSnapshot(
            Id: rootView?.GlobalId.ToString() ?? "view.default",
            Name: rootView?.Name ?? rootSnapshot.Title,
            Nodes: rootSnapshot.Nodes,
            Connectors: rootSnapshot.Connectors,
            Complements: rootView is null ? [] : MapComplements(rootView).ToArray());

        foreach (var idea in composition.GetNestedCompositeIdeas(true)
                     .Where(idea => idea.CompositeViews.Count > 0))
        {
            foreach (var view in idea.CompositeViews)
            {
                if (rootView is not null && ReferenceEquals(view, rootView))
                {
                    continue;
                }

                yield return MapView(view, idea.GlobalId.ToString());
            }
        }
    }

    private static CompositionViewLayerSnapshot MapView(View view, string containerIdeaId)
    {
        var viewObjects = ReadViewObjects(view).ToArray();
        var symbols = viewObjects.OfType<VisualSymbol>().ToArray();
        var symbolIds = symbols.ToDictionary(symbol => symbol, symbol => symbol.GlobalId.ToString());
        var nodes = symbols.Select(MapNodeView).ToArray();
        var connectors = viewObjects
            .OfType<VisualConnector>()
            .Select(connector => MapConnectorView(connector, symbolIds))
            .Where(connector => connector is not null)
            .Cast<CompositionConnectorView>()
            .ToArray();

        return new CompositionViewLayerSnapshot(
            Id: view.GlobalId.ToString(),
            Name: view.Name,
            Nodes: nodes,
            Connectors: connectors,
            Complements: MapComplements(view).ToArray(),
            ContainerIdeaId: containerIdeaId);
    }

    private static IEnumerable<CompositionExtensionSnapshot> MapComplements(View view)
    {
        return ReadViewObjects(view)
            .OfType<VisualComplement>()
            .Select(MapComplement);
    }

    private static CompositionExtensionSnapshot MapComplement(VisualComplement complement)
    {
        var kind = complement.Kind?.TechName ?? complement.GetType().Name;
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kind"] = kind,
            ["title"] = complement.Kind?.Name ?? kind,
            ["x"] = Format(complement.BaseLeft),
            ["y"] = Format(complement.BaseTop),
            ["width"] = Format(complement.BaseWidth),
            ["height"] = Format(complement.BaseHeight),
            ["centerX"] = Format(complement.BaseCenter.X),
            ["centerY"] = Format(complement.BaseCenter.Y),
            ["zOrder"] = complement.ZOrder.ToString(CultureInfo.InvariantCulture)
        };

        AddPropertyField(properties, "text", TryGetField<string>(complement, VisualComplement.PROP_FIELD_TEXT));
        AddPropertyField(properties, "foreground", TryGetField<object>(complement, VisualComplement.PROP_FIELD_FOREGROUND)?.ToString());
        AddPropertyField(properties, "background", TryGetField<object>(complement, VisualComplement.PROP_FIELD_BACKGROUND)?.ToString());
        AddPropertyField(properties, "orientation", TryGetField<object>(complement, VisualComplement.PROP_FIELD_ORIENTATION)?.ToString());
        AddPropertyField(properties, "quadrant", TryGetField<object>(complement, VisualComplement.PROP_FIELD_QUADRANT)?.ToString());
        AddPropertyField(properties, "offsetX", FormatNullable(TryGetDoubleField(complement, VisualComplement.PROP_FIELD_OFFSETX)));
        AddPropertyField(properties, "offsetY", FormatNullable(TryGetDoubleField(complement, VisualComplement.PROP_FIELD_OFFSETY)));
        AddPropertyField(properties, "lineThickness", FormatNullable(TryGetDoubleField(complement, VisualComplement.PROP_FIELD_LINETHICK)));
        AddPropertyField(properties, "lineDash", TryGetField<object>(complement, VisualComplement.PROP_FIELD_LINEDASH)?.ToString());

        return new CompositionExtensionSnapshot(
            $"legacy.complement.{kind}.{complement.GlobalId}",
            complement.ContentAsText ?? string.Empty,
            properties);
    }

    private static void AddPropertyField(
        IDictionary<string, string> properties,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            properties[key] = value!;
        }
    }

    private static double? TryGetDoubleField(VisualComplement complement, string fieldName)
    {
        try
        {
            return complement.GetPropertyField<double>(fieldName, ReturnDefault: false);
        }
        catch
        {
            return null;
        }
    }

    private static T? TryGetField<T>(VisualComplement complement, string fieldName)
    {
        try
        {
            return complement.GetPropertyField<T>(fieldName, ReturnDefault: false);
        }
        catch
        {
            return default;
        }
    }

    private static CompositionNodeView MapNodeView(VisualSymbol symbol)
    {
        return new CompositionNodeView(
            symbol.GlobalId.ToString(),
            symbol.OwnerRepresentation.RepresentedIdea?.Name ?? symbol.GlobalId.ToString(),
            new TcPoint(symbol.BaseArea.X, symbol.BaseArea.Y),
            new TcSize(symbol.BaseArea.Width, symbol.BaseArea.Height));
    }

    private static CompositionConnectorView? MapConnectorView(
        VisualConnector connector,
        IReadOnlyDictionary<VisualSymbol, string> symbolIds)
    {
        if (!symbolIds.TryGetValue(connector.OriginSymbol, out var sourceId) ||
            !symbolIds.TryGetValue(connector.TargetSymbol, out var targetId))
        {
            return null;
        }

        return new CompositionConnectorView(
            connector.GlobalId.ToString(),
            sourceId,
            targetId,
            connector.OwnerRelationshipRepresentation?.RepresentedRelationship?.Name ?? string.Empty);
    }

    private static CompositionRelationshipSnapshot MapRelationship(CompositionConnectorView connector, Relationship? relationship)
    {
        if (relationship is null)
        {
            return new CompositionRelationshipSnapshot(connector.Id, connector.Text, connector.SourceId, connector.TargetId);
        }

        return new CompositionRelationshipSnapshot(
            Id: connector.Id,
            Name: relationship.Name,
            SourceIdeaId: connector.SourceId,
            TargetIdeaId: connector.TargetId,
            DefinitionId: relationship.RelationshipDefinitor?.Value?.GlobalId.ToString() ?? string.Empty,
            LinkRoleId: relationship.Links?
                .Select(link => LinkRoleVariantId(link.RoleVariant))
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id)) ?? string.Empty,
            Details: relationship.Details.Select(MapContainedDetail).ToArray(),
            Markers: relationship.Markings.Select(marker => MarkerId(marker.Definitor)).ToArray(),
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.techName", relationship.TechName),
                new CompositionExtensionSnapshot("legacy.type", relationship.GetType().Name)
            ]);
    }

    private static CompositionDetailSnapshot MapContainedDetail(ContainedDetail detail)
    {
        var value = detail is Table table ? table.RecordsLabel : detail.ToString();
        return new CompositionDetailSnapshot(
            Id: detail.Designation?.GlobalId.ToString() ?? Guid.NewGuid().ToString(),
            Kind: detail.Kind?.TechName ?? detail.GetType().Name,
            Name: detail.Designation?.Name ?? detail.GetType().Name,
            Value: value,
            Extensions:
            [
                new CompositionExtensionSnapshot("legacy.type", detail.GetType().Name),
                new CompositionExtensionSnapshot("legacy.techName", detail.Designation?.TechName ?? string.Empty)
            ]);
    }

    private static CompositionStyleSnapshot MapSymbolStyle(VisualSymbolFormat? format)
    {
        if (format is null)
        {
            return new CompositionStyleSnapshot();
        }

        return new CompositionStyleSnapshot(
            Fill: format.MainBackground?.ToString() ?? string.Empty,
            Stroke: format.LineBrush?.ToString() ?? string.Empty,
            StrokeThickness: format.LineThickness,
            StrokeDash: format.LineDash?.ToString() ?? string.Empty);
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? Format(value.Value) : string.Empty;
    }

    private static string MarkerId(MarkerDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.TechName)
            ? definition.Name
            : definition.TechName;
    }

    private static string LinkRoleVariantId(SimplePresentationElement? variant)
    {
        if (variant is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(variant.TechName)
            ? variant.Name
            : variant.TechName;
    }

    private static IEnumerable<CompositionExtensionSnapshot> MapTemplates(Domain domain)
    {
        foreach (var template in domain.OutputTemplatesForConcepts)
        {
            yield return MapTemplate("concept", template);
        }

        foreach (var template in domain.OutputTemplatesForRelationships)
        {
            yield return MapTemplate("relationship", template);
        }
    }

    private static CompositionExtensionSnapshot MapTemplate(string scope, TextTemplate template)
    {
        var language = template.Language?.TechName ?? "default";
        return new CompositionExtensionSnapshot(
            $"legacy.template.{scope}.{language}",
            template.Text ?? string.Empty,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["language"] = template.Language?.Name ?? string.Empty,
                ["extendsBaseTemplate"] = template.ExtendsBaseTemplate.ToString()
            });
    }

    private static IReadOnlyList<CompositionExtensionSnapshot> BuildPackageExtensions(string filePath, byte[] packageBytes)
    {
        return
        [
            new CompositionExtensionSnapshot("legacy.source.extension", Path.GetExtension(filePath).ToLowerInvariant()),
            new CompositionExtensionSnapshot("legacy.source.fileName", Path.GetFileName(filePath)),
            new CompositionExtensionSnapshot("legacy.package.base64", Convert.ToBase64String(packageBytes))
        ];
    }

    private static IEnumerable<object> ReadViewObjects(View? view)
    {
        if (view is null)
        {
            yield break;
        }

        foreach (var child in view.ViewChildren)
        {
            if (child?.Key is not null)
            {
                yield return child.Key;
            }
        }
    }

    private static Composition PrepareComposition(Composition composition)
    {
        composition.CompositionDefinitor.SetOwnerComposition(composition);
        TryApplyModelFixes(composition.CompositionDefinitor);
        return composition;
    }

    private static void TryApplyModelFixes(Domain domain)
    {
        try
        {
            ModelFixes.ApplyModelFixes(domain);
        }
        catch (NullReferenceException) when (domain.EditEngine is null)
        {
            // Read-only bridge has no active legacy edit engine to mark modified.
        }
        catch (NotSupportedException problem) when (IsLegacyImageMaterializationFailure(problem))
        {
            // Some legacy domains contain WPF image references that cannot be resolved in the bridge process.
            // The bridge is read-only and preserves the original package, so continue without blocking import.
        }
    }

    private static bool IsLegacyImageMaterializationFailure(Exception problem)
    {
        return problem.StackTrace?.IndexOf("ImageAssignment.get_Image", StringComparison.Ordinal) >= 0;
    }

    private static object LoadPackageContent(string filePath, out string? packageTitle, out Guid? packageId)
    {
        using var package = Package.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        packageTitle = package.PackageProperties?.Title;
        packageId = Guid.TryParse(package.PackageProperties?.Identifier, out var id) ? id : null;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var partUri = extension switch
        {
            ".tcom" => CompositionPartUri,
            ".tdom" => DomainPartUri,
            _ => throw new InvalidOperationException($"Unsupported legacy document extension: {extension}")
        };

        var part = package.PartExists(partUri)
            ? package.GetPart(partUri)
            : package.GetParts().FirstOrDefault(candidate => candidate.Uri.ToString().EndsWith(".bin", StringComparison.OrdinalIgnoreCase));

        if (part is null)
        {
            throw new InvalidOperationException($"Legacy document package has no binary content part: {filePath}");
        }

        return BytesHandling.Deserialize<ISphereModel>(part.GetStream());
    }
}
