# Themes (Agent)

Implementation map for color, mode, and density on Cornerstone.Presentation hosts. Default chrome lives in `Cornerstone.Presentation` (`CornerstoneTheme`; do not copy upstream Fluent or Simple themes).

**Product behavior:** [../Themes.md](../Themes.md)

---

## Control theme files

A control's chrome lives in `Cornerstone.Presentation/Theme/Controls/<Control>.cxaml`, one file per control name. `Button.cxaml` and `PlayBar.cxaml` are the shape to copy. The class stays in `Controls`. An `x:Class` view is not the theme.

The file is a `Styles` document:

1. `ControlTheme` with `x:Key="{x:Type <Control>}"` goes inside `Styles.Resources`. That is the default template and the setters that belong to the theme.
2. Selectors that must apply from outside the template go after `</Styles.Resources>`, as siblings of the resource dictionary. `Theme.Color` and class styles (`Button.Flat`, `Button.Icon`) go there. Nested inside the `ControlTheme`, those rules do not match descendants or the rest of the tree.
3. Template parts are named `PART_*`. The class finds them in `OnApplyTemplate` and detaches the previous template's handlers before wiring the new ones.
4. `Theming/Controls.cxaml` includes the file with `StyleInclude` and `csres://Cornerstone.Presentation/Theme/Controls/<Control>.cxaml`. A `Styles` file is not a `MergeResourceInclude`.
5. End the file with a design preview so the control can be opened on its own:

```xml
<Design.PreviewWith>
    <PreviewCodeSnippet MaxWidth="500">
    </PreviewCodeSnippet>
</Design.PreviewWith>
```

Put a live instance of the control inside `PreviewCodeSnippet`. When the control has `Theme.Color` or class variants, add `PreviewCodeSnippet.ItemTemplate` the way `Button.cxaml` does. Update this preview whenever the template or those outer styles change.

The preview variant combo defaults to `Default`. That value follows the system app theme (on Windows, `AppsUseLightTheme` in `Win32PlatformSettings`), not a fixed light theme and not the host window's forced variant. `UISettings` background stays light until the process opts into dark mode, so it is not the system app theme. Dark and Light in the combo stay explicit.

A change to a theme file or to `PreviewCodeSnippet` shows up after rebuilding `Cornerstone.Presentation` and restarting the designer previewer. It does not require a new Visual Studio extension. The previewer process loads Presentation from the build output.

The control is a `TemplatedControl`. Behavior stays in the class. Hosts keep the same tag. Class views still loaded with `CornerstoneXamlLoader.Load(this)` stay next to their code-behind until they are migrated.

---

## Types

| Type | Role |
|------|------|
| `CornerstoneTheme` | Style root: `ThemeColor`, `ThemeMode`, `ThemeDensity`; palette merge; density resource apply |
| `ThemeColor` | Accent enum |
| `ThemeMode` | `Default` / `Light` / `Dark` → Cornerstone.Presentation `ThemeVariant` |
| `ThemeDensity` | `Compact` / `Normal` / `Large` → font tokens |
| `Theme` (static helpers) | `Colors`, `ThemeModes`, `ThemeDensities`, `Get/SetThemeColor`, `Get/SetThemeDensity`, `GetCornerstoneTheme` |
| `Theme.Constants.axaml` | Normal `ControlFontSize` (14), `ControlFontSizeSmall` (12), `ControlFontSizeLarge` (16) |
| `Themes/Fonts.axaml` | `FontFamily` resources; URIs must match folder vs single-file layout |
| `Assets/Fonts/` | Family folders (`DejaVuSansMono/`, `OpenSans/`) vs single file (`DejaVuSansLight.ttf`) |

---

## Density apply path

```
ThemeDensity preset
  → CornerstoneTheme.SelectThemeDensity(density)
      → NormalizeThemeDensity
      → GetControlFontSize / GetControlFontSizeSmall / GetControlFontSizeLarge
      → theme.Resources[ControlFontSize|Small|Large] = …
      → Application.Current.Resources[…] = …   // live DynamicResource consumers
```

| Density | Primary | Small | Large token |
|---------|---------|--------|-------------|
| Compact | 12 | 11 | 14 |
| Normal | 14 | 12 | 16 |
| Large | 16 | 14 | 18 |

Keys: `CornerstoneTheme.ControlFontSizeKey`, `ControlFontSizeSmallKey`, `ControlFontSizeLargeKey`.

---

## Host checklist

1. Persist `ThemeColor`, `ThemeMode`, `ThemeDensity` on app settings.
2. After settings load (and on change): apply triad, e.g.

```csharp
var theme = Theme.GetCornerstoneTheme();
if (theme != null)
{
	theme.ThemeColor = settings.ThemeColor;
	theme.ThemeMode = settings.ThemeMode;
}
CornerstoneTheme.SelectThemeDensity(settings.ThemeDensity);
```

