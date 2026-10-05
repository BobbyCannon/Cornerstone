# Editor behavior

How typing should work in the Cornerstone designer source view (`.cxaml` and `.axaml`).

This is the spec. When a note here and the editor disagree, the note is what we are aiming for. Add examples in the same shape as the ones below so the caret and the whitespace are unambiguous.

`|` is the caret. It is not a character in the file.

Write the line with the same tabs or spaces you care about. A space-indented example must stay spaces. A tab-indented example must use a real tab. Say which indent size the example assumes when the new line gains a level (the editor uses **Tools → Options → Text Editor → Tabs → Indent size** for that extra level of spaces).

```
Before
                    <SelectableTextBlock|

After (indent size 4)
                    <SelectableTextBlock
                            |
```

---

## Enter

Enter with the completion list closed inserts a newline and copies the leading whitespace of the current line. Tabs stay tabs. Spaces stay spaces. The editor does not reindent the line you left.

How far the new line indents depends on what the next line is for.

Child content sits **one** level deeper than the element. A finished opening tag (`<Grid>`, not `<Grid/>` and not `</Grid>`) is that case: the next line is a child.

An attribute on its own line sits **two** levels deeper than the element. Children are already one level in, so attributes go one past that and stay visually distinct from child elements. This applies when the caret is still inside a start tag that has no `>` yet (the element name, or an attribute already on that tag).

Everywhere else the new line repeats the current line’s indent and stops there. A closing tag does not outdent. A blank line uses the previous line’s indent. An attribute line that is already two levels in keeps that indent; Enter does not add another two.

Completing a name from the popup with Enter or Tab does **not** insert a newline. That case stays in [AutoComplete.md](AutoComplete.md).

### Open start tag

```
Before
                    <SelectableTextBlock|

After (indent size 4)
                    <SelectableTextBlock
                            |
```

```
Before
		<SelectableTextBlock|

After
		<SelectableTextBlock
				|
```

The second example is tabs: two tabs on the element, four on the attribute line (two levels, so it sits past a child).

### Attribute already on the next line

The attribute line is already one level deeper. Another Enter keeps that indent. It does not add a second extra level.

```
Before
		<SelectableTextBlock
				Width="1"|

After
		<SelectableTextBlock
				Width="1"
				|
```

### Finished opening tag

```
Before
		<Grid>|

After
		<Grid>
			|
```

### No extra level

```
Before
		Hello|

After
		Hello
		|
```

```
Before
		</Grid>|

After
		</Grid>
		|
```

```
Before
		<Grid/>|

After
		<Grid/>
		|
```

### Caret in the middle of the line

Text after the caret stays on the new line, after the inserted indent.

```
Before
		Hello| world

After
		Hello
		| world
```

---

## More behavior

Add a heading per key or gesture. One short rule, then Before / After pairs. Useful next topics: quote skipping, self-closing versus container tags on commit, and paste.
