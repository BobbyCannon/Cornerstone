# Changelog

## 1.5.297.20657 - 2026-10-25

### Change

- Delete at a line break joins the next line when that line is indented. The break and the indent become one space. A line with no indent, a selection, and an open completion list stay with the editor.
- Go To Definition on a binding path walks each property from the nearest `x:DataType`. A property declared only on a base interface opens that interface member. `$parent`, `#name`, indexers, and casts are not followed.
- Preview document paths are project-relative (`/SourceControl/Views/RepositoryInfoView.cxaml`), matching the csres name from the MSBuild item.
- The first preview frame after attach, resume, or a new listen is posted at Input so a restored document paints. Later frames are posted at Background. There is no composition render hook.
- The hosted editor stores its new rectangle and applies it after layout returns. Resizing during WPF layout could leave Visual Studio ignoring input.
