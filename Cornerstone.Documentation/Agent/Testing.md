# Testing (Agent)

MSTest + generated `Program.Main` for executable test assemblies. Host-facing: [../Testing.md](../Testing.md).

All test projects including `Cornerstone.Presentation.UnitTests` use **MSTest**. Do not add xUnit or NUnit packages.

## When this applies

Adding a test project, CS5001 (no `Main`), CSG003 (`[TestClass]` missing), or skipping Cornerstone.Generators on a test assembly.

## How `Main` is produced

| Step | Where |
|------|--------|
| Analyzer reference | `Cornerstone/Directory.Build.props` → `Cornerstone.Generators.csproj` unless `SkipCornerstoneGenerators` contains `;$(MSBuildProjectName);` |
| Emit | `Generator.GenerateUnitTestMain` (`Generator.UnitTests.cs`) after other generators |
| Runner body | Embedded `TestRunner.cs` copied into the generated source |
| Entry | `class Program { public static int Main(string[] args) }` registers every `[TestMethod]` type |

**Gates (all required):**

- Compilation `OutputKind` is Console / Windows / WindowsRuntime **application** (Library compilations skip emit).
- Referenced assembly named `Microsoft.NET.Test.Sdk` or `MSTest.TestFramework`.
- At least one type with `[TestMethod]`.

Then each such type **must** have `[TestClass]` or **CSG003**. Nested types count.

## csproj contract

Match `Cornerstone.UnitTests` (MSTest).

- `GenerateProgramFile` = false (SDK must not emit a second `Main`).
- `OutputType` Exe (Release / inner TFM). Debug may set Library for VS; inner `-p:TargetFramework=` builds are often still Exe.
- Sign the assembly; add `InternalsVisibleTo` on the product if tests need internals.

## Pitfalls

| Symptom | Cause |
|---------|--------|
| CS5001 no `Main` | Project is Exe, `GenerateProgramFile` false, and generators skipped or gates failed |
| CSG003 | `[TestMethod]` without `[TestClass]` (nested classes too) |
| Duplicate `Main` | `GenerateProgramFile` left true **and** generators emit |
| Generator never runs | Name listed in `SkipCornerstoneGenerators` (do not skip MSTest Exe projects) |
| Second test runner | Mixing another test framework package with MSTest in the same test project |

`SkipCornerstoneGenerators` is for tooling (Generators, BuildTasks, DevGenerators, Remote.Protocol, Designer*). Not for MSTest `*.UnitTests`. `Cornerstone.Generators.UnitTests` and `Cornerstone.Presentation.UnitTests` are MSTest. Keep a stub `Program.Main` if the unit-test generator does not emit one in Debug.

## File map

| Path | Role |
|------|------|
| `Cornerstone/Directory.Build.props` | Analyzer ProjectReference + skip list |
| `Cornerstone.Generators/Generator.cs` | Calls `GenerateUnitTestMain` |
| `Cornerstone.Generators/Generator.UnitTests.cs` | Discovery, CSG003, `Program.Main` emit |
| `Cornerstone.Generators/TestRunner.cs` | Embedded runner (`SkipInAot`, `-f` filter, `-v` verbose, `-q` / `--failed-only`) |
| `Cornerstone.Generators/DiagnosticReporter.cs` | CSG003 |
| `Tests/Cornerstone.UnitTests/` | Product / framework tests |
| `Tests/Cornerstone.Presentation.UnitTests/` | Presentation fork regression (MSTest + Headless) |
