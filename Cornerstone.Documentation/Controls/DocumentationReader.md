# Documentation Reader

How Cornerstone hosts and navigates markdown documentation in-app.

Related: [MarkdownView.md](MarkdownView.md) (parser + Avalonia control).

---

## What it is

A small Avalonia stack that turns a set of **known** `.md` files into a clickable reader:

| Piece | Role |
|-------|------|
| **`MarkdownView`** | Renders markdown; raises `LinkClicked` for `[text](href)` (including **links inside tables**); `ScrollToHome` on new file open; `ScrollToFragment` for heading links (`Controls`) |
| **`DocumentationCatalog` / `DocumentationReader`** | Catalog + chrome (Back / Home / path / **Export**); namespace `Cornerstone.Avalonia.Documentation` |
| **`DocumentationReaderHost`** | Shared WinExe entry: bootstrap, `--export` → `Catalog.Name` folder, open-`.md`, stock window |
| **Thin host** | Application name, window title, Content packaging, optional open-arg resolver (Epic `EpicCoders/`) |
| **Embedded** | Sample `TabDocumentation` sets `Reader.Catalog` only (no `Host.Run`) |

```
┌──────────────────────────────────────────────────────────────────┐
│  WinExe hosts (Documentation / Cornerstone.* Documentation)      │
│  options + AppBuilder → DocumentationReaderHost.Run              │
│                               │                                  │
│                               ▼                                  │
│                  DocumentationReaderApplication                  │
│                  DocumentationReaderMainWindow                   │
│                               │                                  │
│  Sample TabDocumentation ─────┼── sets Catalog on control only   │
│                               ▼                                  │
│                  DocumentationReader (UserControl)               │
│                  - DocumentationCatalog                          │
│                  - NavigateTo / LinkClicked / Export             │
│                               │                                  │
│                               ▼                                  │
│                  MarkdownView (Controls)                         │
└──────────────────────────────────────────────────────────────────┘
```

---

## Catalog (known documents only)

The reader **does not** open arbitrary disk paths. Every navigable page is registered in a `DocumentationCatalog`:

- **Id / logical path** — e.g. `Readme.md`, `Agent/Sync.md`, or prefixed `cornerstone/Keystone.md`
- **Content** — usually `File.ReadAllText` for files copied next to the EXE

### Single-tree host (this project)

`DocumentationCatalog.FromDirectory(AppContext.BaseDirectory, "Readme.md")` after MSBuild copies all `**/*.md` to the output directory.

### Multi-tree host (Sample)

Sample ships the same markdown two ways so **Desktop** and **Browser (WASM) / mobile** both work:

| Packaging | Runtime load | When |
|-----------|--------------|------|
| `Content` + `CopyToOutputDirectory` → `Documents/cornerstone/**` | `DocumentationCatalog.FromDirectory` | Desktop / host filesystem present |
| `EmbeddedResource` with `LogicalName` like `Documents/cornerstone/Agent/Sync.md` | `DocumentationCatalog.FromAssemblyResources` | Browser WASM, Android, iOS (no Content filesystem) |

```xml
<!-- Desktop: files next to the app -->
<Content Include="..\Cornerstone.Documentation\**\*.md"
         Link="Documents\cornerstone\%(RecursiveDir)%(Filename)%(Extension)"
         CopyToOutputDirectory="PreserveNewest" />

<!-- All platforms (required for WASM): embed with '/' LogicalNames -->
<!-- See EmbedDocumentationMarkdown target in Cornerstone.Sample.csproj -->
```

`TabDocumentation` tries directory first, then assembly resources (`Documents/cornerstone/` prefix), then a monorepo source walk for local dev.

Use a union catalog with `idPrefix` when combining multiple documentation trees.

---

## Link rules (v1)

| Href | Behavior |
|------|----------|
| `Other.md` / `Agent/Foo.md` | Resolve relative to current document’s logical directory; navigate **only if** in catalog |
| `#heading-id` | Stay on current doc; scroll to first matching heading |
| `Other.md#heading-id` | Load other doc (if known), then scroll |
| `http://` / `https://` | Open system browser |
| Unknown / missing | Status message; **no** arbitrary file open |

Heading ids use a GFM-style slug: lowercase, spaces → `-`, drop most punctuation (`MarkdownLink.ToHeadingId`).

---

## Running this project

1. Set **Cornerstone.Documentation** as startup project (or `dotnet run --project Cornerstone.Documentation`).
2. Optional: pass a relative path argument, e.g. `Keystone.md`.

Markdown still appears in Solution Explorer for editing; F5 launches the reader.

---

## Authoring tips

- Prefer **relative links** that stay inside the packaged tree (`[Keystone](Keystone.md)`).
- Cross-tree links only work when the host catalog includes both trees and logical paths still resolve after `..` normalization.
- Sidebar tree / search are **not** required for v1 — click navigation is the product bar.
- **Export** (toolbar, top right) writes the current catalog as static HTML + generated theme CSS. Pick a parent folder; files go into a subfolder named from `DocumentationCatalog.Name` (typically `IRuntimeInformation.ApplicationName`). The site opens in the system file browser when the write succeeds.
- **CLI:** `dotnet run --project <host> -- --export <parent-dir>` (shared `DocumentationReaderHost`; writes `<parent>/<Catalog.Name>/`, exit `0` / `1`). Root `Documentation` also allows bare `--export` → `./site/<Catalog.Name>/`.
- Toolbar **color** (default Blue), **density** (Compact / Normal / Large), and **light/dark** apply the same `CornerstoneTheme` tokens the rest of the host uses. The exported site repeats those controls in the page header (`data-theme-color` / `data-theme` / `data-density`, remembered in `localStorage`). Links use `--Theme-Accent`.

---

## Code map

| Item | Location |
|------|----------|
| Link parse + heading ids | `Cornerstone/Parsers/Markdown/MarkdownLink.cs` |
| `TokenTypeLink` | `MarkdownTokenizer` / `MarkdownParser` |
| View + fragment scroll | `Cornerstone.Avalonia/Controls/MarkdownView*` |
| Catalog / reader / export / host | `Cornerstone.Avalonia/Documentation/*` (`DocumentationCatalog`, `DocumentationReader`, `DocumentationExportCommand`, `DocumentationReaderHost`, …) |
| Thin WinExes | `Documentation`, `Cornerstone.Documentation`, `Cornerstone.VisualStudio/Documentation` (`Program` + `App` stub) |
| Sample tab | `Cornerstone.Sample/Tabs/TabDocumentation` (directory + embedded resources) |