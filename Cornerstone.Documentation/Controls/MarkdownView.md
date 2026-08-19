# Markdown (Parser, Tokenizer, MarkdownView)

Cornerstone’s Markdown stack turns source text into structured blocks and paints them in Avalonia. It is built for **live streaming** (for example AI output): open constructs such as fenced code should look like the right kind of block before the closing marker arrives, and finished blocks above the stream should stay stable.

Related: [DocumentationReader.md](DocumentationReader.md) for in-app navigation over catalogued `.md` files; [DebounceAndThrottle.md](../DebounceAndThrottle.md) for the view’s parse throttle.

---

## Layers

| Layer | Role |
|-------|------|
| **Tokenizer / Parser** | Lex and structure raw markdown into tokens and blocks |
| **Formatter / extractors** | Pure text transforms (table layout, code body, headers) |
| **MarkdownView** | Avalonia control: document → block groups → presenters → text surface |

Design goals:

1. **Append-friendly document** — grow the buffer with `Append`, not string `+=` every token  
2. **Open constructs paint early** — incomplete fences become code blocks through EOF until closed  
3. **Stable UI for finished blocks** — only the growing tail should thrash on each batch  

---

## How text becomes UI

```
Document (buffer: Append / Load / DocumentChanged)
        │
        │  throttle ~100 ms
        ▼
MarkdownParser.Process
        │
        ▼
Blocks  →  MarkdownBlockGroup[] (paragraph of inlines, or one block-level node)
        │  reconcile: keep stable prefix; update growing tail in place
        ▼
ItemsControl → MarkdownBlockPresenter per group
        │
        ├── chrome (code header, quote/table border, …)
        └── TextRenderer (projection text + styles)
```

**Source vs display**

- **Document** holds the full markdown source. Block offsets refer to this buffer.  
- Each presenter owns a **projection** (markers stripped, fences removed, tables laid out for display).  
- Those buffers are not the same. Sharing one view-model across source and all presenters is incorrect.

Emphasis (`**`, `*`, `~~`, …) is expanded **in the parser**. Nested constructs become real blocks (for example links) plus emphasis flags. The view only **represents** those blocks; it does not re-parse markdown.

---

## Document API

`MarkdownView` creates a document buffer by default and exposes it as **`Document`** so hosts can bind or assign a shared buffer.

| API | Use |
|-----|-----|
| `Document` | Bind or assign a host document (for example streaming log output) |
| `Document.Append` | Streaming tokens (preferred) |
| `Document.Load` | Full replace |
| `Document.Clear` | Empty |
| `WordWrap`, `AutoScroll`, `ScrollToEnd`, `Copy` | View chrome |

There is **no** string `Markdown` property. One-shot content uses `Document.Load`.

Presenters turn off the blinking insertion caret while keeping text selectable and copyable. That is independent of read-only: a host can be editable and still hide the caret, or read-only and show a caret for keyboard position. MarkdownView always hides the caret so the surface does not look editable.

**Streaming pattern**

```text
// Bind or assign
Document="{Binding OutputDocument}"
// or markdownView.Document = host.OutputDocument;

// Then grow the buffer
token → hostDocument.Append(token)
```

Avoid rebuilding a giant string and calling `Load` on every token:

```csharp
// Bad for streams
OutputMarkdown += token;
view.Document.Load(OutputMarkdown);
```

For off-UI producers, prefer draining into `Document` on the UI projection path (for example TrackIngress / AppDispatcher) rather than touching the UI thread on every character.

---

## Parser vs tokenizer

| | **Tokenizer** | **Parser** |
|--|---------------|------------|
| Output | Tokens for highlighting and styles | Blocks (semantic ranges + offsets) |
| Primary consumer | Editors and token managers | MarkdownView grouping and presenters |

Both walk the same buffer. Fenced-code rules are shared.

**Block-level** groups (header, code fence, quote, horizontal rule, table, unordered list) are single-group nodes. Everything else (text, newlines, emphasis, inline code) collects into **paragraph** groups.

---

## Fenced code and streaming

Older fence handling waited for a closing delimiter, so mid-stream open fences looked like plain text until the final reparse. Current shared fence scanning:

| Stream state | Result |
|--------------|--------|
| 1–2 backticks | Not a fence yet |
| Opening fence (3+ ticks or tildes) at indent | **Code block** through EOF if no closer (**incomplete**) |
| Body streams in | Same incomplete block; end grows |
| Closing fence line | **Complete** code block |

Closing is line-based and CommonMark-aligned in practice: optional indent, same fence character with length at least the opening, only spaces/tabs after the fence on that line. Mid-line fence markers inside the body do **not** close the fence.

**Not open-ended by design:** bold, italic, and inline code still require closers (provisional emphasis is noisy while streaming).

---

## Presenters and chrome

| Block type | Presentation |
|------------|--------------|
| Code | Header bar + language, syntax by language, body extracted from the fence |
| Quote | Border, padding, background |
| Header | Scaled size from `#` count |
| Horizontal rule | Thin rule; source markers not shown |
| Table | Structured Avalonia grid; each cell runs inline projection so **links stay clickable** |
| List | Bullet prefix + per-item inlines |
| Paragraph | Inline markers → content + emphasis / code / link tokens |

**Copy** on code blocks uses the view’s copy command wired in code-behind (do not rely on fragile parent type bindings in merged themes).

### Tables

- **UI path:** GFM tables become a model of rows and cells and render as a grid. Cell inlines support links, emphasis, inline code, and plain text. Nested blocks inside cells show as raw text.  
- **Text/export path:** a table formatter still produces monospace ASCII tables for tests and non-UI use; MarkdownView does **not** use that for layout.

---

## Reconcile and throttle

After each throttled parse:

1. Build a new list of block groups  
2. **Stable prefix:** groups that match structure keep UI identity  
3. **Growing tail:** same type and start (for example an incomplete fence) updates in place  
4. Drop and rebuild from the first dirty index  

A **blank line** closes a paragraph for display (spacing comes from layout, not empty text lines). Soft breaks stay inside the paragraph. Trailing whitespace is trimmed from the display buffer so a final empty line is not painted when source ends with a newline.

`MarkdownView` throttles document changes (~100 ms) so high-rate appends coalesce into one parse/apply. See [DebounceAndThrottle.md](../DebounceAndThrottle.md).

Presenters **copy** `Foreground`, font, and theme chrome onto `TextRenderer` (and quote/code brushes) at apply time. Theme or density changes do not rewrite the document, so the stable-group prefix would never re-apply. Presenters listen for `Foreground`, `FontSize`, `FontFamily`, and `ActualThemeVariant` and re-apply without a reparse.

---

## Quick “how do I…?”

| Goal | Approach |
|------|----------|
| Stream tokens | `Document.Append` on a shared or bound document |
| Show incomplete code while streaming | Rely on open-fence behavior; no special UI flag |
| Full replace | `Document.Load(text)` |
| Fit table to width | `WordWrap` on the view; table presenter uses a character budget |
| Copy a code block | Header Copy → view copy command |

---

## Explicit non-goals (today)

- Full CommonMark compliance suite  
- Open-ended bold/italic while streaming  
- Incremental parse resume from last stable offset (full reparse + group reconcile only)  
- Virtualizing the block list  
- Sharing one gap buffer between a full text editor and MarkdownView without mirroring (a settable shared `Document` is the supported share path)