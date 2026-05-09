# Tables P1 Design

## Decision

Tables P1 will improve the existing WinUI table editor instead of replacing it with a new grid control.

The goal is faster daily editing while preserving the current `.tcdoc` data contract:

- paste multiple rows from clipboard
- sort rows by selected column
- duplicate, move, clear, and delete rows
- keep CSV-style storage for table details and base-table records

This does not introduce a spreadsheet engine. It adds the missing convenience behavior around the current table model.

## User Experience

The current Table editor expander keeps the same structure:

- columns box
- row list
- cell editor
- save table button

It gains compact row actions:

- `Paste rows`
- `Sort A-Z`
- `Sort Z-A`
- `Duplicate row`
- `Move up`
- `Move down`
- `Clear row`
- `Remove row`

Sorting uses the selected column. If no row is selected, it uses the first column. Sorting is stable and treats missing cells as empty strings.

Paste accepts:

- tab-separated rows from Excel/Sheets
- comma-separated rows
- line-separated values

If pasted data has more columns than the current table, columns are extended with generated names such as `Column 4`.

## Architecture

Add core helpers in `ThinkComposer.Core.Rendering`:

- `CompositionDetailTableEditor`
- `CompositionDetailTablePaste`
- `CompositionDetailTableSortDirection`

The helpers work on `CompositionDetailTableSnapshot` and return new snapshots.

WinUI owns:

- clipboard access
- selected row/column state
- applying the returned rows back to the existing editor

Core owns:

- parsing pasted rows
- normalizing row width
- sorting
- moving rows
- duplicating rows
- clearing rows

## Data Flow

Paste flow:

1. WinUI reads clipboard text.
2. Core parses it into rows.
3. Core extends columns if needed.
4. WinUI replaces editor state with the returned snapshot.
5. User saves the table using the existing save button.

Sort flow:

1. WinUI determines selected column index.
2. Core sorts rows.
3. WinUI refreshes the row list.
4. User saves the table using the existing save button.

Row actions:

1. WinUI passes selected row index.
2. Core returns updated rows.
3. WinUI refreshes row list and cell editor.

## Error Handling

- Empty paste is a no-op with status text.
- Invalid row index is a no-op.
- Sort on empty rows is a no-op.
- Missing selected column falls back to column `0`.
- Paste never throws on malformed CSV/TSV; it treats the input as plain rows when needed.

## Testing

Core tests will cover:

- TSV paste adds multiple rows.
- CSV paste preserves quoted commas.
- Paste extends columns when needed.
- Sort ascending and descending by selected column.
- Duplicate row.
- Move row up/down.
- Clear row.
- Invalid row operations are no-ops.

WinUI verification will cover:

- build succeeds
- migration check passes
- table action buttons are present
- existing table save behavior still works

## Out Of Scope

- full spreadsheet editing
- formulas
- column resizing
- drag/drop rows
- clipboard copy/export
- virtualized large table rendering
