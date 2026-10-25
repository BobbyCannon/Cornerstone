# Changelog

## 3.0.297.20556 - 2026-10-25

### Breaking

- `IWindowsHelloService` and `WindowsHelloService` are gone. Vault unlock goes through `IBiometricKeyService`. Windows, Android, and iOS register a platform implementation. Other hosts get `BiometricKeyServiceStub`.
- The Windows challenge file is `Biometric.challenge`. An existing `WindowsHello.challenge` is not read. A vault enrolled on 3.0.277 is set up again.
- `NugetService.QueryForPackageAsync` no longer takes `includePrerelease`. NuGet cache `GetOrAddAsync` factories now receive a `CancellationToken`.
- Browser hosts no longer load `Cornerstone.Browser.js`. Gamepad and storage script come from the module bundle (`browserHost.ts`). The sample `index.html` dropped that script tag.

### Added

- Linux and macOS gamepads on the desktop runtime. Linux reads evdev (`/dev/input/event*`) and prefers a real pad over Steam's virtual Xbox 360 pad. macOS uses Game Controller. The browser pad reads `navigator.getGamepads`. Android stays a stub.
- `IScreenSampler` copies a square of screen pixels around the pointer on Windows, Linux, and macOS.
- Linux video plays through system LibVLC. X11 draws into the child window. Wayland copies frames into the subsurface. The H.264 plugin check accepts the same plugin directories as playback, including Debian multiarch (`/usr/lib/<triplet>/vlc/plugins`).
- Docking can float a tab into its own window and dock it back. `SingleViewFloatWindows` shows only the tab content in the float. `ShowTabStrip` hides the tab headers.
- Clipboard text can clear itself after a delay when that same text is still on the clipboard. A later copy that does not ask for a timeout keeps the text, including a second copy of the same string.
- Text carets use the styled run widths the renderer painted, so bold, italic, and proportional text no longer leave the caret short of the glyphs. Column-add stacks carets from the active caret. A delete shifts the other carets before the document notification so an end caret is not moved twice.
- Sync issues record `Side` (`Client` or `Server`). A child row in the same apply group waits until a parent added in that group has a key.
- SQL indexes can name more than one column. `SqlDatabase.Execute` runs one command on the open SQLite connection.
- On plain `net10.0`, `RuntimeInformation` reports Windows, Linux, or macOS instead of `Unknown`.
- Sample theme skins. `Cornerstone.Presentation.Themes.Extras` ships a Halloween theme that uses the product control templates. The sample swaps skins in place so the window does not rebuild layout.
- `NugetUpdate` decides whether a package version range can move, and what text to write. Restore still takes the lowest version a range allows. An update raises that lower bound.
- Direct reference to `System.Security.Cryptography.Pkcs` 10.0.13 so restore does not stay on the builds flagged by GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7. The Cornerstone package depends on 10.0.13, so a consumer of `Microsoft.Data.SqlClient` 7.1.1 does not stay on 9.0.18. Build tasks keep the private pin because they ship their own span types.
- The cross-platform template lists the shared project and the Desktop, Android, Browser, and iOS hosts as primary outputs, and Visual Studio treats it as a solution. Desktop is the default startup project.

### Changed

- `Restart-WebsiteAppPool` starts a stopped pool. Recycle on a stopped pool threw `0x800710D8`.
- Remote Link on Android types text through `input text` without backslash escapes. Spaces are still `%s`.
- Solid rounded-rect strokes are filled as the band between the outer and inner edges, so corners stay the same width and opacity as the straight edges. Dashed strokes still use the arc stroke.
- Extended window decorations draw a one-pixel border inset. Maximized and full-screen windows drop that inset.
- Docking tab strips use a top border only, and the strip follows `ShowTabStrip`.
- The play bar theme was rebuilt. Seek and volume chrome use fixed colors (`#FFA34E0C`, `#FF101010`, `#FFFF8614`) instead of theme brushes.
- Theme subclasses load their own XAML. The markup compiler rewrites `CornerstoneXamlLoader.Load` in the method that contains the call, so `CornerstoneTheme.LoadThemeXaml` is virtual.
- The database sync client dropped the per-session change-count cache.

### Dependency updates

- Presentation: MessagePack 3.1.12, HarfBuzzSharp 14.2.1.301, WebView2 1.0.4258.31, AndroidX Camera 1.6.2, AppCompat 1.8.0, CsWin32 0.3.363, NWayland 0.12.5, Tmds.DBus 0.95.1.
- Sample, MediaPlayer, and Navigator desktop hosts: HarfBuzzSharp.NativeAssets.Linux 14.2.1.301.
- Cornerstone: SqlClient 7.1.1, Sqlite 10.0.13, MAUI Essentials 10.0.110, System.Management and ProtectedData 10.0.13.
- LibVLCSharp 3.10.2 and VideoLAN.LibVLC.Windows 3.0.24.
- Extension: Avalonia and Avalonia.Templates 12.1.4.
