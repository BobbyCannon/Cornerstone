# Cornerstone : Next Release Changelog

Living feature / update log for the **next** VSIX release (currently shipping **1.1**).  
Add entries as work lands. When a release ships, move the block into a dated release section (or archive) and start a fresh “Unreleased” list.

**Target version:** 1.2  
**Related plan:** [Todo/Optimization.md](Todo/Optimization.md)

---

## Unreleased

### Features

- **Preview color and density**  
  The designer toolbar has Color and Density combos beside the Light/Dark theme combo. Color defaults to Blue (same names as `ThemeColor`, without None or Current). Density defaults to Normal (Compact, Normal, Large). The choice is shared by open designers for the Visual Studio session and is sent with each XAML update. The preview host applies color and density on the application theme.

- **Editor host (one process per Visual Studio instance)**  
  Completion, Enter-indent, and Go To Definition run in a long-lived net10 process. The extension sends the buffer and caret and applies the result. Metadata is loaded on the host from the assembly paths the designer already resolved, and cached until the next build.

- **Cornerstone process panel**  
  View → Other Windows → **Cornerstone**, and a 32px gear at the right of the status bar (left of the resize grip). One row for the shared editor host, plus one preview row per designer tab (state, document name such as `About.cxaml`). The list does not select a row. Each running row has a **Close** button. Preview activity says whether XAML was sent, accepted, or a frame was painted.

- **Attribute value completion: colors, cursors, trimming, transforms**  
  Brush and `Color` properties complete named colors (`Foreground="R"` → `Red`). `TextDecorations`, `Cursor`, `TextTrimming`, `RenderTransform` / `LayoutTransform`, `Effect`, and `CacheMode` complete their known tokens. `Image.Source` and `WindowIcon` complete image and icon paths.

- **Go To Definition on binding paths**  
  `{Binding Greeting}` with `x:DataType` opens that property (`MainViewModel.Greeting`). The caret can sit in the middle of the name. See [GoToDefinition.md](GoToDefinition.md).

- **Go To Definition on attribute values**  
  F12 on an enum value (`HorizontalAlignment="Center"`) or a named brush, color, decoration, or cursor (`Foreground="Red"`) opens that member. Plain text such as `Text="About"` is unchanged.

- **Grid definitions lightbulb, both directions**  
  Caret in `<Grid.ColumnDefinitions>` / `<Grid.RowDefinitions>` still offers **Convert to attribute** (`ColumnDefinitions="*,Auto"`). Caret on that attribute offers **Convert to element**, which puts the widths or heights back into `ColumnDefinition` / `RowDefinition` elements. Other attributes on the tag stay. A self-closing `Grid` is expanded.

- **Cornerstone: Code Cleanup (document + Solution Explorer)**  
  Tools → **Cornerstone: Code Cleanup Document** (keyboard-bindable via Environment → Keyboard, search `Cornerstone.CodeCleanupDocument`).  
  Solution Explorer right-click → **Cornerstone: Code Cleanup** on file, folder, project, or solution (batch, silent save).  
  Configurable under Tools → Options → Cornerstone: file extensions (default `.cxaml;.axaml`), trailing whitespace, final newline, line endings, XML format, sort xmlns/attributes, self-closing empty elements, indent size.  
  Structural XML rules run only when the document is well-formed; otherwise hygiene still applies. Progress via status bar + Output (Cornerstone).

- **Preview host live indicator on document tabs (opt-in)**  
  Tools → Options → **Cornerstone** → **Show previewer-running prefix on tabs** (default **off**).  
  When enabled, open AXAML tabs are prefixed with `•` while the design host is alive (e.g. `•MainView.axaml`); plain file name when stopped/suspended.

- **Modern Settings (VisualStudio.Extensibility hybrid)**  
  Cornerstone options are contributed via in-proc VisualStudio.Extensibility `Setting` definitions (category `cornerstone`) instead of legacy `ProvideOptionPage` / DialogPage.  
  VSIX is `ExtensionType="VSSDK+VisualStudio.Extensibility"`. Runtime still uses MEF `ICornerstoneSettings` via a bridge that seeds Extensibility from the old store and applies changes back.

