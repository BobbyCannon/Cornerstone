# Agent Documentation

Implementation references for Cornerstone subsystems: architecture maps, extension checklists, file maps, and pitfalls.

Product behavior and host-facing overviews live under the parent folder: [../Readme.md](../Readme.md).

---

## Index

| Document | Use when… | Primary code |
|----------|-----------|--------------|
| [Storage.md](Storage.md) | SQL mapper vs EF: generated CUD, migrations, transactions, Sample SQLite. EF-workflow todo: [Todo/SqlEfWorkflow.md](../Todo/SqlEfWorkflow.md) | `Cornerstone/Storage/Sql/`, `Cornerstone.EntityFramework` |
| [Sync.md](Sync.md) | Building or updating entity sync: `Sync(SyncOperation)`, converters, filters, SQL apply | `Cornerstone/Sync/`, EF + SQL sync adapters |
| [TextEditor.md](TextEditor.md) | Building or updating the Cornerstone text editor / terminal: document model, input, tokens, layout, multi-caret | `Cornerstone.Presentation/Controls/Text/`; themes in `Theme/Controls/TextEditor.cxaml` and `Terminal.cxaml` |
| [TextFormatting.md](TextFormatting.md) | Document pretty-print / minify: token walkers, `FormatSettings`, `FormatDocument` undo | `Cornerstone/Text/Formatting/`, `TextEditorViewModel.FormatDocument` |
| [MarkdownView.md](MarkdownView.md) | Building or updating Markdown parse/view: fences, streaming, block groups, tables | `Cornerstone/Text/Parsing/Markdown/`, `Cornerstone.Presentation/Controls/Markdown*` |
| [TreeDataGrid.md](TreeDataGrid.md) | Building or updating TreeDataGrid: MinRowHeight path, virtualization, row themes | `Cornerstone.Presentation/Controls/TreeDataGrid/`; theme in `Theme/Controls/TreeDataGrid.cxaml` |
| [Themes.md](Themes.md) | Theme color / mode / density, and the expected control theme file (`Styles`, `ControlTheme`, outer selectors) | `Cornerstone.Presentation/Theme/CornerstoneTheme*`, `Theme/Controls/*.cxaml` |
| [Controls.md](Controls.md) | Where a new control type goes: kind folders, root-namespace lock, theme include | `Cornerstone.Presentation/Controls/`, `Theming/Controls.cxaml` |
| [../Presentation/Readme.md](../Presentation/Readme.md) | Presentation runtime (human): XAML compile / BuildTasks, upstream pin, native layering | `Cornerstone/Cornerstone.Presentation/` |
| [../Presentation/Projects.md](../Presentation/Projects.md) | Presentation* projects (human): runtime vs Theme vs tooling vs designer vs sandbox | `Cornerstone.Presentation*`, `Cornerstone.Designer.HostApp` |
| [../NuGet.md](../NuGet.md) | Human package list: seven published ids, embedded protocol and BuildTasks, source-only integrations | `Package.props` |
| [NativeLayering.md](NativeLayering.md) | Native-behind Skia: file map, failed attempts, flag | `NativeControlHost`, `Win32NativeAirspaceOverlay`, platform hosts |
| [DependencyInjection.md](DependencyInjection.md) | `[DependencyInjected]`, `RegisterDependencies`, first-wins `Add*`, which assemblies a host should call | `Cornerstone/Runtime/DependencyProvider.cs`, `Cornerstone.Generators` |
| [BrowserDebug.md](BrowserDebug.md) | Browser lockup: numbered `TabTrace` console procedure, and the Welcome card `BrowserInteropProxy` exception | `Cornerstone.Sample.Browser/Program.cs`, `Platforms/Browser/HostAppBuilderExtensions.cs` |
| [SourceReflection.md](SourceReflection.md) | Generated AOT-safe type maps: compiled `Invoke` / get/set, never `GetConstructor` for construct | `Cornerstone.Generators/Processors/SourceReflectionProcessor.cs`, `Cornerstone/Reflection/` |
| [Testing.md](Testing.md) | MSTest Exe + generated `Program.Main`; SkipCornerstoneGenerators; CS5001 / CSG003 | `Cornerstone.Generators/Generator.UnitTests.cs`, `Tests/` |

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