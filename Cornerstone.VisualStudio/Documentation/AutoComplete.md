# Auto complete features

Cornerstone’s XAML IntelliSense for `.cxaml` and `.axaml` files opened in the **Cornerstone designer**. It is not Visual Studio’s C# completion, and it is not the generic XML schema list.

---

## How to open the list

| Action | Result |
|--------|--------|
| Type `<` | Element names for the current xmlns |
| Type inside an open tag | Properties, events, attached members, `xmlns` |
| Type `=` or `"` on an attribute | Value list when one exists (enums, brushes, classes, …) |
| Type `{` in a value | Markup extensions (`Binding`, `StaticResource`, …) |
| **Ctrl+Space** (or Edit → IntelliSense → Complete Word) | Force the list at the caret, including inside `Classes=""` |
| Ctrl+J | Same invoke as Complete Word (List Members) |

Typing letters in **element content** (between tags) does not open the list. That keeps everyday text from stalling the UI. Use `<` or Ctrl+Space when you want markup completion.

Completion needs **type metadata** from the previewer host. If the designer has not found a desktop host yet, element and property lists stay empty even though the editor is open. Style **class** names can still appear from XAML in the solution (see below).

---

## What you get

### Elements

After `<`, the list is non-abstract types from the document’s xmlns.

- **Leaf** controls (`TextBlock`, `Image`, …) insert a self-closing tag: `<TextBlock />` with the caret before `/>`.
- **Containers** (`StackPanel`, `Grid`, `Button`, …) insert a matching end tag: `<StackPanel></StackPanel>` with the caret between the tags.
- `</` offers the matching close tag.
- `<Parent.` offers property-element syntax (`Grid.RowDefinitions`, …).
- `<!--` is offered as a comment.

Enter or Tab commits without inserting a newline or extra indent.

### Attributes

Inside an open start tag, completion lists:

- Properties and events on that type
- Attached properties / events (`Grid.Row`, …)
- XAML directives (`x:Name`, …) when they apply
- `xmlns:` and type prefixes for attached members

Committing a property inserts `Name=""` and leaves the caret between the quotes (`Name="|"`), then opens value completion when a list exists. Completing a value (for example `Dark`) leaves the caret after the value (`RequestedThemeVariant="Dark|"`).

### Attribute values

Depends on the property:

