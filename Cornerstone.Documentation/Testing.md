# Testing

Cornerstone product and framework tests use **MSTest**. Visual Studio Test Explorer and `dotnet test` both run them. Release (and some inner TFM) builds are **executables** so a process can run the suite without the VSTest host — that is how AOT and trimmed publishes are exercised.

The Presentation fork suite (`Cornerstone.Presentation.UnitTests`) is also **MSTest** (Headless session via `[HeadlessTestMethod]`).

## Projects

| Project | Role |
|---------|------|
| `Cornerstone.UnitTests` | Framework and product tests (including some Presentation/Theme behavior). MSTest. SQLite memory and generated SQL only |
| `Cornerstone.IntegrationTests` | SQL Server (`localhost` / `Cornerstone.Sample.Tests`) and other external resources. Inconclusive if the server is down |
| `Cornerstone.Presentation.UnitTests` | Regression for the UI runtime. MSTest + Headless |
| `Cornerstone.Generators.UnitTests` | Source generators. MSTest |
| `Cornerstone.AutomationTests` | Automation / headless browser. MSTest |
| `Cornerstone.PerformanceTests` | Benchmarks. MSTest |

New tests belong in the project that matches the code under test. Do not mix Presentation-fork engine tests into `Cornerstone.UnitTests`. Do not put live SQL Server (or other machine resources) in `Cornerstone.UnitTests`.

## Test project shape

Typical csproj (see `Cornerstone.UnitTests`):

- `OutputType` **Exe** by default, **Library** in Debug so the IDE can load a test DLL.
- `GenerateProgramFile` **false** — do not let Microsoft.NET.Test.Sdk emit its own `Main`.
- `IsTestProject` true in Debug.
- MSTest packages (`MSTest.TestFramework`, `MSTest.TestAdapter`, `Microsoft.NET.Test.Sdk`).
- Assembly signed like the rest of Cornerstone.

Inner builds that pass `TargetFramework=` (including `net10.0-windows…`) often still compile as **Exe** even in Debug. That is expected. The process then needs a `Main`.

## Generated `Main`

`Cornerstone.Generators` is referenced as an analyzer from `Directory.Build.props` unless the project name is listed in `SkipCornerstoneGenerators`.

When the compilation is an executable **and** it references MSTest, the generator:

1. Finds types with `[TestMethod]`.
2. Requires `[TestClass]` on those types (**CSG003** if missing, including nested types).
3. Emits `Program.Main` plus an embedded `TestRunner` that constructs those classes, runs initialize / methods / cleanup, and honors `[SkipInAot]`.

That generated entry point is why `GenerateProgramFile` is false. If the generator does not run and `OutputType` is Exe, the compiler reports **CS5001** (no suitable `Main`).

Do **not** add MSTest test projects to `SkipCornerstoneGenerators`. Tooling projects (BuildTasks, Presentation generators, the Generators project itself, Designer*) stay on the skip list.

## Writing tests

- `[TestClass]` on every type that has `[TestMethod]`, including nested classes.
- Method names: PascalCase, no underscores (`ParseBlockQuotes`). Some Presentation runtime tests keep upstream names (underscores) so they stay diffable against the pin.
- `[assembly: DoNotParallelize]` on assemblies that share dispatcher or locator state.
- `[SkipInAot("reason")]` on a class or method that cannot run under AOT.

## Presentation tests

`Cornerstone.Presentation.UnitTests` targets the Presentation runtime and runs on **MSTest 4.4.0**. Headless `PresentationTestApplication` plus `[assembly: DoNotParallelize]`. Use `[PresentationTestMethod]` / `[HeadlessTestMethod]` / `[DataRow]` / `[TestData]`. Some imported test method names keep underscores so they stay diffable against the pin.

`x:Name` / generated-property snapshots live in `Cornerstone.Generators.UnitTests` under `Presentation/NameGenerator/` and `Presentation/PropertyGenerator/`.

Headless product tests that need `Cornerstone.Presentation.Theme` stay in `Cornerstone.UnitTests` (`TestAppBuilder`, `CornerstonePresentationUnitTest`).
