# MarkdownView / Markdown Parser (Agent)

Dense reference for implementing or changing Markdown parsing and the Avalonia `MarkdownView`.

**Product behavior:** [../Controls/MarkdownView.md](../Controls/MarkdownView.md)  
**Text editor document model:** [TextEditor.md](TextEditor.md)

---

## Purpose / out of scope

**In scope:** fenced code streaming, block grouping, document-driven view, table fit/wrap, presenter chrome, agent/sample wiring.

**Out of scope here:** full CommonMark spec work, HTML export details (`MarkdownRendererForHtml`), generic `TextProcessor` API design.

---

## Architecture

```
Document (TextEditorViewModel.Buffer)
    DocumentChanged → Throttle(100ms)
        MarkdownParser.Process()
        BuildGroups → ReconcileGroups → RebuildBlocksFromGroups
        MarkdownBlockPresenter.Apply (per group / ContentChanged / table reflow)
```

| Type | Role |
|------|------|
| `MarkdownView` | Owns/settable `Document`; throttle; pool blocks/groups; reconcile |
| `MarkdownBlockGroup` | UI unit; `MatchesStructure` / `MatchesGrowingTail`; `ContentChanged` |
| `MarkdownBlockPresenter` | Chrome + projection into `TextRenderer` |
| `MarkdownFence` | Open/complete fenced code (shared parser/tokenizer) |
| `MarkdownParser` | `Block` stream for view |
| `MarkdownTokenizer` | `Token` stream + type id registry |
| `MarkdownTableFormatter` | Column width + wrap (pure string) |
| `MarkdownRenderer.Extract*` | Code body/language, header size/content |

---

## File map

```
Cornerstone/Parsers/Markdown/
  MarkdownFence.cs              ★ incomplete + line-based closers
  MarkdownParser.cs
  MarkdownTokenizer.cs
  MarkdownTableFormatter.cs
  MarkdownRenderer.cs
  MarkdownRendererForHtml.cs
  MarkdownService.cs
  MarkdownOptions.cs

Cornerstone/Parsers/
  Parser.cs, Tokenizer.cs, TextProcessor.cs, Block.cs, Token.cs
  Parsers.md

Cornerstone.Avalonia/Controls/
  MarkdownView.axaml(.cs)
  MarkdownBlockGroup.cs
  MarkdownBlockPresenter.axaml(.cs)
  MarkdownViewTokenizer.cs
  MarkdownBlockConverter.cs     legacy; view template uses Presenter

Tests/Cornerstone.UnitTests/Parsers/Markdown/
  MarkdownParserTests.cs        includes char-by-char stream tests
  MarkdownTokenizerTests.cs
  MarkdownTableFormatterTests.cs
  MarkdownRendererTests.cs
```

---

## Checklist: change fence / streaming behavior

1. Edit **`MarkdownFence.TryRead`** only (keep parser/tokenizer thin wrappers).  
2. Preserve offsets: `[contentRegionStart, contentRegionEnd]` for `ExtractCodeBlockInfo`.  
3. Incomplete: `EndOffset == buffer.Count`, `ContentRegionEnd == buffer.Count`, still `TokenTypeCodeBlock`.  
4. Closing: own line, same char, length ≥ open, optional spaces only after fence.  
5. Update **`ParseCodeBlockCharacterByCharacterStream`** (+ incomplete tests).  
6. Do **not** open-end bold/italic without product decision.  

---

## Checklist: change MarkdownView render path

1. Prefer `Document.Append` / `Load` (no string property on the control).  
2. Parse from **`Document.Buffer`**, not a copied string buffer.  
3. Group reconcile before pooling blocks (blocks owned by live groups).  
4. Presenter applies projection; avoid pure-binding converters with side effects.  
5. Tables: **`WordWrap = false`** on table renderer; budget from view width with slack; reflow on bounds.  
6. Copy button: wire in code-behind to `MarkdownView.CopyCommand` — never `$parent[local:MarkdownView]` in merged resource dictionaries.  

---

## Checklist: agent / streaming host

1. Host holds `TextEditorViewModel` (e.g. `LogForOutput` / `OutputDocument`) **or** uses view-owned `Document`.  
2. If shared: bind `Document="{Binding LogForOutput}"` **or** assign `markdownView.Document = hostDocument` (`Document` is a DirectProperty).  
3. Stream with `Append`; clear with `Clear`/`Load`.  
4. Optional: `TextIngress` + drain once per dispatcher tick for off-UI producers.  
5. Do not rebuild a string and call `Document.Load` every token.  

---

## Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Require closing fence before CodeBlock | Code chrome pops only at end of fence | `MarkdownFence` incomplete-at-EOF |
| `string += token` then full reload | GC thrash every token | `Document.Append` |
| Soft wrap on preformatted table | Blank line between every table row | Table presenter: `WordWrap = false` |
| First layout `Bounds.Width == 0` | Tiny/wrong table width stuck forever | Unconstrained until usable width; reflow |
| `$parent[local:MarkdownView]` in merged theme | `Unable to resolve namespace for type local:MarkdownView` | Code-behind `FindAncestorOfType` |
| Pool blocks still referenced by groups | Corrupted blocks / wrong text | Pool only when group discarded; rebuild `Blocks` from groups |
| Structure match too strict on growing fence | Full group tear-down each append | `MatchesGrowingTail` + `UpdateFrom` |
| Mid-line `` ``` `` closes fence | Broken streaming code bodies | Line-based closer only |
| Character stream 1–2 backticks | Expect CodeBlock too early | Need ≥ 3 fence chars |
| InlineCode block without `Offsets` | Backticks visible; no code style | `TryProcessDelimitedInlineSelection` must set content offsets `[contentStart, contentEnd]` |
| List dumped as raw markdown | `**` / `` ` `` show as markers in bullets | `ProjectUnorderedList` + per-item `ProjectFragment` |
| HR not in `IsBlockLevel` | `---` appears as paragraph text | Treat `TokenTypeHorizontalRule` as block-level + border chrome |

---

## Key APIs (quick)

```csharp
// Stream
markdownView.Document.Append(token);

// Share host buffer (or bind Document="{Binding LogForOutput}" in XAML)
markdownView.Document = agentViewModel.LogForOutput;

// One-shot full replace
markdownView.Document.Load(fullText);

// Fence (parser/tokenizer)
MarkdownFence.TryRead(buffer, position, out MarkdownFenceMatch match);
// match.IsComplete, StartOffset, EndOffset, ContentRegionStart/End
```

**Offsets for code blocks**

- `ExtractCodeBlockInfo(buffer, block)` uses `Offsets[0]` / `Offsets[1]` as content region (after open fence markers → closer or EOF).  
- Incomplete: still extract language once opening line has newline.

---

## Tests to run after Markdown changes

```text
dotnet test Cornerstone/Tests/Cornerstone.UnitTests/Cornerstone.UnitTests.csproj --filter "FullyQualifiedName~Markdown"
```

Must keep green:

- Incomplete + complete fences  
- Character-by-character stream  
- Mid-line fence does not close  
- Table formatter suite  
- `ExtractCodeBlockInfo`  

---

## Related

- Throttle: [../DebounceAndThrottle.md](../DebounceAndThrottle.md)  
- Text document/render: [TextEditor.md](TextEditor.md)  
- Ingress: `Cornerstone/Text/TextIngress.cs`, `DispatchableViewModel.TrackIngress`