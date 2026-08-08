# Documentation Reader

How Cornerstone hosts and navigates markdown documentation in-app.

Related: [MarkdownView.md](MarkdownView.md) (parser + Avalonia control).

---

## What it is

A small Avalonia stack that turns a set of **known** `.md` files into a clickable reader:

| Piece | Role |
|-------|------|
| **`MarkdownView`** | Renders markdown; raises `LinkClicked` for `[text](href)` (including **links inside tables**); `ScrollToHome` on new file open; `ScrollToFragment` for heading links |
| **`DocumentationCatalog`** | Registry of documents the reader is allowed to open |
| **`DocumentationReader`** | Thin chrome (Back / Home / path) + load + link resolution; mouse **X1 (back)** goes through history like the Back button |
| **Host** | Documentation WinExe, Sample tab, or any Avalonia app that supplies a catalog |

```
┌──────────────────────────────────────────────────────────────────┐
│  Hosts                                                            │
│  ┌─────────────────┐ ┌──────────────────┐ ┌────────────────────┐ │
│  │ Cornerstone.    │ │ Documentation    │ │ Sample             │ │
│  │ Documentation   │ │ (Epic) WinExe    │ │ TabDocumentation   │ │
│  │ WinExe          │ │                  │ │                    │ │
│  └────────┬────────┘ └────────┬─────────┘ └─────────┬──────────┘ │
│           │                   │                     │            │
│           │    builds catalog │         multi-root  │            │
│           │    from own .md   │         catalog     │            │
│           └───────────────────┴─────────────────────┘            │
│                               ▼                                  │
│                  DocumentationReader (UserControl)               │
│                  - DocumentationCatalog                          │
│                  - NavigateTo(docId | relative), fragment        │
│                  - Handles MarkdownView.LinkClicked              │
│                               │                                  │
│                               ▼                                  │
│                  MarkdownView (+ link tokens + LinkClicked)      │
│                  + heading id map for fragment scroll            │
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

---

## Code map

| Item | Location |
|------|----------|
| Link parse + heading ids | `Cornerstone/Parsers/Markdown/MarkdownLink.cs` |
| `TokenTypeLink` | `MarkdownTokenizer` / `MarkdownParser` |
| View + fragment scroll | `Cornerstone.Avalonia/Controls/MarkdownView*` |
| Catalog / reader | `DocumentationCatalog` (`FromDirectory`, `FromAssemblyResources`), `DocumentationDocument`, `DocumentationReader` |
| This host | `Cornerstone.Documentation` (`Program`, `App`, `MainWindow`) |
| Sample tab | `Cornerstone.Sample/Tabs/TabDocumentation` (directory + embedded resources) |