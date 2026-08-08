# Themes (Agent)

Implementation map for color, mode, and density on Avalonia hosts.

**Product behavior:** [../Themes.md](../Themes.md)

---

## Types

| Type | Role |
|------|------|
| `CornerstoneTheme` | Style root: `ThemeColor`, `ThemeMode`, `ThemeDensity`; palette merge; density resource apply |
| `ThemeColor` | Accent enum |
| `ThemeMode` | `Default` / `Light` / `Dark` → Avalonia `ThemeVariant` |
| `ThemeDensity` | `Compact` / `Normal` / `Large` → font tokens |
| `Theme` (static helpers) | `Colors`, `ThemeModes`, `ThemeDensities`, `Get/SetThemeColor`, `Get/SetThemeDensity`, `GetCornerstoneTheme` |
| `Theme.Constants.axaml` | Default `ControlFontSize` (14), `ControlFontSizeSmall` (12) |

---

## Density apply path

```
ThemeDensity preset
  → CornerstoneTheme.SelectThemeDensity(density)
      → NormalizeThemeDensity
      → GetControlFontSize / GetControlFontSizeSmall
      → theme.Resources[ControlFontSize|Small] = …
      → Application.Current.Resources[…] = …   // live DynamicResource consumers
```

| Density | Primary | Small |
|---------|---------|--------|
| Compact | 12 | 11 |
| Normal | 14 | 12 |
| Large | 16 | 14 |

Keys: `CornerstoneTheme.ControlFontSizeKey`, `ControlFontSizeSmallKey`.

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

3. XAML chrome: `{DynamicResource ControlFontSize}` / `ControlFontSizeSmall` — **never** `StaticResource` for these tokens if density must update live.
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

## Pitfalls

| Pitfall | Fix |
|---------|-----|
| `StaticResource ControlFontSize` on Button (or any control) | Use `DynamicResource` or density never updates after load |
| Hardcoded `FontSize="12"` in feature XAML | Switch to density tokens for chrome/lists |
| Expecting density to scale code editors | Leave explicit sizes / separate controls; density is chrome tokens only unless you opt in |
| Subtree-only density via ThemeVariantScope | Not supported; set local `Resources` on a panel if you need isolation |
| Applying density before `Application.Current` / theme exists | Call again after UI load (Sample re-applies in `AppViewModel.LoadLifecycle`) |

---

## File map

| Path | Notes |
|------|--------|
| `Cornerstone.Avalonia/CornerstoneTheme.axaml(.cs)` | Theme properties + `SelectThemeDensity` |
| `Cornerstone.Avalonia/Themes/ThemeDensity.cs` | Enum |
| `Cornerstone.Avalonia/Themes/ThemeMode.cs` | Enum |
| `Cornerstone.Avalonia/Themes/Theme.cs` | Static picker arrays + get/set helpers |
| `Cornerstone.Avalonia/Themes/Theme.Constants.axaml` | Default font tokens |
| `Cornerstone.Avalonia/Controls/Button.axaml` | Must use DynamicResource for FontSize |
| `Cornerstone.Avalonia/Controls/PreviewCodeSnippet.*` | Design-time triad |
| `Tests/.../ThemeDensityTests.cs` | Size map / normalize |