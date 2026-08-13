# Agent Documentation

Implementation references for Cornerstone subsystems: architecture maps, extension checklists, file maps, and pitfalls.

Product behavior and host-facing overviews live under the parent folder: [../Readme.md](../Readme.md).

---

## Index

| Document | Use when… | Primary code |
|----------|-----------|--------------|
| [Sync.md](Sync.md) | Building or updating entity sync: managers, sessions, converters, filters, entities/models | `Cornerstone/Sync/`, EF sync adapters |
| [TextEditor.md](TextEditor.md) | Building or updating the Avalonia text editor / terminal: document model, input, tokens, layout | `Cornerstone.Avalonia/Text/` |
| [MarkdownView.md](MarkdownView.md) | Building or updating Markdown parse/view: fences, streaming, block groups, tables | `Cornerstone/Parsers/Markdown/`, `Cornerstone.Avalonia/Controls/Markdown*` |
| [TreeDataGrid.md](TreeDataGrid.md) | Building or updating TreeDataGrid: MinRowHeight path, virtualization, row themes | `Cornerstone.Avalonia/TreeDataGrid/` |
| [Themes.md](Themes.md) | Theme color / mode / density: apply path, tokens, host checklist, Static vs Dynamic pitfalls | `Cornerstone.Avalonia/CornerstoneTheme*`, `Themes/*` |
| [DependencyInjection.md](DependencyInjection.md) | `[DependencyInjected]`, `RegisterDependencies`, first-wins `Add*`, which assemblies a host should call | `Cornerstone/Runtime/DependencyProvider.cs`, `Cornerstone.Generators` |

---

## Conventions

Prefer including:

1. **Purpose** and what is explicitly out of scope
2. **Architecture at a glance** (diagram + role table)
3. **Key types and data shapes**
4. **How to add / update** functionality (checklists)
5. **Common pitfalls**
6. **File map** into the repo

When adding a new topic, link it in this table and keep one topic per file.
Link product intent from [../Readme.md](../Readme.md) rather than duplicating long narrative here.