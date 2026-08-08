# Cornerstone : Next Release Changelog

Living feature / update log for the **next** VSIX release (currently shipping **1.1**).  
Add entries as work lands. When a release ships, move the block into a dated release section (or archive) and start a fresh “Unreleased” list.

**Last updated:** 2026-08-06  
**Target version:** 1.2  
**Related plan:** [Todo/Optimization.md](Todo/Optimization.md)

---

## Unreleased

### Features

- **Cornerstone: Code Cleanup (document + Solution Explorer)**  
  Tools → **Cornerstone: Code Cleanup Document** (keyboard-bindable via Environment → Keyboard, search `Cornerstone.CodeCleanupDocument`).  
  Solution Explorer right-click → **Cornerstone: Code Cleanup** on file, folder, project, or solution (batch, silent save).  
  Configurable under Tools → Options → Cornerstone: file extensions (default `.axaml;.xaml`), trailing whitespace, final newline, line endings, XML format, sort xmlns/attributes, self-closing empty elements, indent size.  
  Structural XML rules run only when the document is well-formed; otherwise hygiene still applies. Progress via status bar + Output (Cornerstone).

- **Preview host live indicator on document tabs (opt-in)**  
  Tools → Options → **Cornerstone** → **Show previewer-running prefix on tabs** (default **off**).  
  When enabled, open AXAML tabs are prefixed with `•` while the design host is alive (e.g. `•MainView.axaml`); plain file name when stopped/suspended.

- **Modern Settings (VisualStudio.Extensibility hybrid)**  
  Cornerstone options are contributed via in-proc VisualStudio.Extensibility `Setting` definitions (category `cornerstone`) instead of legacy `ProvideOptionPage` / DialogPage.  
  VSIX is `ExtensionType="VSSDK+VisualStudio.Extensibility"`. Runtime still uses MEF `ICornerstoneSettings` via a bridge that seeds Extensibility from the old store and applies changes back.

- **StaticResource / DynamicResource: local `x:Key` completion**  
  `{StaticResource …}` and `{DynamicResource …}` (and `ResourceKey=`) complete keys defined with `x:Key` in the **current document**, in addition to the built-in theme key list. Project-wide / `ResourceInclude` keys are planned next.

### Performance

- **Debounce + skip unchanged XAML to host**  
  Classic debounce on every edit; adaptive idle delay for large documents (300 / 500 / 750 ms by size); skip `UpdateXaml` when settled buffer matches last successfully sent XAML.

- **Suspend design host when not needed**  
  - Stop host ~2 s after the document tab is hidden, deactivated, or the window is minimized.  
  - Restart when the tab becomes active again (unless build/debug paused).  
  - Source-only view: stop host after ~15 s idle; last markup error remains for the error tagger; next edit restarts the host.  
  - Intentional suspend no longer shows a “previewer process exited” crash banner.

- **Remove Fit zoom modes**  
  “Fit All” and “Fit to Width” removed from the zoom list (designer + Options). Zoom is percentage-only (default **100%**). Avoids viewport resize ↔ host scaling feedback loops. Legacy saved Fit values coerce to `100%`. Ctrl+wheel still steps fixed percentages.

### Fixes

- **Preview host reloads after build (design data / C# code-behind)**  
  BuildDone no longer only flips `IsPaused` (that raced process kill vs start and could leave a frozen last frame). The designer now waits for the host to exit, reloads run targets, and starts a fresh process so rebuilt assemblies are loaded. Pure XAML edits still use debounced `UpdateXaml`; C# design-data changes require this recycle.

- **WPF SDK-style markup compile + designer toolbar**  
  Project sets `UseWPF=true` so Page/XAML `g.cs` participates in CoreCompile. VS MSBuild MarkupCompilePass1 cannot resolve `imaging:CrispImage` / ImageCatalog monikers from VS SDK package refs — designer chrome uses plain text toolbar labels instead (behavior unchanged).

- **TextChanged cross-thread access**  
  `ChangedOnBackground` no longer touches WPF dependency properties (`View`) or `DispatcherTimer` off the UI thread (ActivityLog `InvalidOperationException` while typing). Buffer text is snapshotted on the background thread; throttle / Source-only timer arming run on the main thread.

- **Softer preview on mid-edit XAML**  
  Typing incomplete markup (e.g. a lone `<`) no longer immediately “breaks” the preview: clearly mid-edit buffers are not pushed to the host; default debounce is 500 ms; “Invalid Markup” overlay is deferred ~800 ms after a host error; after ~1.5 s idle on still-incomplete text, a force send surfaces real errors.

- **Element completion Enter/Tab**  
  Committing a tag name (e.g. `<TextB` → TextBlock) no longer leaks Enter into VS smart-indent (spaces + stuck caret). ApplicableTo span is clamped; best match is selected for typed filter text; Enter/Tab always swallowed after commit. Enter is not swallowed when no completion session is active.

- **Element completion: leaf vs container tags**  
  - Leaf controls (`TextBlock`, `Image`, …) → `<TextBlock| />`  
  - Containers (`StackPanel`, `Grid`, `Button`, …) → `<StackPanel>|</StackPanel>` (caret between tags)

- **Element completion avoids XML smart-indent**  
  Manual buffer replace plus **restore original line indent** if the editor grows leading tabs/spaces on commit.

- **Text manipulator hardened vs IntelliSense**  
  Completion-shaped inserts (`Grid></Grid>`, `TextBlock />`) no longer run start/end tag sync (was corrupting parent tags like `</UserControl>`). Bounds checks fix ActivityLog `IndexOutOfRange` on delete. Manipulators suppressed during completion apply.

### Stability / designer UX (baseline for this release train)

- **Invalid markup freeze** — ACK frames but keep last good frame; show line/col error + “paused on last valid frame”; soft-fail transport errors; host exit → pause UI and restart on next edit.
- **Frame pipeline** — coalesce/drop intermediate frames with ACK; ~60 FPS UI frame notifications (safe with single live host per active tab); no full pixel buffer logging; DPI/scaling sent reliably after connect; process dispose hygiene.
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
| Zoom | Percentages only (no Fit); default 100%; resize pane does not re-scale host |

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