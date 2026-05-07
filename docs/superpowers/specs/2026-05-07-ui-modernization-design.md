# ThinkComposer UI Modernization Design

Date: 2026-05-07

## Decision

Use the B2 "Command Center" mockup as the target direction: a Figma-like editor shell with a central canvas, left activity rail, left explorer, right inspector, top command bar, command/object search, and collapsible panels similar to VS Code.

Reference mockups:

- `docs/ui-mockups/thinkcomposer-canvas-focus-iterations.html`
- `docs/ui-mockups/thinkcomposer-b2-vscode-collapse.html`
- `docs/ui-mockups/thinkcomposer-b2-vscode-light-dark.html`

## Goals

- Make the canvas the primary workspace.
- Keep navigation and properties discoverable.
- Let users collapse the explorer, inspector, and bottom panel independently.
- Preserve existing editor behavior while modernizing shell layout and styling.
- Keep the first implementation WPF-native and incremental.

## Layout

The shell will use four main regions:

- Activity rail: always visible, narrow, icon-first navigation for explorer, inspector, messages/search, and focus mode.
- Explorer panel: left panel for map/document/navigation content.
- Canvas: central editor surface with tabs, quick actions, and floating contextual tools.
- Inspector panel: right panel for properties and selected-object actions.
- Bottom panel: collapsible area for messages, search results, previews, and future diagnostics.

The top command bar remains visible and compact. It contains common actions and a search box for commands, objects, and views.

## Collapsible Behavior

Panels collapse like VS Code:

- The rail stays visible.
- Clicking a rail icon toggles the related panel.
- Each panel also has a local collapse button.
- Focus mode collapses explorer, inspector, and bottom panel together.
- Collapsed state should not destroy panel content; it only hides layout width or height.

Preferred initial widths:

- Rail: 54 px.
- Explorer: 240 px.
- Inspector: 250 px.
- Bottom panel: 180 px.

## Visual Style

Use VS Code-inspired light and dark themes:

- Default theme: clean almost-white UI with white panels, very light borders, pale canvas grid, and blue command accent.
- Secondary theme: dark editor UI with VS Code-like graphite panels, dark canvas grid, and the same blue command accent.
- Keep accent color usage restrained: primary actions, active rail item, selected tab, and status bar.
- Avoid warm beige/tan shell colors.
- Minimal gradients; mostly solid fills, subtle borders, and clear contrast.

Avoid decorative hero-style visuals. The product should feel like a dense editor, not a landing page.

## Implementation Boundaries

First implementation should target the shell only:

- `ThinkComposer/ApplicationShell/MainWindow.xaml`
- `ThinkComposer/ApplicationShell/MainWindowHeader.xaml`
- related shell code-behind only where needed
- shared WPF resources in `Common/Themes/Generic.xaml` or a new app resource dictionary

Do not rewrite canvas rendering, document model, or command logic in the first UI pass.

## UX Details

- The canvas should remain usable at small and large window sizes.
- Panel collapse/expand should be fast and predictable.
- Command search can be visual-only in the first pass if wiring all commands is too large.
- Existing palette/tool commands should be preserved, then reorganized.
- Keyboard shortcuts are a second pass, except focus mode if cheap to wire.

## Verification

Minimum verification for the first implementation:

- `dotnet build Instrumind_ThinkComposer.sln -p:Configuration=Debug -p:Platform=x86`
- net10 smoke test: app opens, responds, panels collapse/expand, app closes with exit code 0.
- Manual check at 1440x810 and a narrower width around 1100 px.

## Non-goals

- No full Figma clone.
- No canvas engine rewrite.
- No document format changes.
- No cleanup of legacy projects in this pass.

## Risks

- Existing panels may assume fixed host sizes.
- Current toolbar/palette content may not map cleanly into the new shell.
- Some controls may rely on old resource keys, so theme changes should preserve compatibility keys while changing values.