| Context | Completions |
|---------|-------------|
| Brush properties (`Foreground`, `Background`, …) | Named brushes from `Brushes` (`Red`, `RoyalBlue`, `Transparent`, …). Typing `R` inside the quotes filters to `Red`. |
| `Color` properties | Named colors from `Colors`, the same way |
| `TextDecorations` | `Underline`, `Strikethrough`, `Overline`, `Baseline` |
| `Cursor` | Standard cursor names (`Hand`, `Ibeam`, `Arrow`, …) |
| `TextTrimming` | `None`, `CharacterEllipsis`, `WordEllipsis`, `PrefixCharacterEllipsis`, `LeadingCharacterEllipsis`, `PathSegmentEllipsis` |
| `RenderTransform` / `LayoutTransform` | `translate`, `scale`, `rotate`, `skew`, `matrix`, and the `X` / `Y` forms |
| `Effect` | `blur(`, `drop-shadow(` |
| `CacheMode` | `BitmapCache` |
| `Image.Source`, `WindowIcon`, bitmap properties | Image and icon paths from the loaded assemblies |
| Other hint values (enums, booleans, …) | Known values from metadata |
| `Classes=""` | Style class names (see [Style classes](#style-classes)) |
| `Selector=""` | Types, `.` classes, `:pseudoclasses`, `[Property=`, combinators |
| `TargetType` / type-valued properties | Type names |
| Event handlers | `<New Event Handler>` (writes a method in the code-behind) |
| `xmlns` / `xmlns:` | Known namespaces, `clr-namespace:`, `using:` |
| `x:Class` | Types in the current assembly |
| `Setter` `Property` / `Value` | Style setter members and values |
| `OnPlatform` / `OnFormFactor` `Options` | Platform / form-factor names |

### Markup extensions

Inside `{…}`:

- Extension names (`Binding`, `StaticResource`, `DynamicResource`, `RelativeSource`, …)
- Properties of the current extension (`Path=`, `Mode=`, …)
- `{StaticResource …}` / `{DynamicResource …}` / `ResourceKey=` — `x:Key` values from the **current document**, plus built-in theme keys. Keys from other files / `ResourceInclude` are not listed yet.

### Style classes

`Classes=""` and `Selector="Type."` complete class names from:

1. `Selector="…"` in the current document (`Button.ControlCard`, `^.Search`, …)
2. Other `Classes="foo bar"` usages in the current document
3. `.cxaml` / `.axaml` elsewhere in the **open solution** (theme files included)

Attached-property dots in selectors (`[(Theme.Color)=None]`) are not treated as class names.

The solution list is built in the background when the extension loads. If the first Ctrl+Space inside `Classes=""` is empty, invoke it once more after the designer has been open a moment.

Class names shipped only as compiled assets in a NuGet (no XAML in the solution) are not discovered.

---

## Committing a match

These keys commit the selected item when the list is open: **Space**, **Tab**, **Enter**, `'`, `"`, `=`, `>`, `.`, `#`, `)`, `]`.

| Insert | Caret |
|--------|--------|
| Property `RequestedThemeVariant=""` | `RequestedThemeVariant="\|"` at the **original** indent |
| Enum / hint value `Dark` | `RequestedThemeVariant="Dark\|"` (not before the attribute name) |
| Leaf element | `<TextBlock\| />` |
| Container | `<StackPanel>\|</StackPanel>` |

- Enter and Tab never insert a newline or extra indent on commit. Completing a property must not add XML attribute-align indent (`                |RequestedThemeVariant=""`). Completing `Dark` must not leave the caret at `|RequestedThemeVariant="Dark"`.
- After a self-closing element (`<TextBlock />`), the commit key does not start another session.
- After `Classes=""` (or any `Name=""` insert), value completion opens between the quotes.
- XML auto-quotes: typing `"` over an existing quote moves into the value and opens the list.
- Arrow keys and typing work immediately after commit (the caret is not held).

Escape or an empty list dismisses. Enter/Tab with **no** session behave as normal editing.

If the engine has nothing to offer, the log says `engine returned no completions` and Visual Studio follows with `Completion session dismissed during Start`. That is an empty list, not a crash. Brush and color names are part of that list only after metadata has been built. `IBrush` is an interface, so it is not loaded as a normal type. The completion metadata attaches the `Brushes` names to it. Reopen the designer file after an extension update so that metadata is built again. A caret after the closing quote (`Foreground="Red"|`) is past the value, so the color list does not open. Keep the caret inside the quotes (`Foreground="R|"`).

The root tag in `App.cxaml` is `CornerstoneApplication`. Completion treats that as the CLR type `Application`, so members such as `RequestedThemeVariant` (`Default` / `Light` / `Dark`) complete there.

### How commit is implemented

Visual Studio’s default commit replaces `CompletionSet.ApplicableTo` with `InsertionText` and leaves the caret at the **end** of that text. It does not read a caret offset. Mid-insert caret is ours: `XamlCompletion` implements **`ICustomCommit`**. `session.Commit()` calls `Commit()`, which replaces the applicable span and places the caret at `replaceStart + index` in `InsertionText`.

The command filter decides **whether** to commit and swallows Enter/Tab so they are not typed. It must not do a second buffer replace, pin `Caret.PositionChanged`, scan the line for `="`, or `MoveTo` after `ICustomCommit`.

The designer buffer is XML content type. VS 2026’s XML `ViewFilter.HandlePostExec` still runs after our commit when XML is ahead in the command chain. `IsCompletorActive` only sees XML’s own completion UI, not our MEF session, so Enter used to call `HandleSmartIndent`: extra leading whitespace plus `SetCaretPos` at the start of the attribute (`|RequestedThemeVariant="Dark"` / `                |RequestedThemeVariant=""`). Those two cases are fixed: the filter is re-headed so we swallow Enter/Tab first, and the text view indent style is set to None for that command (`XmlEditorSmartIndent`). The XML language indent preference stays off while a designer view is open and is restored when the last one closes. It is applied after the view is created, not inside the commit command. `SetUserPreferences` on that stack re-enters the shell and the UI stays frozen until another window message arrives. Do not bring back line scans, caret pinning, delayed indent restore, or a language-preference change inside the command filter.

Related types: `XamlCompletion` (`ICustomCommit`), `XamlCompletionSource` (`ApplicableTo` span), `XamlCompletionCommandHandler` (commit keys), `XmlEditorSmartIndent`, `CompletionCaretPlacement` (pure caret math, unit-tested).

---

## Go To Definition

F12 and Peek are documented in [GoToDefinition.md](GoToDefinition.md). They use the same metadata as completion. Binding paths follow `x:DataType`. Enum values and named brushes (`Center`, `Red`) open that member.

---

## Limits

- Works in the Cornerstone designer tab, not in a plain XML editor opened with **Open With**.
- Element/property/enum lists need a successful metadata load (desktop preview host). Rebuild if types are missing after you add controls.
- Letters in element **content** do not auto-open the list; use `<` or Ctrl+Space.
- `{StaticResource}` keys are current-document `x:Key` plus theme keys, not the whole project.
- Snippets (Avalonia DP/AP/RoutedEvent) are separate from this list; they insert from the snippet manager.

Related: [NextRelease.md](NextRelease.md) (ship notes), [ExtensibilityPlatform.md](ExtensibilityPlatform.md) (why this stays on classic VSSDK).