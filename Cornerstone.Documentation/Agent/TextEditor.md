# Text Editor (`Cornerstone.Avalonia.Text`)

Dense reference for implementing or changing the Avalonia text editor and terminal.

**Primary location:** `Cornerstone.Avalonia/Text/`

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
                    ├── Caret + Selection
                    ├── InputManager    key bindings → commands
                    ├── UndoManager
                    ├── Clipboard / Indention / Completion managers
                    └── ViewMetrics     layout metrics (char size, viewport, extent)
```

| Layer | Type | Responsibility |
|-------|------|----------------|
| Control | `TextEditor` / `TextEditor<T>` | Template, margins, scroll helpers, `Text` property, IME client, `IsReadOnly` |
| Surface | `TextRenderer` | Measure/arrange, paint tokens, pointer/keyboard, logical scroll, caret blink |
| Document VM | `TextEditorViewModel` | Mutations, `DocumentChanged` pipeline, managers |
| Storage | `StringGapBuffer` | O(1)-ish insert/delete near gap; source of truth for characters |
| Structure | `LineManager` / `Line` | Logical lines, wrap points, visual rects, hit-testing |
| Style | `TokenManager` + `Tokenizer` | Syntax tokens; rebuild on document change |
| Terminal | `Terminal` + `TerminalViewModel` | Prompt lock, command history, ANSI colors |

**Design rule:** mutate the **ViewModel buffer** (via Insert/Remove/Load/Append), never paint or invent parallel text state. Everything else rebuilds from `DocumentChanged`.

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
    Caret.cs, CaretMoveDirection.cs, Selection.cs
    Line.cs, LineManager.cs
    TokenManager.cs, UndoManager.cs
    ClipboardManager.cs, IndentionManager.cs, CompletionManager.cs

  Input/
    InputManager.cs          Default key bindings
    KeyCommand.cs            ICommand wrapper for KeyEventArgs
    IReadOnlySectionProvider.cs

  Rendering/
    IRenderer.cs             Background draw plug-in
    CaretVisual.cs, CurrentLineRenderer.cs, SelectionRenderer.cs
    TextMetrics.cs           ViewMetrics + GetAdvance()

  Margins/
    Margin.cs, LineNumberMargin.cs

  History/
    CommandHistory.cs, CommandHistoryProvider.cs   (terminal command history, not undo)

  Terminal.axaml(.cs), TerminalViewModel.cs, TerminalTokenizer.cs
```

**Related:** Markdown rendering reuses `TextEditorViewModel` / `TextRenderer` as the document and paint surface — see [MarkdownView.md](MarkdownView.md) (agent) and [../Controls/MarkdownView.md](../Controls/MarkdownView.md) (product).

**Related (outside Avalonia Text folder):**

- `Cornerstone/Text/StringGapBuffer.cs` — buffer
- `Cornerstone/Parsers/Token.cs`, `Tokenizer.cs`, `IndentionService.cs`, `CompletionService.cs`
- `Cornerstone.Avalonia/Themes/SyntaxBrushes.cs`, `SyntaxColor.*.axaml`
- Unit tests: `Tests/Cornerstone.UnitTests/Avalonia/Text/`

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
- `AutoScroll`: on document change posts `ScrollToEnd`; user scroll up turns it off

### `TextEditorViewModel`

Central API for **all** document edits. Marked `[Updateable(UpdateableAction.All, ["*"])]` for Keystone/dispatcher integration.

Managers constructed in the ctor:

| Manager | Role |
|---------|------|
| `Lines` | Line index rebuild + measure |
| `TokenManager` | Syntax tokens (optional tokenizer) |
| `Caret` | Offset, preferred visual X, selection |
| `InputManager` | Key gesture → command table |
| `UndoManager` | Stack of change groups |
| `Clipboard` | Cut/copy/paste via `ClipboardService` |
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
        ├── Reset → Caret.Reset(), UndoManager.Clear()
        ├── else if UndoManager.Enabled → UndoManager.Add(args)
        │         (skipped when UndoManager.IsProcessing)
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
| `Delete(offset, forward)` | Selection first, else backspace/delete (handles `\r\n`) |
| `ProcessTextInput(text)` | Remove selection, insert, move caret |
| `HandleEnterKey()` | Insert `\r\n`, optional smart indent |
| `Indent()` / `Unindent()` | Caret or multi-line selection (compound undo) |

