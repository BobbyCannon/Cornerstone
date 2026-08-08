# TreeDataGrid (Agent)

Dense reference for implementing or changing the forked Avalonia TreeDataGrid.

**Product behavior:** [../Controls/TreeDataGrid.md](../Controls/TreeDataGrid.md)

---

## Purpose / out of scope

**In scope:** MinRowHeight path, row realize/unrealize, theme defaults, virtualization-safe height, product binding patterns.

**Out of scope here:** full TreeDataGrid feature inventory, selection models, drag/drop design.

---

## Architecture

```
TreeDataGrid (MinRowHeight, source, scroll)
  └─ TreeDataGridRowsPresenter
       RealizeElement → row.MinHeight = grid.MinRowHeight
       UnrealizeElement → recycle pool
  └─ TreeDataGridRow → cells presenter
```

| Type | Role |
|------|------|
| `TreeDataGrid` | Host; owns `MinRowHeight`; propagates on property change |
| `TreeDataGridRowsPresenter` | Virtualization pool; applies height on realize |
| `TreeDataGridRow` | Realized row shell |
| Theme `TreeDataGrid.axaml` | Default row/cell/header MinHeight; **no** ancestor height bindings |

---

## MinRowHeight contract

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `TreeDataGrid.MinRowHeight` | `int` | `28` | Row minimum height |
| `Layoutable.MinHeight` on grid | `double` | Avalonia default | Control size, not row height |

Propagation:

1. Theme default on row (`MinHeight="28"`).
2. `RealizeElement` copies `grid.MinRowHeight` → row `MinHeight`.
3. `OnPropertyChanged(MinRowHeight)` updates realized rows.

### Do not

```xml
<!-- Fragile on virtualized rows -->
<Setter Property="MinHeight"
        Value="{Binding MinRowHeight, RelativeSource={RelativeSource AncestorType=TreeDataGrid}}" />
```

Symptom: `[Binding] ... Ancestor not found` on `TreeDataGridRow`.

---

## Checklist: change row height behavior

1. Prefer `MinRowHeight` API over theme-only constants if hosts must set height.  
2. Apply on realize **and** property change.  
3. Keep cell theme MinHeight aligned when product needs pixel-perfect graph/row match.  
4. Do not use `$visualParent[TreeDataGrid]` / AncestorType height bindings on row themes.  
5. Product sample: Editor `RepositoryInfoView.axaml` — `MinRowHeight="{Binding GraphModel.MinRowHeight}"`.

---

## File map

| Path | Notes |
|------|-------|
| `Cornerstone.Avalonia/TreeDataGrid/TreeDataGrid.axaml` | Control themes; row default MinHeight only |
| `Cornerstone.Avalonia/TreeDataGrid/TreeDataGrid.axaml.cs` | `MinRowHeight`, propagate on change |
| `Cornerstone.Avalonia/TreeDataGrid/Cells/TreeDataGridRowsPresenter.cs` | Apply MinRowHeight in RealizeElement |
| `Cornerstone.Avalonia/TreeDataGrid/Cells/TreeDataGridRow.cs` | Realize / unrealize lifecycle |

---

## Pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Ancestor bind row MinHeight to grid | Binding spam / wrong height | Push height from presenter |
| Use grid MinHeight for rows | Layout of control changes, rows unchanged | Use MinRowHeight |
| Theme hard-code only | Product cannot bind graph height | MinRowHeight + realize/change |
| Lower MinRowHeight below cell theme min | Content does not shrink | Align cell and row defaults |