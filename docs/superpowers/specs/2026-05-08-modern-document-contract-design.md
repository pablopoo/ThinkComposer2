# Modern Document Contract Design

Date: 2026-05-08

## Goal

Replace the simplified `.tcview` snapshot contract with a versioned modern document contract that can preserve legacy ThinkComposer data before the missing WinUI features are rebuilt.

## Scope

This first P0 block builds the data foundation only:

- A versioned full document DTO in `ThinkComposer.Core`.
- XML save/load roundtrip for the DTO.
- Conversion from the existing `CompositionViewSnapshot` into the new document DTO.
- Projection from the new document DTO back to `CompositionViewSnapshot` so current WinUI rendering keeps working.
- Extension buckets for legacy data that is not yet first-class, so import work can preserve data before UI editors exist.

This block does not yet build all missing UI. Domain/details/style/report UI comes after the data contract exists.

## Contract Shape

The modern document contract is explicit and extensible:

- `CompositionDocumentSnapshot`
  Root object with schema version, document identity, title, domain, ideas, relationships, views, templates, and extensions.
- `CompositionDomainSnapshot`
  Domain metadata plus concept definitions, relationship definitions, marker definitions, table definitions, external languages, and templates.
- `CompositionIdeaSnapshot`
  Concept-like semantic idea data, definition reference, summary, details, markers, and extensions.
- `CompositionRelationshipSnapshot`
  Relationship semantic data, definition reference, source/target ids, link role data, details, markers, and extensions.
- `CompositionViewLayerSnapshot`
  Visual view data: nodes, connectors, complements, view settings, and extensions.
- `CompositionDetailSnapshot`
  Generic detail container for attachments, links, tables, custom fields, and unknown legacy details.
- `CompositionStyleSnapshot`
  Generic visual style fields for fill, stroke, text, connector, and raw properties.
- `CompositionExtensionSnapshot`
  Key/value or XML-string extension bucket used to preserve unsupported legacy data without losing it.

## Compatibility

Existing `.tcview` stays readable. The new contract can project a `CompositionViewSnapshot` for current WinUI canvas rendering.

For migration safety:

- Legacy import should eventually emit this full document DTO, not only view nodes/connectors.
- Save from imported legacy documents should use the modern full contract once the bridge supports it.
- Unknown legacy fields must be preserved under extensions rather than discarded.

## Testing

The first implementation must include core tests proving:

- A modern document with domain metadata, definitions, details, styles, templates, markers, complements, and extensions roundtrips through XML.
- A simple `CompositionViewSnapshot` converts to `CompositionDocumentSnapshot`.
- A `CompositionDocumentSnapshot` projects back to the same renderable nodes/connectors used by WinUI.

## Follow-On Blocks

1. Expand `ThinkComposer.LegacyBridge` to export a full modern document.
2. Update WinUI to open/save the new modern document format.
3. Add read-only Domain/Details/Styles panels from the full contract.
4. Add editing for Domain/Details/Styles.
5. Rebuild reports and generation over the full contract.