**When adding features that change text:** call these APIs (or mutate `Buffer` then `OnDocumentChanged`). Do not leave Lines/Tokens out of sync.

### Compound edits

Multi-line indent/unindent:

1. Set `UndoManager.IsProcessing = true` (blocks per-change undo entries)
2. Apply each buffer change + `OnDocumentChanged` (still rebuilds lines/tokens)
3. `UndoManager.AddCompound(changes)` for one undo unit
4. Clear `IsProcessing`

`TryRemoveSelection` is a no-op while `IsProcessing` (avoids double-delete during undo/compound).

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

Hit-testing: `GetNearestOffsetAtVisual(visualX, visualY, isAtEndOfLine)`.

---

## Caret and selection

### `Caret`

- `Offset` is the document index (clamped 0..`Buffer.Count`).
- `_preferredVisualX` preserved across vertical moves.
- `IsAtEndOfLine` disambiguates wrap boundary (same offset = end of previous visual row vs start of next).
- `UpdateVisualLayout()` → line’s `UpdateCaretVisual`.
- Events: `CaretMoved` (renderer ensures visible + updates selection while keyboard-selecting).

Movements (`CaretMoveDirection`): char L/R, line U/D, page U/D, line start/end, smart line start (Home), document start/end. Word left/right exist on the enum but are **not wired** in `InputManager` yet.

### `Selection`

- Inclusive start, exclusive-ish end via offsets; `Length = Abs(End - Start)`.
- Keyboard: Shift + navigation sets `IsSelectingUsingKeyboard`.
- Mouse: `StartMouseSelection` / `StopMouseSelection` from renderer/margin.
- `Updated` event → renderer invalidates for selection paint.

### Typing over selection

`ProcessTextInput` and `Delete` call `TryRemoveSelection` first (unless undo is processing).

---

## Input path

```
KeyDown (TextRenderer)
  → ViewModel.ProcessKeyDownEvent
      → Selection.ProcessKeyDown (Shift tracking)
      → InputManager.ProcessKeyArgs (first matching KeyBinding)

TextInput (TextEditor)
  → ViewModel.ProcessTextInput (if not IsReadOnly)

Pointer (TextRenderer)
  → hit-test line → Caret.Move / Selection update / double-click SelectWord

Ctrl+Wheel (TextRenderer)
  → FontSize 12..40
```

### Default bindings (`InputManager.InitializeBindings`)

| Gesture | Action |
|---------|--------|
| Arrows / Shift+Arrows | Move / extend selection |
| Home / End (+ Ctrl/Shift) | Smart line start, line end, document start/end |
| PageUp/Down (+ Shift) | Page move |
| Ctrl+A | Select all |
| Enter / Return | `HandleEnterKey` |
| Back / Delete | Delete backward / forward |
| Ctrl+X/C/V | Cut / Copy / Paste |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| Insert | Toggle overstrike |
| Tab / Shift+Tab | Indent / Unindent |

To add shortcuts: `InputManager.AddBinding(gesture, new KeyCommand(...))` or `RemoveBinding`.

`KeyCommand` marks `KeyEventArgs.Handled` by default (`willHandle: true`).

---

## Undo / redo

- Stacks of `TextDocumentChangedArgs[]` (LIFO queues).
- Single edits → one-element arrays; multi-line indent → compound arrays.
- **Undo** replays inverse (Add↔Remove), reverse order within compound.
- **Redo** reapplies original changes.
- While processing: `IsProcessing = true` so nested `OnDocumentChanged` does not push new undo entries.
- `Load` / Reset clears stacks.
- Disable recording: `UndoManager.Enabled = false`.

**Caveat:** caret position after undo is not fully restored as an independent snapshot; undo moves caret to change offset during remove/insert replay.

---

## Rendering pipeline

1. **Measure:** sample `"X"` layout → `ViewModel.Measure` → char metrics + `Lines.Measure` → `DocumentSize` / `Viewport`.
2. **Scroll:** `TextRenderer` is `ILogicalScrollable`; `Offset` syncs to `ViewMetrics.Offset`.
3. **Render order:**
   - Background `IRenderer`s: current line, selection
   - Visible lines only (`GetVisualLines` by Y range)
   - Per visual subline: tokens from `TokenManager.GetTokens`, styled via `SyntaxBrushes` / token foreground
   - `CaretVisual` child (blink timer 500ms when focused)

### Extending background paint

Implement `IRenderer.Draw` and add to `TextRenderer.BackgroundRenderers`.

