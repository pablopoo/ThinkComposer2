# Advanced Complements And Tables Design

## Decision

Close the next practical parity gaps without introducing a full spreadsheet engine or a legacy-perfect visual clone.

## Complement Scope

- Respect legacy complement `lineThickness` as a stroke-width alias.
- Respect `lineDash` / `strokeDash` in WinUI canvas and native PDF output.
- Apply `offsetX` / `offsetY` to explicit and generated complement positions.
- Use `quadrant` / `orientation` as placement hints for generated complement cards.

Unknown legacy complement kinds remain preserved as extension data and are rendered with the closest card or group behavior.

## Table Scope

- Persist column-width metadata in table snapshots, XML table records, and CSV table details.
- Add a compact column-width field to the WinUI table editor.
- Add simple formula evaluation for common table cells:
  - `=A1+B1`
  - `=A1*2`
  - `=SUM(A:A)`
  - `=SUM(A1:A3)`
- Add a WinUI action to evaluate formulas into values.
- Evaluate formulas in report output so exported tables show calculated values.

## Out Of Scope

- Pointer-driven column resizing.
- Drag/drop row reordering.
- Full spreadsheet grammar.
- Large-table virtualization.
