Just a quick documentation explaining the classes in Parsers.

Tokenizer will tokenize text into tokens.
Parser will produce Blocks. Blocks are sets of tokens that represents symatic meaning.
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
(etc)            Final Output (HTML, XAML, ...)
```

Definitions:
- AST: Abstract Syntax Tree
- Token: Smallest part of text
	- Ex. '#': header token, ' ': white space, "Header": text
- Block: Set of tokens with specific meaning
	- Ex. '# Header' of tokens[#, White Space, Header]