- **StaticResource / DynamicResource: local `x:Key` completion**  
  `{StaticResource …}` and `{DynamicResource …}` (and `ResourceKey=`) complete keys defined with `x:Key` in the **current document**, in addition to the built-in theme key list. Project-wide / `ResourceInclude` keys are planned next.

- **Stop solution build on first project failure (default on)**  
  Tools → Options → **Cornerstone** → **Stop solution build on first project failure**.  
  When any project fails, the rest of the solution build is cancelled. Projects already compiling in parallel may still finish. Turn off to restore VS default (keep building remaining projects).

### Performance

- **Completion metadata starts when the solution opens**  
  The editor host reads assemblies in the background once when the solution is loaded. Each assembly is stored once in the solution's `.cscache` folder and reused by every project in that solution. A file is read again only when its size or write time changes. Older whole-set cache files in that folder are ignored.

- **Debounce + skip unchanged XAML to host**  
  Classic debounce on every edit; adaptive idle delay for large documents (300 / 500 / 750 ms by size); skip `UpdateXaml` when settled buffer matches last successfully sent XAML.

- **Suspend design host when not needed**  
  - Stop host ~2 s after the document tab is hidden, deactivated, or the window is minimized.  
  - Restart when the tab becomes active again (unless build/debug paused).  
  - Source-only view: stop host after ~15 s idle; last markup error remains for the error tagger; next edit restarts the host.  
  - Intentional suspend no longer shows a “previewer process exited” crash banner.

- **Preview frames follow the monitor**  
  The designer copies a preview frame during the WPF render pass, so a moving preview stays on the monitor refresh (60 or 120). Nothing is scheduled while the picture is still. The host renders at up to 120 Hz and keeps painting while a frame is on the socket; it sends the latest frame when the write finishes, and stops sending if the client has not acknowledged the last two. A frame that keeps the same size does not run layout. Open documents whose host is suspended still do not wake Visual Studio.

- **Remove Fit zoom modes**  
  “Fit All” and “Fit to Width” removed from the zoom list (designer + Options). Zoom is percentage-only (default **100%**). Avoids viewport resize ↔ host scaling feedback loops. Legacy saved Fit values coerce to `100%`. The designer zoom box is a selection list, not a text field. Ctrl+wheel still changes the preview; a step that is not in the list (such as 125%) leaves the combo with no selection until the zoom matches a listed level.

### Fixes

- **Cornerstone preview frames no longer sit above the keyboard**  
  Each frame posted a dispatcher callback at Render priority, which runs before input. While a preview kept painting, Visual Studio ignored the keyboard and mouse until a resize. One callback below input now installs the render hook, and later frames ride that hook. A size change lays out below input instead of inside the render pass. Hiding the tab, or switching to source only, drops the hook immediately. The preview process still waits a moment before it exits. A rebuilt designer listens on the same session again, and that listen presents frames. A pump left stopped from the previous process was acknowledging frames and dropping them.

- **Editor-host progress no longer freezes Visual Studio**  
  “Cornerstone is loading” is the editor host before the first progress line. Each metadata tick posted a UI update above the keyboard, so that line stayed up and input stopped until the window was resized. Those updates now run below input, and only one is posted at a time.

- **Closed documents stop their preview host**  
  Closing a designer tab stops the preview process in `OnClose`, instead of waiting for the pane to be disposed. The render hook is removed immediately. A host left running after the tab was gone kept posting render work above the keyboard, and Visual Studio ignored input until the window was resized.

- **Preview no longer freezes Visual Studio**  
  Frames were applied at the same priority as the keyboard, and sizing the hosted editor called `SetWindowPos` during layout. The shell stopped taking input until a resize or any key in the preview pumped another window message. Frame work now stays below input, same-size frames skip layout, and the editor size change is posted instead of sent.

- **Designer toolbar combos follow the Visual Studio theme**  
  Theme, zoom, and target use the shell dialog combo style and update with the theme. The zoom box is not editable.

- **Navigate Backward returns to the CXAML after Go to Definition**  
  F12 records the caret in the CXAML on Visual Studio’s back stack. Navigate Backward (the back button) returns to that line.

- **Namespace lightbulb uses the editor host**  
  The suggestion for a missing xmlns or alias is decided in the editor host from the metadata cache. Visual Studio only inserts the text.

