# Text Editor (`Cornerstone.Presentation.Controls.Text`)

Dense reference for implementing or changing the Cornerstone text editor and terminal.

**Primary location:** `Cornerstone.Presentation/Controls/Text/`
**Themes:** `Cornerstone.Presentation/Theme/Controls/TextEditor.cxaml`, `Theme/Controls/Terminal.cxaml`

---

## Architecture at a glance

```
TextEditor / Terminal          (templated control, host + chrome)
    │
    ├── LeftMargins            (e.g. LineNumberMargin)
    ├── ScrollViewer
    └── TextRenderer           (drawing surface, ILogicalScrollable, input hit-test)
            │
            └── TextEditorViewModel   (document + caret + managers)
                    ├── Buffer          StringGapBuffer (char storage)
                    ├── Lines           LineManager → Line[]
                    ├── TokenManager    syntax / color tokens
                    ├── Diagnostics     squiggle spans (DiagnosticManager)
                    ├── Carets (CaretManager) / Caret (primary) + Selection
                    ├── InputManager    key bindings → commands
                    ├── UndoManager
                    ├── Clipboard / Indention / Completion managers
                    └── ViewMetrics     char size, viewport, extent, **scroll offset**
```

| Layer | Type | Responsibility |
|-------|------|----------------|
| Control | `TextEditor` / `TextEditor<T>` | Template, margins, scroll helpers, `Text` property, IME client, `IsReadOnly` |
| Surface | `TextRenderer` | Measure/arrange, paint tokens, pointer/keyboard, logical scroll, caret blink |
| Document VM | `TextEditorViewModel` | **All control state**: document, caret, folds, tokens, scroll, AutoScroll |
| Storage | `StringGapBuffer` | O(1)-ish insert/delete near gap; source of truth for characters |
| Structure | `LineManager` / `Line` | Logical lines, wrap points, visual rects, hit-testing |
| Style | `TokenManager` + `Tokenizer` | Syntax tokens; rebuild on document change |
| Terminal | `Terminal` + `TerminalViewModel` | Prompt lock, command history, ANSI colors |

**Design rule:** mutate the **ViewModel** (buffer via Insert/Remove/Load/Append, plus caret, folds, `ViewMetrics.Offset`, `AutoScroll`). The control and renderer are a projection. Never invent parallel text or scroll state on the control or a docking tab.

### State ownership (do not break this)

`TextEditorViewModel` holds **all** editor/terminal control state so docking can detach the view. Reattach and reopen re-apply the VM; they do not remember scroll on `TextEditor` or `EditorTabViewModel`.

| State | Lives on | View does |
|-------|----------|-----------|
| Text, caret, folds, tokens, undo | VM | Rebuild / paint |
| Scroll | `ViewMetrics.Offset` | Apply after layout; copy back only when the viewport is **stable** (height and extent &gt; 1) |
| Stick-to-bottom | VM `AutoScroll` | Pin on **extent growth**, not on every attach |
| Split console, tab header | Host tab (`EditorTabViewModel`) | Host chrome only |

**Must not:**

- Write `ViewMetrics.Offset` from a collapsed/zero viewport (inactive `TabControl` content). Cornerstone.Presentation clamps `ILogicalScrollable.Offset` to 0; that 0 is not user state.
- Reset offset to `(0,0)` on document `Load`/`Reset` or folding measure. Clamp to the new extent after layout.
- Let `EnsureCaretVisible` win over a restored VM offset (host sets `IsRestoringViewport` around caret restore; then apply `ViewMetrics.Offset`).
- Add `ScrollOffsetX/Y` (or similar) on a docking tab. Persist layout by serializing `ViewMetrics.Offset`.

**Restore order:** load document → folds → caret → apply `ViewMetrics.Offset`.

---

## File map

