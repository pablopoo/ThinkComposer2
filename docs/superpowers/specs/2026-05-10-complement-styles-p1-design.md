# Complement Styles P1 Design

Date: 2026-05-10

## Goal

Improve visual-complement parity by carrying complement style metadata from modern documents into both the WinUI canvas and native PDF exports.

This targets practical style support for current-view complements, not a full legacy visual grammar clone.

## Scope

Complements can already define:

- `kind`
- `title`
- `x`
- `y`
- `width`
- `height`

This slice adds style parsing for common properties:

- `fill`, plus aliases `background`, `backgroundColor`
- `stroke`, plus aliases `border`, `borderColor`
- `text`, plus aliases `foreground`, `foregroundColor`, `textColor`
- `opacity` or `alpha`
- `strokeThickness`, `borderThickness`, `borderWidth`
- `font`, `fontFamily`
- `fontSize`
- `icon`

## UX

The inspector stays text-based for now: users can keep editing complement payload/properties through the existing complement model.

Rendering changes:

- Group regions use style fill/stroke/opacity when present.
- Cards use style fill/stroke/text/opacity when present.
- Legend, quote, and info cards retain readable defaults if no style is supplied.
- Unknown or invalid style values fall back to current theme defaults.

## Architecture

Add a small core style record:

- `CompositionComplementStyle`

Extend:

- `CompositionComplementRenderItem` with `Style`
- `CompositionViewComplementLayout` to parse style metadata once

Consumers:

- WinUI canvas reads `item.Style` and maps it into Win2D colors.
- PDF exporter reads `item.Style` and maps it into Skia colors.

No WinUI types are introduced into core.

## Testing

Core tests cover:

- style aliases are parsed
- opacity and stroke thickness parse invariant-culture numeric values
- invalid or missing style values are safe fallbacks
- existing layout/kind/title behavior still works
- PDF export still handles styled complements

Visual smoke is covered through existing WinUI build and migration checks.

## Acceptance Criteria

- Complement style properties affect WinUI canvas rendering.
- Same style properties affect native PDF complement rendering.
- Existing documents without complement styles render as before.
- Tests and migration checks remain green.