### Selection paint (must follow soft wrap)

Selection is painted by `SelectionRenderer` using the **same layout authority** as caret and wrap:

- Walk logical lines in the selection range
- For each line, `Line.GetSelectionRects(start, end)` emits one rect per **visual subline**
- X advances use `ViewMetrics.GetAdvance`; Y uses `VisualLayout.Top + subIndex * CharacterHeight`

Do **not** re-layout the line with Avalonia `TextLayout.HitTestTextRange` for selection — that re-wraps with a different algorithm and desyncs highlights from soft wrap.

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
| `CharacterHeight` / `CharacterWidth` | Measured from sample `TextLayout` |
| `DocumentSize` | Sum of line visual layouts |
| `Viewport` | Available size from measure |
| `Offset` | Scroll position from renderer |

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
| Insert at caret | `Insert(text)` or `ProcessTextInput` |
| Insert at offset | `Insert(offset, text)` then `Caret.Move(...)` if needed |
| Delete range | `RemoveAt(offset, length)` |
| Atomic multi-edit | `IsProcessing` + multiple changes + `AddCompound` |

After programmatic edits, UI refresh is event-driven (`InvalidateMeasure` on renderer). Prefer not calling layout APIs from background threads; stay on UI thread.

### Property change surface

VM notifies `DocumentLength`, `ShowLineNumbers`, `WordWrap`, `HighlightCurrentLine`, manager computed props. Control listens for line-number visibility and wrap → scrollbar mode.

---

## Known TODOs / gaps (from source)

These are intentional or unfinished — useful when planning work:

| Area | Notes |
|------|-------|
| Line foldings | Mentioned on `TextEditorViewModel` header |
| Multi-cursor | Planned |
| Inline / rectangle snippets | Planned |
| Word left/right keys | Enum exists; not bound in `InputManager` |
| `IsWordChar` | Hardcoded; todo to vary by document type |
| Overstrike typing | Mode toggle exists; insert path does not fully implement overwrite |
| IME preedit / surrounding text | Client stubs; `SurroundingText` empty |
| `LineManager.Clear` | Not implemented (rebuild/pool path used instead) |
| Caret scroll race | Comment: EnsureCaretVisible may run before visual layout recalc |
| Indent selection + undo | Compound undo added after individual `OnDocumentChanged` calls (individual adds may still hit stack when not processing correctly — follow existing indent pattern carefully) |

---

## Testing

Under `Tests/Cornerstone.UnitTests/Avalonia/Text/`:

- `TextEditorViewModelTests`, `TextDocumentTests`
- `CaretTests`, `LineTests`
- `TokenManagerTests`
- `TerminalTests`

Prefer unit-testing **ViewModel** mutations and line/token rebuilds without UI when possible.

---

## Decision guide (where to change what)

| You want to… | Change… |
|--------------|---------|
| Insert/delete/load text | `TextEditorViewModel` mutation methods + `OnDocumentChanged` |
| New keyboard shortcut | `InputManager` bindings or custom `KeyCommand` |
| Block edits in a region | `IReadOnlySectionProvider` |
| New gutter (breakpoints, etc.) | `Margins` control + `LeftMargins` collection |
| Selection/current-line look | `SelectionRenderer` / `CurrentLineRenderer` or theme brushes |
| Syntax colors | `TokenManager` + `Tokenizer` + `SyntaxBrushes` |
| Word wrap / metrics | `ViewMetrics`, `Line.UpdateLineMetrics`, `WordWrap` |
| Undo behavior | `UndoManager` |
| Console / REPL UX | `Terminal` / `TerminalViewModel` |
| Scroll-with-output | `TextEditor.AutoScroll` |
| Paint overlay | `IRenderer` on `BackgroundRenderers` |
| Host chrome (border, font) | `TextEditor.axaml` ControlTheme |

---

## Mental model for agents

1. **Buffer is truth.** Lines and tokens are projections rebuilt from changes.
2. **Always fire `OnDocumentChanged`** after buffer edits (or use public APIs that already do).
3. **UI is reactive:** `DocumentChanged` / `CaretMoved` / `Selection.Updated` → measure/render; do not mirror text in the control.
4. **Terminal is a constrained editor:** same pipeline, plus prompt lock and command history.
5. **Compound edits** need `IsProcessing` + `AddCompound` to stay undo-friendly.
6. **Offsets are absolute** into the gap buffer; line numbers are 1-based convenience on top.
)