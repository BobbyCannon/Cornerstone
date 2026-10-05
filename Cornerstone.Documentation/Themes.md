# Themes

Cornerstone apps share one theme surface for **color**, **light/dark mode**, and **UI density**. That surface lives in `Cornerstone.Presentation.Theme` — the default chrome package plus product controls. Hosts pick values at startup (and optionally at runtime); chrome and lists pick up those choices through theme resources.

---

## Three axes

| Axis | What it changes | How users usually meet it |
|------|-----------------|---------------------------|
| **Color** | Accent palette (buttons, highlights, decorative chrome) | App theme setup, sample Themes tab, design previews |
| **Mode** | Dark, light, or default (follows system when default) | Theme toggle, settings |
| **Density** | Base text size for UI chrome and lists | Settings (e.g. Compact / Normal / Large), sample Themes tab |

Color and density are Cornerstone concepts applied through `CornerstoneTheme`. Mode maps onto Cornerstone.Presentation’s theme variant.

---

## Density presets

Density is not a free-form font slider. Three presets keep layout predictable:

| Density | Primary | Secondary | Emphasis |
|---------|---------|-----------|----------|
| Compact | 12 | 11 | 14 |
| Normal (default) | 14 | 12 | 16 |
| Large | 16 | 14 | 18 |

Normal sizes live in `Theme.Constants.axaml`. Compact and Large are derived from those defaults (`CornerstoneTheme.GetControlFontSize` / `GetControlFontSizeSmall` / `GetControlFontSizeLarge`).

Published theme resources:

- **ControlFontSize** — body text, list primary rows, control labels  
- **ControlFontSizeSmall** — muted captions, metadata, helper copy  
- **ControlFontSizeLarge** — section titles and other emphasis  

Only UI that **reads those resources** (or inherits from a parent that does) changes size when density changes. Hardcoded sizes (for example `FontSize="12"`) stay fixed.

---

## How hosts apply the triad

A typical host:

1. Stores color, mode, and density in app settings.
2. On load (and when the user changes a setting), applies them to the live theme.
3. Uses dynamic theme resources in XAML for chrome and lists.

In the sample app, **Themes** (navigation) and the density combo next to the light/dark toggle demonstrate this end to end. In Cornerstone Editor, **Settings → General → UI density** persists density for that product.

Mode is scoped for a visual subtree when you use Cornerstone.Presentation’s theme-variant scope. Color and density are applied on the shared theme / application resources, so they affect the whole app (not a single panel) unless you deliberately set local resources.

---

## Writing UI that respects density

Prefer theme tokens for chrome:

```xml
<TextBlock FontSize="{DynamicResource ControlFontSize}" Text="Primary label" />
<TextBlock FontSize="{DynamicResource ControlFontSizeSmall}"
		Opacity="0.7"
		Text="Secondary caption" />
<TextBlock FontSize="{DynamicResource ControlFontSizeLarge}" Text="Section title" />
```

Use **DynamicResource**, not StaticResource. Static resolution freezes the size at load time, so density changes will not update the control.

Leave dedicated code or diff editors on their own size when you want monospaced content independent of chrome density.

---

## Bundled fonts

Typefaces live in `Cornerstone.Presentation.Theme/Assets/Fonts`. How they are laid out is driven by Cornerstone.Presentation `FontFamily` URIs, not by taste:

- **Family folder** (URI ends at the folder, no `.ttf`) — Cornerstone.Presentation loads every file in the folder as one family. Regular, Bold, Italic, and Oblique are real files, so `FontWeight.Bold` is not a synthetic outline. Used for **DejaVu Sans Mono** (`DejaVuSansMono/`) and **Open Sans** (`OpenSans/`).
- **Single file** (URI includes `.ttf`) — one cut only. Used for **DejaVu Sans Light** (there is no other cut) and for **Open Sans Light** (the Light file is named so Light is the family default instead of Regular from the folder).

Do not drop a lone Regular `.ttf` at the Fonts root and point `FontFamily` at it if the UI uses Bold or Italic. That is what made Bold look fuzzy before the family folders were added.

Theme keys: `DejaVuSansMono`, `DejaVuSansLight`, `OpenSansRegular`, `OpenSansLight` in `Themes/Fonts.axaml` and matching `CornerstoneTheme` static properties.

---

## Design and preview

`PreviewCodeSnippet` can switch color, Cornerstone.Presentation theme variant, and density while previewing controls. Variant is scoped to the preview; color and density follow the same whole-theme apply model as the running app, which matches how products configure them.

The Visual Studio designer toolbar has those same three choices. Theme defaults to Default (follow the operating system). Color defaults to Blue. Density defaults to Normal. The choice lasts for the Visual Studio session and is applied to the preview host.

---

## Where to try it

- **Cornerstone Sample** — **Themes** tab (color, mode, density) and the density combo next to the light/dark toggle in the navigation pane  
- **Cornerstone Editor** — Settings → General → UI density