# Document formatting (`Cornerstone.Text.Formatting`)

Dense reference for document pretty-print / minify. Used by the text editor; the engine has no UI dependency.

**Code:** `Cornerstone/Cornerstone/Text/Formatting/`  
**Parsing:** `Cornerstone/Cornerstone/Text/Parsing/` ([colocated Parsing.md](../../Cornerstone/Text/Parsing/Parsing.md))  
**Editor apply:** [TextEditor.md](TextEditor.md) (`FormatDocument`)  
**Host:** the app that owns the editor applies the result and stores per-extension settings

Not Skia `Presentation.Media.TextFormatting`. Not `Cornerstone.Text.StringFormatter`.

---

## Purpose / out of scope

**In:** rewrite a buffer from tokenizer tokens + `DocumentFormatOptions`. JSON pretty/minify. XML / CXAML / XAML / HTML pretty/minify. Per-extension settings. Tab indent string from the same options.

**Out:** format on save, format selection, text reflow, EditorConfig, C#/Markdown/PowerShell structure rewrite (identity formatters). A C# pretty-printer would be a Roslyn pass outside this walker.

---

## Architecture

```
FormatSettings.Resolve(extension)     persisted profiles (the host owns the instance)
        │
        ▼
DocumentFormatter.TryFormat(tokenizer, buffer[, tokens], options)
        │
        ▼
IDocumentFormatter (JSON / XML / identity …)
        │
        ▼
string  →  TextEditorViewModel.FormatDocument(options)
              BeginCompound → RemoveAt(0,n) + Insert(0, formatted) → EndCompound
```

| Type | Role |
|------|------|
| `IDocumentFormatter` | `CanFormat(Tokenizer)` + `Format(buffer, tokens, options)` |
| `DocumentFormatter` | Static registry; `GetFormatter` / `TryFormat` |
| `DocumentFormatOptions` | Indent char/count, `LineEndingMode`, optional explicit `NewLine` |
| `FormatSettings` | `General` + `Json` / `Xml` / `Xaml` / `Html` / `CSharp` / `Markdown` / `PowerShell` |
| `IdentityDocumentFormatter` | Return `buffer.ToString()` |
| `MarkdownTableFormatter` | Table column fit/wrap (not a document formatter) |

**Resolve order:** json → csharp → **xaml** (`cxaml`/`axaml`/`xaml`) → **html** (`html`/`htm`) → remaining `XmlTokenizer.Extensions` → markdown → powershell. Xaml/html must be checked before generic XML because those extensions share `XmlTokenizer`.

---

## Options

| Setting | Where | Notes |
|---------|--------|--------|
| `LineEndingMode` Detect / Lf / Crlf | General + each profile | Detect: buffer contains `\r\n` → CRLF, else LF. Non-empty `NewLine` wins (tests). |
| `IndentCharacter`, `IndentCount` | same | `Indent(depth)` / `Indent(1)` is the tab string for `IndentionManager.SetIndent` |
| JSON `Minify`, `SpaceAfterColon` | `JsonFormatOptions` | Space after colon ignored when minify |
| XML `Minify`, `AttributesOnNewLine`, `SpaceBeforeEmptyClose`, `Dialect` | `XmlFormatOptions` | Xaml profile defaults `AttributesOnNewLine`. Html profile `Dialect = Html` |

Tab PropertyGrid binds the **same instance** as Settings for that extension. Editing a `.cxaml` tab changes the Xaml profile globally.

Invalid input is best-effort (still returns a string). Do not invent missing tags.

---

## Apply (`TextEditorViewModel.FormatDocument`)

- No-op if read-only, no tokenizer, no formatter, or output equals buffer (no undo spam).
- Reuse `TokenManager` tokens when `Count > 0`; else `tokenizer.Process()`.
- **Never `Load`.** Reset clears undo.
- Caret clamped; selection reset.
- Host: `EditorTabViewModel.FormatDocumentCommand`; File menu + Ctrl+E then Ctrl+C (`ApplicationShortcuts["Format Document"]` via `ShortcutBindingManager` on `AppWindow`). Bare Ctrl+C stays Copy.

`html`/`htm` are on `XmlTokenizer.Extensions` so highlighting and format share one tokenizer.

---

## Language coverage

| Language | Formatter | Behavior |
|----------|-----------|----------|
| JSON | `JsonDocumentFormatter` | Pretty or minify; empty `{}`/`[]` stay compact |
| XML / csproj / slnx | `XmlDocumentFormatter` dialect Xml | Token walk; comments/CDATA/PI/doctype verbatim; `xml:space="preserve"` inner verbatim |
| CXAML / AXAML / XAML | same, Xaml profile | Attributes on new lines by default |
| HTML / HTM | same, Html dialect | Void elements; preserve `pre`/`script`/`style`/`textarea` |
| C# / Markdown / PowerShell | identity | Registry hit; text unchanged |

XML mixed content: text stays; child elements get new indented lines. No HTML5 recovery / optional-tag omission.

---

## How to add a language

1. Tokenizer with `Extensions` + `Tokenizer.GetByExtension`.
2. `*FormatOptions : DocumentFormatOptions` and `*DocumentFormatter : IDocumentFormatter` (or identity stub).
3. Register in `DocumentFormatter` array.
4. Add profile on `FormatSettings` + `Resolve` branch.
5. Tests under `Tests/Cornerstone.UnitTests/Text/Formatting/`.

---

## Pitfalls

- `Load` / `TextDocumentChangeType.Reset` clears undo — format must compound Remove+Insert.
- Chord Ctrl+E, Ctrl+C is application-level, not `InputManager`.
- `Cornerstone.Parsers` namespace is gone; use `Cornerstone.Text.Parsing` / `.Formatting`.
- Do not put rewrite logic on `TextEditorViewModel` or the Editor tab.

---

## File map

```
Cornerstone/Text/Formatting/
  IDocumentFormatter.cs, DocumentFormatter.cs, IdentityDocumentFormatter.cs
  DocumentFormatOptions.cs, LineEndingMode.cs, FormatSettings.cs
  Json*, Xml* (including XmlFormatDialect), CSharp*, Markdown*, PowerShell*
  MarkdownTableFormatter.cs
  Formatting.md

Cornerstone/Text/Parsing/          tokenizers; XmlTokenizer.Extensions includes html/htm
Tests/Cornerstone.UnitTests/Text/Formatting/

Editor host:
  Settings/TextEditorSettings.Format
  Settings/SettingsView.cxaml          General + JSON/XML/XAML/HTML grids
  Editors/EditorTabViewModel           Resolve + FormatDocumentCommand + indent
  Settings/ApplicationShortcuts        "Format Document"
```
