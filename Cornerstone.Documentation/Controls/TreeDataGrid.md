# TreeDataGrid

Cornerstone hosts a forked Avalonia **TreeDataGrid** under `Cornerstone.Avalonia`. It shows flat or hierarchical tabular data with virtualized rows.

Product example: editor source-control history and working-change lists bind row height so the commit graph and grid stay aligned.

---

## At a glance

| Piece | Role |
|-------|------|
| **TreeDataGrid** | Host control: source, headers, scroll, drag/drop, **MinRowHeight** |
| **Flat / hierarchical sources** | Row and column model for the grid |
| **Rows presenter** | Virtualizes rows; realizes and unrealizes elements in a pool |
| **Row** | One realized row; hosts the cells presenter |
| **Theme** | Control themes for grid, row, cells, headers |

---

## Row height: use MinRowHeight

### Settable API

```xml
<TreeDataGrid ItemsSource="{Binding HistorySource}"
        MinRowHeight="{Binding GraphModel.MinRowHeight}"
        ShowColumnHeaders="False" />
```

Or a constant:

```xml
<TreeDataGrid MinRowHeight="40" ... />
```

| Property | Meaning |
|----------|---------|
| **MinRowHeight** on TreeDataGrid | Row height floor; when fixed mode is on, every row is exactly this tall (default 28) |
| **UseFixedRowHeight** | Default **true**: exact scroll extent (`count × MinRowHeight`). **False**: content-sized rows; scroll uses averages (can thrash) |
| **MinHeight** on the grid control | Minimum height of the **control as a whole** — not row height |

Do **not** treat the grid’s `MinHeight` as “row height.” They are different properties.

### How the value reaches rows

Rows are virtualized. The owner grid applies height in code:

1. **Default** — row theme sets a sane `MinHeight` before realize.  
2. **On realize** — if `UseFixedRowHeight`, sets `Height` / `MinHeight` / `MaxHeight` to `MinRowHeight`; otherwise only `MinHeight` and clears forced Height.  
3. **On change** — when either property changes, realized rows are updated and measure is invalidated.

### Scroll extent

| Mode | Extent | When to use |
|------|--------|-------------|
| Fixed (default) | `rowCount × MinRowHeight` | Product lists, SC, anything that should scroll smoothly |
| Variable | Estimated from realized row averages | Demos / rare multi-line content; expect thumb thrash |

Sample tab **Tree Data Grid** can toggle both modes for comparison.

---

## Why not bind row MinHeight to the parent grid?

Ancestor binding works for ordinary controls that stay in the visual tree. Virtualized rows are different: a row may evaluate bindings while it is still off-tree or in a recycle pool, before it is under a TreeDataGrid. That produces “ancestor not found” binding errors and unreliable height.

What went wrong historically:

| Mistake | Why it failed |
|---------|----------------|
| Theme binding row MinHeight to the parent grid | Rows evaluate before they sit under a TreeDataGrid (and again on recycle) |
| Binding to the grid’s MinHeight | That is the **control** min size, not **row** min size |
| Hard-coding theme height only | Stops the errors but makes MinRowHeight unsettable |

### Patterns that stay virtualization-safe

| Pattern | When to use |
|---------|-------------|
| **MinRowHeight** + apply on realize / property change | Preferred API for row min height |
| **TemplatedParent** inside a control template | Child is always owned by the templated control |
| **Style the row type** from app XAML | When you do not need a grid-level property |
| **Set after attach (code)** | Only resolve parent once the row is in the tree |

Avoid putting ancestor bindings for height on the row theme.

---

## Virtualization mental model

```
TreeDataGrid
  └─ rows presenter
       ├─ row (realized, in viewport)
       ├─ row (realized)
       └─ … recycle pool of unrealized rows
```

- Only viewport (plus a small buffer) rows are realized.  
- Recycled rows keep living as controls; they are not permanent children of a stable ancestor graph for binding purposes.  
- Parent-driven values that must match the **current** grid should be pushed from the presenter or grid (realize + property change), not pulled via ancestor binding in the row theme.

---

## Related layout notes

- **Cells** also have a theme min height aligned with the default row height. Keep cell content within `MinRowHeight` (or clip); the row is forced to that height for layout. Commit graph dots should use the same `MinRowHeight` so lanes stay aligned with rows.  
- Column headers use their own min height; headers are not row-virtualized the same way.  
- After a full list **Reset**, the body ScrollViewer offset is clamped into the new extent so rebuilds do not snap the view to the top.  
- **Expand / collapse all** should use `HierarchicalTreeDataGridSource.ExpandAll` / `CollapseAll` (or `ExpandCollapseRecursive`). Those rebuild the flattened row list once. Toggling only model `IsExpanded` flags does not expand unrealized rows and can leave the scrollbar extent wrong.  
- After hierarchical inserts (expand), fixed-height mode fills realized-slot placeholders and realigns `StartU = firstIndex × height` so anchors stay coherent.

---

## Related

- [Readme.md](Readme.md) — index of control docs