```
Text/
  TextEditor.axaml(.cs)      Control + theme template
  TextEditorViewModel.cs     Document API & change pipeline
  TextRenderer.cs            Render + input + scroll
  TextDocumentChangedArgs.cs Change event payload
  TextDocumentChangeType.cs  Reset | Add | Remove
  TextBoxTextInputMethodClient.cs  IME bridge

  Models/
    Caret.cs, CaretManager.cs, CaretMoveDirection.cs, Selection.cs
    Line.cs, LineManager.cs
    TokenManager.cs, UndoManager.cs
    ClipboardManager.cs, IndentionManager.cs, CompletionManager.cs, DiagnosticManager.cs, TextDiagnostic.cs

  Input/
    InputManager.cs          Default key bindings
    KeyCommand.cs            ICommand wrapper for KeyEventArgs
    IReadOnlySectionProvider.cs

  Rendering/
    IRenderer.cs             Background draw plug-in
    CaretVisual.cs (all carets), CurrentLineRenderer.cs (primary line), SelectionRenderer.cs (all selections)
    DiagnosticRenderer.cs    Squiggles from DiagnosticManager
    TextMetrics.cs           ViewMetrics + GetAdvance()

  Margins/
    Margin.cs, LineNumberMargin.cs

  History/
    CommandHistory.cs, CommandHistoryProvider.cs   (terminal command history, not undo)

  Terminal.axaml(.cs), TerminalViewModel.cs, TerminalTokenizer.cs
```

**Related:** Markdown rendering reuses `TextEditorViewModel` / `TextRenderer` as the document and paint surface — see [MarkdownView.md](MarkdownView.md) (agent) and [../Controls/MarkdownView.md](../Controls/MarkdownView.md) (product).

**Related (outside the Text folder):**

- `Cornerstone/Text/StringGapBuffer.cs` — buffer
- `Cornerstone/Text/Parsing/Token.cs`, `Tokenizer.cs`, `IndentionService.cs`, `CompletionService.cs`
- `Cornerstone/Text/Formatting/` — `DocumentFormatter`, `FormatSettings.Resolve(extension)`, `TextEditorViewModel.FormatDocument(options)` (one undo unit; never `Load`)
- `Cornerstone.Presentation/Controls/Text/SyntaxBrushes.cs`, `Themes/SyntaxColor.*.cxaml`
- Unit tests: `Tests/Cornerstone.UnitTests/Presentation/` (`Text/` editor tests)

---

## Control vs ViewModel

### `TextEditor` / `TextEditor<T>`

- Generic host: `TextEditor : TextEditor<TextEditorViewModel>`
- Subclass pattern: `Terminal : TextEditor<TerminalViewModel>`
- Owns `ViewModel` (`new T()` by default); rebindable via `ViewModelProperty`
- Forwards:
  - `Text` ↔ `ViewModel.Load` / `ToString()`
  - `ShowLineNumbers`, `WordWrap`, `HighlightCurrentLine` ↔ VM
  - `OnTextInput` → `ViewModel.ProcessTextInput` (unless `IsReadOnly`)
- Template parts: `PART_ScrollViewer`, `PART_TextRenderer`
- Left margin: adds `LineNumberMargin<T>` in `OnApplyTemplate` when empty
- `AutoScroll`: forwards `ViewModel.AutoScroll`; on document **growth** posts `ScrollToEnd`; user scroll up turns it off. Attach does not pin to end unless the VM offset is already at the bottom.

### `TextEditorViewModel`

Central API for **all** document **and view** state (including scroll). Marked `[Updateable(UpdateableAction.All, ["*"])]` for Keystone/dispatcher integration.