3. XAML chrome: `{DynamicResource ControlFontSize}` / `ControlFontSizeSmall` / `ControlFontSizeLarge` — **never** `StaticResource` for these tokens if density must update live.
4. Do not use `AppBootstrap.GetInstance` from feature code for theme services; apply from host lifecycle / settings that already hold the values.

### Sample reference

| Piece | Location |
|-------|----------|
| Settings | `Cornerstone.Sample/Keystone/State/AppSettings` — triad + `ApplyTheme()` |
| Nav density + mode toggle | `AppView.axaml` / `AppViewModel.ToggleThemeMode` |
| Discovery UI | `Tabs/TabThemes` |
| Bootstrap theme defaults | `App.axaml` → `<CornerstoneTheme … />` (overridden after load by settings) |

### Editor reference

| Piece | Location |
|-------|----------|
| Settings property | `AppSettings.ThemeDensity` → `SelectThemeDensity` on set/load |
| UI | Settings → General → UI density |
| SC list tokens | SourceControl views/popups use DynamicResource tokens |

---

## PreviewCodeSnippet

| Property | Apply model |
|----------|-------------|
| `ThemeVariant` | Local `ThemeVariantScope` |
| `ThemeColor` | `Theme.SetThemeColor` (global theme) |
| `ThemeDensity` | `Theme.SetThemeDensity` → `SelectThemeDensity` (global resources) |

Pickers: `Theme.Colors`, `Theme.ThemeVariants`, `Theme.ThemeDensities`.

---

## Visual Studio previewer

The designer toolbar sends the same three choices on `UpdateXamlMessage`: `ThemeVariant` (Default / Light / Dark), `ThemeColor` (ThemeColor name, default Blue), and `ThemeDensity` (Compact / Normal / Large, default Normal). The choice is remembered for the Visual Studio session and shared by open designers. Each change resends the current XAML.

`DesignWindowLoader.ApplyThemeOverride` applies them before the preview window is shown. Variant still forces a concrete Light or Dark (Default follows the operating system). Color and density set `IApplicationTheme` on the previewed app when that app has a `CornerstoneTheme`. An empty or unknown color or density leaves the loaded application theme alone.

---

## Pitfalls

| Pitfall | Fix |
|---------|-----|
| `StaticResource ControlFontSize` on Button (or any control) | Use `DynamicResource` or density never updates after load |
| Hardcoded `FontSize="12"` in feature XAML | Switch to density tokens for chrome/lists |
| Expecting density to scale code editors | Leave explicit sizes / separate controls; density is chrome tokens only unless you opt in |
| Subtree-only density via ThemeVariantScope | Not supported; set local `Resources` on a panel if you need isolation |
| Applying density before `Application.Current` / theme exists | Call again after UI load (Sample re-applies in `AppViewModel.LoadLifecycle`) |
| `Theme.GetCornerstoneTheme()` inside `namespace Cornerstone.Presentation.Theme` | `Theme` is the parent namespace. Use `Theming.Theme.GetCornerstoneTheme()` |
| Solution still lists Toolkit | `*.slnx` (not only `*.sln`): `Cornerstone.slnx`, `Cornerstone.VisualStudio.slnx` |
| `FontFamily` URI to a single Regular `.ttf` when Bold/Italic is used | Point at the family folder (`Assets/Fonts/DejaVuSansMono`, `Assets/Fonts/OpenSans`). A single file makes Bold synthetic and fuzzy |

---

## File map

| Path | Notes |
|------|--------|
| `Cornerstone.Presentation/Theme/CornerstoneTheme.cxaml(.cs)` | Theme properties + `SelectThemeDensity` |
| `Cornerstone.Presentation/Theming/ThemeDensity.cs` | Enum (`namespace Cornerstone.Presentation.Theme.Theming`) |
| `Cornerstone.Presentation/Theming/ThemeMode.cs` | Enum |
| `Cornerstone.Presentation/Theming/Theme.cs` | Static picker arrays + get/set helpers |
| `Cornerstone.Presentation/Theming/Theme.Constants.cxaml` | Default font tokens |
| `Cornerstone.Presentation/Theming/Fonts.cxaml` | Family resources; comment on folder vs `.ttf` URIs |
| `Cornerstone.Presentation/Assets/Fonts/` | Family folders vs `DejaVuSansLight.ttf`; do not re-add unused root OpenSans files |
| `Cornerstone.Presentation/Theme/Controls/Button.cxaml` | Must use DynamicResource for FontSize |
| `Cornerstone.Presentation/Controls/PreviewCodeSnippet.*` | Design-time triad |
| `Cornerstone.VisualStudio/.../Views/CornerstoneDesigner.*` | Toolbar combos; session-shared strings |
| `Cornerstone.Presentation.Remote.Protocol/.../DesignMessages.cs` | `UpdateXamlMessage` theme fields |
| `Cornerstone.Designer.HostApp/DesignWindowLoader.cs` | Applies variant, color, and density |
| `Tests/.../ThemeDensityTests.cs` | Size map / normalize |