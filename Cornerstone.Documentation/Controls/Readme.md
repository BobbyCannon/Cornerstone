# Controls

Cornerstone.Presentation controls owned or heavily customized under `Cornerstone.Presentation.Theme`.

These pages explain control behavior, settable APIs, and layout rules that matter when building product UI.

---

## Index

| Document | Summary |
|----------|---------|
| [DockingLifecycle.md](DockingLifecycle.md) | DockingManager owns tab Init/Load/Start and AppDispatcher Track/Release |
| [DocumentationReader.md](DocumentationReader.md) | In-app docs reader: catalog, link/header navigation, hosts |
| [MarkdownView.md](MarkdownView.md) | Markdown parser, streaming fences, MarkdownView document model |
| [TreeDataGrid.md](TreeDataGrid.md) | Flat or hierarchical grid, MinRowHeight, virtualization, row height rules |

---

## When adding a control page

1. Prefer one control (or tightly related family) per file under `Controls/`.
2. Link it from this table and from the parent [Readme.md](../Readme.md) Controls section.
3. Call out settable APIs vs theme defaults, and any virtualization constraints.
4. Point at real product call sites when they illustrate the intended pattern.
5. Keep implementation file maps and checklists out of these pages.