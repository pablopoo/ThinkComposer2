namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionViewOptionsSnapshot(
    bool ShowGrid = false,
    bool SnapToGrid = false,
    bool ShowGridPoints = false,
    bool ShowIndicators = true,
    bool ShowMarkers = true,
    bool ShowMarkerTitles = false,
    bool ShowConceptDefinitionLabels = false,
    bool ShowRelationshipDefinitionLabels = false,
    bool ShowLinkRoleDescriptorLabels = false,
    bool ShowLinkRoleDefinitorLabels = false,
    bool ShowLinkRoleVariantLabels = false,
    bool AutoSizeByEnteredText = false);
