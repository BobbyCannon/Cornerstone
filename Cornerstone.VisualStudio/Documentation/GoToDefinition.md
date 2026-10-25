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
| `{Binding Greeting}` or `{Binding Authentication.AuthenticatedAccountId}` with `x:DataType` | The property the caret is on. One name opens that property on the data type. A dotted path walks each property, so a caret in `AuthenticatedAccountId` opens that property on the type of `Authentication`. `Path=` and a leading `!` belong to the same path. The caret can sit in the middle of a name (`Authenticat\|edAccountId`) |
| `Classes=""` / `Selector="Type.class"` | The `Selector` that defines that class, in this document first, then other XAML in the solution |
| `Selector` type or `[Property=…]` | The type or the member |

`x:DataType` has to be in scope. A nearer `DataType`, such as on a `DataTemplate`, wins over the one on the root. `AboutView.cxaml` sets `x:DataType="views:AboutViewModel"`, so `{Binding Authentication.AuthenticatedAccountId}` walks `AboutViewModel.Authentication` and opens `AuthenticatedAccountId` on that property's type. A property that is only declared on a base interface opens that interface member: `{Binding RuntimeInformation.DeviceId}` opens `ISyncClientDetails.DeviceId`. The caret can sit on any segment of the path. Without a data type, F12 on the path does nothing.

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
- The binding segment is `$parent`, `#name`, an indexer, or a cast. F12 follows plain property names from `x:DataType`.

The Cornerstone output pane logs `CXAML Go To Definition: kind=...` for each attempt.
