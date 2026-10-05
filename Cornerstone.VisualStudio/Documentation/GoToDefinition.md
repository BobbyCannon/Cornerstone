# Go To Definition

F12 and Peek Definition on `.cxaml` and `.axaml` in the **Cornerstone designer**. This is not Visual Studio’s C# go-to-definition, and it does not run in a plain XML editor opened with **Open With**.

Related: [AutoComplete.md](AutoComplete.md).

---

## What the caret is on

| Caret | Opens |
|-------|--------|
| Element name (`TextBlock`, `Button`) | That CLR type |
| Property or event on the tag (`Text`, `Click` as a member name) | That member. An event **value** (`Click="OnClick"`) opens the handler method on `x:Class` |
| Attribute value that is an enum member (`HorizontalAlignment="Center"`) | That enum field |
| Attribute value that is a named brush, color, decoration, or cursor (`Foreground="Red"`, `Cursor="Hand"`) | The static member those names are copied from (`Brushes.Red`, `Colors.Red`, `TextDecorations`, `StandardCursorType`) |
| Attached member (`Grid.Row`) | The owner type when the caret is on `Grid`, the member when the caret is on `Row` |
| `x:Class`, `x:DataType`, `TargetType` | That type |
| `{Binding}` / `{StaticResource}` (the extension name) | The markup-extension type |
| `{Binding Greeting}` with `x:DataType` | The property on the data-type (`MainViewModel.Greeting`). The caret can sit in the middle of the name (`Greet\|ing`) |
| `Classes=""` / `Selector="Type.class"` | The `Selector` that defines that class, in this document first, then other XAML in the solution |
| `Selector` type or `[Property=…]` | The type or the member |

`x:DataType` has to be in scope. `MainView.cxaml` sets `x:DataType="vm:MainViewModel"` on the root, so `{Binding Greeting}` on a child resolves to that view model. Without a data type, F12 on the path does nothing.

A caret on the word `Binding` still opens the markup extension, not the path.

---

## Where it runs

The designer sends the buffer text, the caret, the assembly name, and the assembly paths to the **one** editor host. The host runs `XamlGoToDefinitionResolver` on the metadata it already cached for completion. Visual Studio only navigates:

- Types and members go through the Roslyn workspace, skipping `.g.cs` / `obj` when a real source file exists
- Style classes move the caret to the matching `Selector` in a XAML buffer

If those assembly paths are not on the buffer yet, resolution stays in-process on the metadata the designer already built. Rebuild the project once after an extension update so the paths are recorded.

---

## When it does nothing

- The designer has not loaded metadata yet (no desktop host, or the project has not been built).
- The view-model assembly is not in that metadata. Build the app, then try F12 again.
- The caret is in element text, not on a name.
- The name is not a type, a member, a binding path, or a style class.

The Cornerstone output pane logs `CXAML Go To Definition: kind=...` for each attempt.
