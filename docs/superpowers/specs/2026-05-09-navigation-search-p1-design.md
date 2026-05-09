# Navigation And Search P1 Design

## Decision

The next P1 slice will improve daily navigation and editing speed in the WinUI app:

- implement `Go to Parent`
- add `Find & Replace`
- keep search results and command palette behavior consistent

This is intentionally smaller than all remaining P1 work. Tables, complements, and Domain Studio authoring stay separate.

## User Experience

### Go To Parent

When the current view belongs to a composite idea, `Go to Parent` returns to the nearest parent view and selects the container idea.

Entry points:

- command palette
- canvas context menu
- inspector action area where relevant

If the current view has no parent, the command is disabled with a clear reason.

### Find & Replace

The bottom Search panel becomes the main text workflow:

- search box keeps current behavior
- replace box appears below search
- `Replace selected`
- `Replace all`

Search covers:

- concept names
- relationship names
- detail names and values
- view names
- domain definition names
- complement keys and values

Replace should be conservative:

- `Replace selected` edits only the selected search result
- `Replace all` applies only to editable text fields in the current document snapshot
- command names are searchable but not replaceable
- generated preview text is searchable only through its source fields, not as raw generated output

## Architecture

Add core services in `ThinkComposer.Core.Rendering`:

- `CompositionDocumentNavigator`
- `CompositionDocumentTextSearch`
- `CompositionDocumentTextReplacer`

The core layer owns traversal and immutable document updates. WinUI only binds controls, displays results, and applies returned snapshots.

Search result entries should identify:

- target kind
- target id
- field path
- display title
- preview text
- whether the result is replaceable

## Data Flow

`Find & Replace` flow:

1. WinUI reads search/replace text.
2. Core search returns result entries.
3. Selecting a result navigates or selects the relevant object.
4. Replace command calls core replacer.
5. Core returns an updated `CompositionDocumentSnapshot`.
6. WinUI applies the document, refreshes the current view, marks dirty state, and refreshes search results.

`Go to Parent` flow:

1. WinUI asks core navigator for the parent navigation target.
2. If a target exists, WinUI switches to the parent view and selects the container idea.
3. If no target exists, the command remains disabled and reports the reason through command metadata.

## Error Handling

- Empty search text returns no results.
- Empty replace text is valid and means delete matched text.
- Replace all on zero editable matches is a no-op with status text.
- If a search result points to an object that no longer exists, selection is ignored and results refresh.
- Invalid navigation state does not throw in UI; core returns no parent target.

## Testing

Core tests will cover:

- finding concept, relationship, detail, view, definition, and complement text
- replace selected for a concept name
- replace all across multiple editable fields
- command/search entries are not replaceable
- parent navigation from child view to parent view/container idea
- no parent target for root view

WinUI verification will cover:

- build succeeds
- migration check passes
- search panel exposes replace controls
- command catalog disables `Go to Parent` when no parent exists
