Document rewrite lives in `Cornerstone.Text.Formatting`. Tokenize first (`Cornerstone.Text.Parsing`), then walk tokens and emit a new string.

Do not confuse this with `Cornerstone.Presentation.Media.TextFormatting` (Skia line layout) or `Cornerstone.Text.StringFormatter` / `TextFormat`.

```
IStringBuffer + Tokenizer
        │
        ▼
DocumentFormatter.TryFormat(tokenizer, buffer, options)
        │
        ▼
language IDocumentFormatter (JSON, XML, …)
        │
        ▼
formatted string  →  TextEditorViewModel.FormatDocument(options)
```

`FormatSettings.Resolve(extension)` picks the persisted profile (JSON / XML / XAML / HTML / …). General holds line-ending and indent defaults copied into profiles at construct time.

JSON pretty-prints or minifies. XML/CXAML/HTML share `XmlDocumentFormatter` (`XmlFormatDialect`). C#, Markdown, and PowerShell formatters are identity (pass-through) except they still participate in the registry.

Apply through `TextEditorViewModel.FormatDocument`: compound undo of Remove+Insert. Never `Load` (that is Reset and clears undo).