- **Upgrade no longer leaves the previous extension loaded**  
  The editor host and previewer processes are stopped when Visual Studio exits, including a hard close. The installer can then delete the old extension folder instead of leaving it beside the new one (that duplicate was the “designer has no XAML resource” dialog).

- **Brush completion returned nothing**  
  `IBrush` is an interface, so it never received the `Brushes` name list. `Foreground="R"` logged `engine returned no completions` and Visual Studio dismissed the session. Named colors are attached now.

- **Preview host: SignalR.Client does not mark a project as web**  
  `Microsoft.AspNetCore.SignalR.Client` no longer classifies a desktop app as an ASP.NET web project, which had excluded it from the desktop host list.

- **Preview host: find product Desktop apps**  
  Host matching no longer requires DTE `Project` COM identity (`Contains(_project)`). Uses UniqueName, treats `Foo.Desktop` as a host for `Foo`, and inherits designer support in a fixpoint so hosts listed before their CXAML library still qualify.

- **Preview host: prefer Cornerstone.Sample.Desktop for presentation libraries**  
  Editing `Cornerstone.Presentation` / Theme / Diagnostics / Sandbox CXAML selects `Cornerstone.Sample.Desktop` when that project is a valid host, instead of the first `*.Desktop` alphabetically. Product apps still prefer their own `*.Desktop` sibling. Ranking lives in `PreviewHostSelector`.

- **Go To Definition / lightbulb on CXAML**  
  Suggested actions no longer throw `InvalidOperationException` when completion metadata has no framework xmlns (Cornerstone default xmlns is `https://github.com/BobbyCannon/Cornerstone`). VS queries the lightbulb on caret/F12; that throw was breaking the debugger and Go To Definition.  
  F12 opens the first source declaration (skipping `.g.cs` / `obj`) instead of the `"{name}" declarations` tool window.  
  Style `Selector` values resolve too: type names (`Button` in `Button.ControlCard`) and properties (`[Content=…]`).  
  Style classes (`.ControlCard` / `Classes="ControlCard"`) jump to the `Selector` that defines that class in the current document, then other XAML in the solution.  
  `Classes=""` and `Selector="Type."` complete those class names from the current document. Ctrl+Space invokes completion (it was incorrectly treated as Space, which commits/does nothing).

