# Themes

Cornerstone Avalonia apps share one theme surface for **color**, **light/dark mode**, and **UI density**. Hosts pick values at startup (and optionally at runtime); chrome and lists pick up those choices through theme resources.

---

## Three axes

| Axis | What it changes | How users usually meet it |
|------|-----------------|---------------------------|
| **Color** | Accent palette (buttons, highlights, decorative chrome) | App theme setup, sample Themes tab, design previews |
| **Mode** | Dark, light, or default (follows system when default) | Theme toggle, settings |
| **Density** | Base text size for UI chrome and lists | Settings (e.g. Compact / Normal / Large), sample Themes tab |

Color and density are Cornerstone concepts applied through `CornerstoneTheme`. Mode maps onto Avalonia’s theme variant.

---

## Density presets

Density is not a free-form font slider. Three presets keep layout predictable:

| Density | Primary text | Secondary text |
|---------|--------------|----------------|
| Compact | 12 | 11 |
| Normal (default) | 14 | 12 |
| Large | 16 | 14 |

Primary and secondary sizes are published as theme resources:

- **ControlFontSize** — body text, list primary rows, control labels  
- **ControlFontSizeSmall** — muted captions, metadata, helper copy  

Only UI that **reads those resources** (or inherits from a parent that does) changes size when density changes. Hardcoded sizes (for example `FontSize="12"`) stay fixed.

---

## How hosts apply the triad

A typical host:

1. Stores color, mode, and density in app settings.
2. On load (and when the user changes a setting), applies them to the live theme.
3. Uses dynamic theme resources in XAML for chrome and lists.

In the sample app, **Themes** (navigation) and the density combo next to the light/dark toggle demonstrate this end to end. In Cornerstone Editor, **Settings → General → UI density** persists density for that product.

Mode is scoped for a visual subtree when you use Avalonia’s theme-variant scope. Color and density are applied on the shared theme / application resources, so they affect the whole app (not a single panel) unless you deliberately set local resources.

---

## Writing UI that respects density

Prefer theme tokens for chrome:

```xml
<TextBlock FontSize="{DynamicResource ControlFontSize}" Text="Primary label" />
<TextBlock FontSize="{DynamicResource ControlFontSizeSmall}"
		Opacity="0.7"
		Text="Secondary caption" />
```

Use **DynamicResource**, not StaticResource. Static resolution freezes the size at load time, so density changes will not update the control.

Leave dedicated code or diff editors on their own size when you want monospaced content independent of chrome density.

---

## Design and preview

`PreviewCodeSnippet` can switch color, Avalonia theme variant, and density while previewing controls. Variant is scoped to the preview; color and density follow the same whole-theme apply model as the running app, which matches how products configure them.

---

## Where to try it

- **Cornerstone Sample** — **Themes** tab (color, mode, density) and the density combo next to the light/dark toggle in the navigation pane  
- **Cornerstone Editor** — Settings → General → UI density