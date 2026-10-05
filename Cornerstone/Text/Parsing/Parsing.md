Parsing lives in `Cornerstone.Text.Parsing` (tokenizers, parsers, renderers). Document rewrite lives in `Cornerstone.Text.Formatting` (see [Formatting.md](../Formatting/Formatting.md)).

Tokenizer will tokenize text into tokens.
Parser will produce Blocks. Blocks are sets of tokens that represents semantic meaning.
Renderer will process (Tokens/Blocks) into other output like HTML, Xaml, etc.

```
        Raw Text
           |
       Processor
     /           \
    /             \
Tokenizer        Parser
   ↓                ↓
List<Token>      Blocks / AST
   ↓                ↓
Syntax           Renderer
Highlighting        ↓
Document         Final Output (HTML, XAML, ...)
formatter
(`Cornerstone.Text.Formatting`)
```

Definitions:
- AST: Abstract Syntax Tree
- Token: Smallest part of text
	- Ex. '#': header token, ' ': white space, "Header": text
- Block: Set of tokens with specific meaning
	- Ex. '# Header' of tokens[#, White Space, Header]