- **Preview host reloads after build (design data / C# code-behind)**  
  BuildDone no longer only flips `IsPaused` (that raced process kill vs start and could leave a frozen last frame). The designer now waits for the host to exit, reloads run targets, and starts a fresh process so rebuilt assemblies are loaded. Pure XAML edits still use debounced `UpdateXaml`; C# design-data changes require this recycle.

- **WPF SDK-style markup compile + designer toolbar**  
  Project sets `UseWPF=true` so Page/XAML `g.cs` participates in CoreCompile. VS MSBuild MarkupCompilePass1 cannot resolve `imaging:CrispImage` / ImageCatalog monikers from VS SDK package refs — designer chrome uses plain text toolbar labels instead (behavior unchanged).

- **TextChanged cross-thread access**  
  `ChangedOnBackground` no longer touches WPF dependency properties (`View`) or `DispatcherTimer` off the UI thread (ActivityLog `InvalidOperationException` while typing). Buffer text is snapshotted on the background thread; throttle / Source-only timer arming run on the main thread.

- **Softer preview on mid-edit XAML**  
  Typing incomplete markup (e.g. a lone `<`) no longer immediately “breaks” the preview: clearly mid-edit buffers are not pushed to the host; default debounce is 500 ms; “Invalid Markup” overlay is deferred ~800 ms after a host error; after ~1.5 s idle on still-incomplete text, a force send surfaces real errors.

- **Enter copies the line's indentation**  
  With no completion list open, Enter inserts a newline and the current line's leading tabs or spaces. An unclosed start tag gains two indent levels (two tabs, or twice the editor indent size in spaces). A finished opening tag such as `<Grid>` gains one level. Tabs stay tabs; spaces stay spaces.

- **Element completion Enter/Tab**  
  Committing a tag name (e.g. `<TextB` → TextBlock) no longer leaks Enter into VS smart-indent (spaces + stuck caret). ApplicableTo span is clamped; best match is selected for typed filter text; Enter/Tab always swallowed after commit. Enter is not swallowed when no completion session is active.

- **Element completion: leaf vs container tags**  
  - Leaf controls (`TextBlock`, `Image`, …) → `<TextBlock| />`  
  - Containers (`StackPanel`, `Grid`, `Button`, …) → `<StackPanel>|</StackPanel>` (caret between tags)

- **XAML completion commit uses `ICustomCommit`**  
  `XamlCompletion` implements Visual Studio’s `ICustomCommit`: one replace of `ApplicableTo` plus one caret move (`replaceStart + index`). The command handler only calls `session.Commit()` and swallows Enter/Tab. XML `HandleSmartIndent` after Enter (it does not count our MEF session as a completor) is suppressed for that command and the filter is re-headed so XML never sees the commit key. That was `|RequestedThemeVariant="Dark"` and the extra indent on `RequestedThemeVariant=""`.  
  Caret: property `Name="|"`, value `="Dark|"`, leaf `<TextBlock| />`, container `<Grid>|</Grid>`.  
  `CornerstoneApplication` in App.cxaml completes as CLR `Application` (`RequestedThemeVariant`, and so on).

- **Text manipulator hardened vs IntelliSense**  
  Completion-shaped inserts (`Grid></Grid>`, `TextBlock />`) no longer run start/end tag sync (was corrupting parent tags like `</UserControl>`). Bounds checks fix ActivityLog `IndexOutOfRange` on delete. Manipulators suppressed during completion apply.

### Stability / designer UX (baseline for this release train)

- **Invalid markup freeze** — ACK frames but keep last good frame; show line/col error + “paused on last valid frame”; soft-fail transport errors; host exit → pause UI and restart on next edit.
- **Frame pipeline** — present on the WPF render pass; host renders up to 120 Hz and sends the latest frame when the socket is free; ACK limits the pipe to two frames; no full pixel buffer logging; DPI/scaling sent reliably after connect; process dispose hygiene.
- **Preview surface** — skip redundant layout when size/scaling unchanged; filter tiny pointer moves; lighter `FrameReceived` path; blur shadow replaced with a simple border.

### Notes for testers

| Scenario | Expect |
|----------|--------|
| Idle designer open | Host + devenv settle; no continuous CPU thrash |
| Multiple AXAML tabs | Only the active tab keeps a live host (after short delay); tab `*` reflects that |
| Type in large AXAML | Debounced updates; no multi-second freezes from every keystroke |
| Type `<` and pause briefly | Last good preview stays; no immediate Invalid Markup flash |
| Invalid markup | Last frame frozen; error banner; fix markup → resume |
| Host process exit (crash) | Pause UI; edit restarts host; tab loses `*` until live again |
| Source-only idle | Host stops after ~15 s; `*` drops; typing brings host back |
| Logging | Default remains Information; no frame pixel dumps |
| Zoom | Percentages only (no Fit); default 100%; zoom box is a list, not a text field; resize pane does not re-scale host |
| Blank preview, no error | See [Todo/BlankPreview.md](Todo/BlankPreview.md). Host can accept XAML while the surface stays the empty 100×100 frame |
| Complete `RequestedThemeVariant` (Enter/Tab) | `RequestedThemeVariant="|"` at the original indent; value list may open |
| Complete `Dark` in that value | `RequestedThemeVariant="Dark|"` — caret not before the attribute name |

---

## How to maintain this file

1. **When you finish a user-visible change**, add a short bullet under the right heading (`Features`, `Performance`, `Fixes`, `Breaking`, etc.).
2. Prefer **what / why** over file lists; link to plans or PRs only when useful.
3. On **release**:
   - Bump all version stamps together, e.g.  
     `.\scripts\Update-ExtensionVersion.ps1 -Major X -Minor Y -Build Z`  
     (three-part `Major.Minor.Build` only; updates `Directory.Build.props`, `source.extension.vsixmanifest`, and `InstalledProductRegistration`).
   - Build and publish to Marketplace:  
     `.\scripts\Publish-Extension.ps1`  
     (uses `Cornerstone.VisualStudio/publishManifest.json`; PAT via `$env:VS_MARKETPLACE_PAT` or prior `-Login`).
   - Rename `## Unreleased` to `## x.y.z — YYYY-MM-DD`.
   - Add a fresh empty `## Unreleased` at the top.
4. Keep this focused on **ship notes**. Deep investigation and checklists stay in `Todo/`.