**Design limitation:** unlike a Keystone feature tab, this ViewModel **is** the live document store (buffer, caret, undo, tokens). The editor cannot be a thin AppDispatcher projection of `*State`. Hosts may still persist or project slices through Keystone, but they must not treat `TextEditorView` / `TextEditorViewModel` as the model for GrokMonitor-style dashboards. See [Keystone.md](../Keystone.md#scope-and-thread) and [AppDispatcher.md](../AppDispatcher.md).

Managers constructed in the ctor:

| Manager | Role |
|---------|------|
| `Lines` | Line index rebuild + measure |
| `Carets` | `CaretManager`: primary + extras (cap 500); `Caret` is `Carets.Primary` |
| `TokenManager` | Syntax tokens (optional tokenizer) |
| `InputManager` | Key gesture → command table |
| `UndoManager` | Stack of `UndoUnit` (changes + optional caret snapshots) |
| `Clipboard` | Cut/copy/paste; multi-caret join / line-split paste |
| `IndentionManager` | Tab string + smart indent on Enter |
| `CompletionManager` | Optional completion service |
| `ViewMetrics` | Character size, document extent, viewport, scroll offset |

Optional: `Profiler`, `ReadOnlySectionProvider`.

---

## Document change pipeline (critical)

Every mutation that changes text **must** end in `OnDocumentChanged`. That is the single coordination point.

### Change types

```csharp
public enum TextDocumentChangeType { Reset, Add, Remove }
public readonly struct TextDocumentChangedArgs
{
    int Offset;
    string Text;   // inserted text, or removed text; null on Reset
    TextDocumentChangeType Type;
}
```

### Flow

```
Buffer mutation (Insert / RemoveAt / Reset / Append)
        │
        ▼
OnDocumentChanged(offset, text, type)
        │
        ├── Reset → Carets.CollapseToPrimary(), Caret.Reset(), UndoManager.Clear()
        ├── else if UndoManager.Enabled → UndoManager.Add(args)
        │         (into an open compound, or a one-change UndoUnit)
        ├── Lines.Rebuild(args)
        ├── TokenManager.Rebuild(args)
        ├── Notify DocumentLength, UndoManager
        └── DocumentChanged event
                │
                ├── TextEditor: AutoScroll, margin invalidate, Reset measure
                └── TextRenderer: Offset=0 on Reset, InvalidateMeasure()
```

### Public mutation APIs

| Method | Effect |
|--------|--------|
| `Load(string)` | `Buffer.Reset` → **Reset** (full rebuild; clears undo) |
| `Clear()` | `Load("")` |
| `Append(string)` | Append at end → **Add** |
| `Insert(offset, string)` / `Insert(string)` at caret | **Add** (respects `ReadOnlySectionProvider`) |
| `RemoveAt(offset, length)` | **Remove** |
| `Delete(offset, forward)` | Selection first, else backspace/delete (handles `\r\n`); all carets when `Carets.Count > 1` |
| `ProcessTextInput(text)` | Same text at every caret (selection replace + insert) |
| `ProcessPaste(text)` | Line-split paste when line count equals caret count; else `ProcessTextInput` |
| `HandleEnterKey()` | Newline + optional smart indent at every caret |
| `Indent()` / `Unindent()` | Per caret: current line or that caret's selection |
| `FormatDocument(DocumentFormatOptions)` | Pretty-print / minify via `Cornerstone.Text.Formatting`. Compound Remove+Insert. **Not** `Load`. No-op if unchanged, read-only, or no tokenizer. See [TextFormatting.md](TextFormatting.md). |

**When adding features that change text:** call these APIs (or mutate `Buffer` then `OnDocumentChanged`). Do not leave Lines/Tokens out of sync.

### Compound edits

User-facing multi-site edits (`ForEachCaretEdit`, indent, paste, enter) wrap:

1. `UndoManager.BeginCompound()` — snapshot carets, collect following `Add` calls
2. Mutate high offset first; `Carets.ShiftAfter` so later carets stay valid
3. `OnDocumentChanged` still rebuilds lines/tokens; `Add` appends to the compound list
4. Merge overlapping carets; `EndCompound()` — one `UndoUnit` with before/after caret snapshots

`TryRemoveSelection` is a no-op while `UndoManager.IsProcessing` (undo/redo replay). `AddCompound` still exists for callers that already have a change array.

---

## Buffer and lines

### `StringGapBuffer`

- Internal `Buffer` on the view model; capacity starts at 16384.
- Characters only; line structure is derived.
- Newlines: code treats **`\r\n` as a unit** on delete/move; Enter inserts `"\r\n"`.

### `LineManager.Rebuild`

Incremental-style rebuild from the line containing `args.Offset`:

1. Start at `GetLineOffsetForDocumentOffset(offset)`
2. Walk buffer with `NextLine`, reusing pooled `Line` instances when possible
3. Pool surplus lines after the last rebuilt line

Empty document still has one empty line.

Lookup: binary search by document offset or by **visual Y** (`TryGetLineForOffset`).

### `Line`

Extends `TextRange` (`StartOffset`..`EndOffset`):

| Field | Meaning |
|-------|---------|
| `LineNumber` | 1-based |
| `LineEndingLength` | 0, 1 (`\n`/`\r`), or 2 (`\r\n`) |
| `VisualLayout` | Rect in document space (Y cumulative, height may span wraps) |
| `WrappedStartOffsets` | Document offsets where wrap continues |

`UpdateLineMetrics(offsetY, maxWidth)` computes wrap and visual size using `ViewMetrics.GetAdvance`. Measure is driven by `LineManager.Measure` from `TextEditorViewModel.Measure` during `TextRenderer.MeasureOverride`.

Measure is incremental:

- Add-only rebuilds start at the earliest dirty line (`_layoutFromIndex`).
- Lines after the dirty line keep cached width/wrap when their length and start-delta match the edit; Measure only assigns `VisualLayout.Y`.
- Wrap-off with a valid cache, unchanged character metrics, and at least one line with Height &gt; 0 returns the cached `DocumentSize` (resize must not walk every line). Reset/Remove drop that cache so Load after the empty constructor measure still computes visuals.
- Font-size / wrap-width changes remasure from line 0.
- `LastMeasureChangedLayout` is false on that wrap-off cache hit so carets are not rebuilt.
- Unconstrained measure does **not** write Viewport; `ArrangeOverride` owns the real viewport. Scroll invalidation fires only when Extent, Viewport, or Offset changed.
- Last-line in-place edits skip `FoldingManager.Refresh` (offsets still shift via `ApplyDocumentChange`).

Hit-testing: `GetNearestOffsetAtVisual(visualX, visualY, isAtEndOfLine)`.

---

## Caret and selection

### `CaretManager`

Always at least one caret. `TextEditorViewModel.Caret` **is** `Carets.Primary` (same instance). Each `Caret` owns its own `Selection`. Cap: `CaretManager.MaxCarets` (500). Overlapping empty offsets or touching/overlapping selections `MergeOverlapping`.

| API | Role |
|-----|------|
| `AddAt(offset)` | Add extra, or **toggle off** a non-primary empty caret at that offset |
| `AddRelativeToPrimary(lineDelta)` | Add one line beyond the **farthest** caret in that direction (same column, clamped). Repeats stack down/up the file |
| `EnsureAt` (private) | Add without toggle (used by relative add) |
| `CollapseToPrimary` | Drop extras |
| `DocumentOrder` / `ReverseDocumentOrder` | Clipboard vs buffer mutation |
| `ShiftAfter(offset, delta, source)` | Move other carets/selections after an insert/delete |
| `Capture` / `Restore` | Undo caret-set snapshots |

Events: `CaretMoved`, `SelectionUpdated`, `CaretsChanged` (renderer invalidates).

### `Caret`

- `Offset` is the document index (clamped 0..`Buffer.Count`).
- `_preferredVisualX` preserved across vertical moves **per caret**.
- `IsAtEndOfLine` disambiguates wrap boundary (same offset = end of previous visual row vs start of next).
- `UpdateVisualLayout()` → line’s `UpdateCaretVisual`. `Measure` calls `Carets.UpdateVisualLayouts()`.
- Overstrike is editor-wide (primary's `OverstrikeMode` used when painting extras).

Movements (`CaretMoveDirection`): char L/R, line U/D, page U/D, line start/end, smart line start (Home), document start/end. `MoveAllCarets` applies the same direction to every caret then merges. Word left/right exist on the enum but are **not wired** in `InputManager` yet.

### `Selection`

- Inclusive start, exclusive-ish end via offsets; `Length = Abs(End - Start)`.
- Keyboard: Shift + navigation sets `IsSelectingUsingKeyboard`. **Shift+Alt** does not start a selection (multi-caret add).
- Mouse: `StartMouseSelection` on the caret being dragged (`HandlePointerPressed` / `HandlePointerMoved`).
- `Updated` event → `CaretManager.SelectionUpdated` → renderer invalidates.

### Multi-site typing

`ProcessTextInput`, `Delete` (when count > 1), indent/unindent, duplicate, and Enter run `ForEachCaretEdit`: reverse document order, skip `CanModify == false`, `ShiftAfter`, merge. Completion trigger/open only when `Carets.Count == 1`.

Primary-only: IME (`TextBoxTextInputMethodClient`), highlight-current-line, scroll-into-view (`EnsureCaretVisible` on the caret that moved).

---

## Input path

```
KeyDown (TextRenderer)
  → ViewModel.ProcessKeyDownEvent
      → CompletionManager.TryHandleKey
          (Escape / arrows / Enter / Tab while open; **silent** triggers such as Ctrl+Space)
          Typed triggers (`.` `-` `\`) return false so the glyph is not eaten.
      → each Selection.ProcessKeyDown (Shift tracking)
      → InputManager.ProcessKeyArgs (first matching KeyBinding)

TextInput (TextEditor)
  → ViewModel.ProcessTextInput (if not IsReadOnly)
      → CompletionManager.TryTriggerFromInsertedText for typed triggers (`.`)

Pointer (TextRenderer)
  → hit-test line → HandlePointerPressed / Moved / Released

Ctrl+Wheel (TextRenderer)
  → FontSize 12..40
```

### Pointer (`HandlePointerPressed`)

| Gesture | Action |
|---------|--------|
| Alt+Click | `AddAt` (toggle extra); Alt-drag extends that caret's selection |
| Click | `CollapseToPrimary`, place primary |
| Shift+Click | Extend primary selection; extras kept |
| Double-click | Collapse, `SelectWord` |

### Default bindings (`InputManager.InitializeBindings`)

| Gesture | Action |
|---------|--------|
| Arrows / Shift+Arrows | `MoveAllCarets` / extend each selection |
| Home / End (+ Ctrl/Shift) | Smart line start, line end, document start/end (all carets) |
| PageUp/Down (+ Shift) | Page move (all carets) |
| Shift+Alt+Up/Down | Add caret on previous/next line from the farthest caret (VS). Ctrl+Alt+Up/Down also bound |
| Escape | `CollapseToPrimary` when `Count > 1` (otherwise not handled) |
| Ctrl+A | Select all |
| Enter / Return | `HandleEnterKey` |
| Back / Delete | Delete backward / forward (all carets when count > 1) |
| Ctrl+X/C/V | Cut / Copy / Paste |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| Insert | Toggle overstrike (primary) |
| Tab / Shift+Tab | Indent / Unindent |

To add shortcuts: `InputManager.AddBinding(gesture, new KeyCommand(...))` or `RemoveBinding`.

`KeyCommand` marks `KeyEventArgs.Handled` by default (`willHandle: true`).

---

## Undo / redo

- Stacks of `UndoUnit`: `Changes` (`TextDocumentChangedArgs[]`) plus optional `Before` / `After` `CaretSnapshot[]`.
- Single edits outside a compound → one-change unit, no caret snapshot (replay still moves primary).
- Compound (`BeginCompound` / `EndCompound`) → one unit; undo restores `Before` carets, redo restores `After`.
- **Undo** replays inverse (Add↔Remove), reverse order within the change list, then `Carets.Restore(Before)` if present.
- **Redo** reapplies original changes, then `Restore(After)`.
- While processing: `IsProcessing = true` so nested `OnDocumentChanged` does not push new undo entries.
- `Load` / Reset clears stacks and collapses to primary.
- Disable recording: `UndoManager.Enabled = false`.

## Clipboard

| Action | One caret | Several carets |
|--------|-----------|----------------|
| Copy | Selection only (`CanCopy`); payload is that range | Join each caret's selection, or whole line if empty, with `\r\n` (document order) |
| Cut | Selection or whole line; readonly segments via `GetDeletableSegments` | Same payloads, then `DeleteCopyPayloads` (compound) |
| Paste | `ProcessTextInput` | If clipboard line count **equals** caret count, one line per caret (document order); else same string at every caret |

`Clipboard.GetCopyText()` is the join helper (tests; `Copy` pushes it to `ClipboardService`).

---

## Rendering pipeline

1. **Measure:** cached sample `"X"` layout → `ViewModel.Measure` → char metrics + `Lines.Measure` → `DocumentSize`. Viewport is set only when available size is finite; otherwise arrange supplies it.
2. **Scroll:** `TextRenderer` is `ILogicalScrollable`. User scroll with a stable viewport writes `ViewMetrics.Offset`. Attach/arrange applies `ViewMetrics.Offset` (clamped). A collapsed viewport must not write 0 back into the VM. `OnScrollInvalidated` is skipped when Extent/Viewport/Offset are unchanged.
3. **Render order:**
   - Background `IRenderer`s: current line (**primary** only), then **every** selection (`SelectionRenderer.CollectDocumentRects`)
   - Visible lines only (binary search by Y, then a `for` over `LineManager` until past the viewport)
   - Per visual subline: `TokenManager.GetOverlappingTokens` (struct enumerator, no `TextRange` alloc). Styled `TextLayout`s are cached per run and reused across scroll; document/font/theme changes drop the cache.
   - Fold markers: `GetFoldingStartingOnLine` / `GetNextFolding` binary-search the sorted section list.
   - `CaretVisual` child: primary blinks; extras stay visible while focused. One caret uses theme foreground; several carets: primary red, extras blue.

### Extending background paint

Implement `IRenderer.Draw` and add to `TextRenderer.BackgroundRenderers`.

### Selection paint (must follow soft wrap)

Selection is painted by `SelectionRenderer` using the **same layout authority** as caret and wrap:

- Walk logical lines in the selection range
- For each line, `Line.GetSelectionRects(start, end)` emits one rect per **visual subline**
- X advances use `ViewMetrics.GetAdvance`; Y uses `VisualLayout.Top + subIndex * CharacterHeight`

Do **not** re-layout the line with Cornerstone.Presentation `TextLayout.HitTestTextRange` for selection — that re-wraps with a different algorithm and desyncs highlights from soft wrap.

Helpers on `Line`:

- `VisualSubLineCount`, `GetVisualSubLineRange`
- `GetVisualX(subLineStart, documentOffset)`
- `GetSelectionRects(selectionStart, selectionEnd)` → document-space rects

### Syntax highlighting

```csharp
viewModel.ConfigureForFileType(".cs"); // Completion + Indention + Tokenizer by extension
// or
viewModel.TokenManager.Initialize(customTokenizer);
```

`TokenManager.Rebuild` only runs if tokenizer exists and `SupportsRebuilding`. Tokens are pooled. Paint uses `token.SyntaxKind` → `SyntaxBrushes`, optional bold/italic/strikethrough, token background.

---

## Read-only regions

`IReadOnlySectionProvider`:

- `CanModify(offset)` — block insert/delete at offset
- `GetDeletableSegments(range)` — partial delete support (wired for future use; primary gate today is `CanModify`)

Used by:

- `Insert`, backspace/delete
- `ClipboardManager` cut/paste
- `TerminalViewModel` (offsets before `PromptOffset` are locked)

---

## Terminal specialization

```
Terminal : TextEditor<TerminalViewModel>
TerminalViewModel : TextEditorViewModel, IReadOnlySectionProvider
```

| Concern | Behavior |
|---------|----------|
| Prompt | `Prompt` string (default `"> "`); `PromptOffset` = first editable offset |
| Editable range | `CanModify` → `offset >= PromptOffset` |
| Input | `ReadInput()` from prompt to end; `SetInput` replaces that span |
| Submit | Enter (tunneled) → `ExecuteInput` → newline + `CommandEntered` |
| History | Up/Down at prompt → `CommandHistoryProvider` (not document undo) |
| Append colored | `AppendText` / ANSI via `TerminalTokenizer.ProcessAnsiText` |
| Tokens | Manual tokens for colors; `SupportsRebuilding` path may not re-lex ANSI |

Patterns:

```csharp
terminal.PromptForCommand();           // write prompt, set PromptOffset, scroll
terminal.AppendText("output\n");
terminal.AppendText("\e[32mOK\e[0m\n"); // ANSI
terminal.CommandEntered += (_, cmd) => { /* run */ terminal.PromptForCommand(); };
```

`Clear()` resets `PromptOffset` and token pool.

---

## ViewMetrics and layout numbers

| Property | Source |
|----------|--------|
| `CharacterHeight` / `CharacterWidth` | Measured from sample `TextLayout` (cached on the renderer until font changes) |
| `DocumentSize` | Sum of line visual layouts (wrap-off resize reuses the last measure) |
| `Viewport` | Arranged size. Unconstrained measure does not overwrite it. |
| `Offset` | **Source of truth** for scroll. Renderer applies it; do not treat `ScrollViewer.Offset` as durable. |

`GetAdvance(char)`:

- `\r`/`\n` → 0
- `\t` → 4 × CharacterWidth
- else ≈ CharacterWidth (ASCII/BMP); 2× for non-BMP

Monospace-oriented; DejaVu Sans Mono is the default theme font for the editor.

---

## Lifecycle and wiring tips

### Hosting a plain editor

```xml
<TextEditor Text="{Binding SourceText}"
            ShowLineNumbers="True"
            WordWrap="False"
            AutoScroll="False" />
```

Or code:

```csharp
var editor = new TextEditor();
editor.ViewModel.Load(fileText);
editor.ViewModel.ConfigureForFileType(Path.GetExtension(path));
editor.ViewModel.DocumentChanged += (_, e) => { /* dirty flag, etc. */ };
```

### Subclassing for a specialized editor

1. Create `MyViewModel : TextEditorViewModel` (optional extra state).
2. Create `MyEditor : TextEditor<MyViewModel>`.
3. Override input (`OnTextInput`, key handlers) only when host behavior differs; prefer VM methods for text mutations.

### Updating text programmatically

| Goal | Approach |
|------|----------|
| Replace entire document | `Load(text)` |
| Append log/output | `Append(text)` (+ `AutoScroll` if desired) |
| Insert at caret(s) | `Insert(text)` (primary) or `ProcessTextInput` (all carets) |
| Insert at offset | `Insert(offset, text)` then `Caret.Move(...)` if needed |
| Delete range | `RemoveAt(offset, length)` |
| Atomic multi-edit | `BeginCompound` / `EndCompound` (or `IsProcessing` + `AddCompound`) |

After programmatic edits, UI refresh is event-driven (`InvalidateMeasure` on renderer). Prefer not calling layout APIs from background threads; stay on UI thread.

### Property change surface

VM notifies `DocumentLength`, `ShowLineNumbers`, `WordWrap`, `HighlightCurrentLine`, manager computed props. Control listens for line-number visibility and wrap → scrollbar mode.

---

## Known TODOs / gaps (from source)

These are intentional or unfinished — useful when planning work:

| Area | Notes |
|------|-------|
| Line foldings | Mentioned on `TextEditorViewModel` header |
| Rectangle / column selection | Still planned (spawn one caret per line later) |
| Inline snippets | Planned |
| Word left/right keys | Enum exists; not bound in `InputManager` |
| `IsWordChar` | Hardcoded; todo to vary by document type |
| Overstrike typing | Mode toggle exists; insert path does not fully implement overwrite |
| IME preedit / surrounding text | Client stubs; `SurroundingText` empty |
| `LineManager.Clear` | Not implemented (rebuild/pool path used instead) |
| Caret scroll race | Comment: EnsureCaretVisible may run before visual layout recalc |
| Indent + undo | Indent/unindent go through `ForEachCaretEdit` / `BeginCompound` |

---

## Testing

Under `Tests/Cornerstone.UnitTests/Presentation/` (`Text/`):

- `TextEditorViewModelTests`, `TextDocumentTests`
- `CaretTests`, `CaretManagerTests`, `LineTests`
- `MultiCaretEditTests`, `MultiCaretRenderTests`, `MultiCaretInputTests`, `MultiCaretClipboardTests`
- `TokenManagerTests`
- `TerminalTests`

Prefer unit-testing **ViewModel** mutations and line/token rebuilds without UI when possible.

---

## Decision guide (where to change what)

| You want to… | Change… |
|--------------|---------|
| Insert/delete/load text | `TextEditorViewModel` mutation methods + `OnDocumentChanged` |
| Multi-caret edit | `CaretManager` + `ForEachCaretEdit` / `ProcessTextInput` / `ProcessPaste` |
| New keyboard shortcut | `InputManager` bindings or custom `KeyCommand` |
| Block edits in a region | `IReadOnlySectionProvider` |
| New gutter (breakpoints, etc.) | `Margins` control + `LeftMargins` collection |
| Selection/current-line look | `SelectionRenderer` / `CurrentLineRenderer` or theme brushes |
| Syntax colors | `TokenManager` + `Tokenizer` + `SyntaxBrushes` |
| Word wrap / metrics | `ViewMetrics`, `Line.UpdateLineMetrics`, `WordWrap` |
| Undo behavior | `UndoManager` |
| Console / REPL UX | `Terminal` / `TerminalViewModel` |
| Scroll-with-output | `TextEditorViewModel.AutoScroll` |
| Scroll position | `ViewMetrics.Offset` (never a host-side copy) |
| Paint overlay | `IRenderer` on `BackgroundRenderers` |
| Host chrome (border, font) | `TextEditor.axaml` ControlTheme |

---

## Mental model for agents

1. **ViewModel is truth** (buffer, caret, folds, scroll, AutoScroll). Lines and tokens are projections rebuilt from changes. The view is a projection of the VM.
2. **Always fire `OnDocumentChanged`** after buffer edits (or use public APIs that already do).
3. **UI is reactive:** `DocumentChanged` / `CaretMoved` / `Selection.Updated` → measure/render; do not mirror text or scroll in the control or tab.
4. **Terminal is a constrained editor:** same pipeline, plus prompt lock and command history.
5. **Compound edits** need `BeginCompound` / `EndCompound` (or `IsProcessing` + `AddCompound`) to stay undo-friendly.
6. **Offsets are absolute** into the gap buffer; line numbers are 1-based convenience on top.
7. **`Caret` is primary.** Extra carets live on `Carets`; mutate high offset first and `ShiftAfter`.
8. **Docking detaches views.** Never treat `ScrollViewer.Offset` as durable; persist `ViewMetrics.Offset`.
)