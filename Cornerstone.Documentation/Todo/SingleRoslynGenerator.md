# TODO: One Roslyn generator

**Status:** open  
**Why:** `Cornerstone.Presentation.DevGenerators` exists only because three of its generators fire on names and filenames instead of an opt-in. That split is extra surface area, not a compiler limit. One well-gated analyzer is easier to ship, review, and keep from breaking unrelated compiles.

## Problem

Two Roslyn analyzer projects emit C# today:

| Project | Who loads it | Packed |
|---------|----------------|--------|
| `Cornerstone.Generators` | Almost every project, via `Cornerstone/Directory.Build.props`, unless the project name is in `SkipCornerstoneGenerators` | Yes. `analyzers/dotnet/cs` in the `Cornerstone` and `Cornerstone.Generators` packages |
| `Cornerstone.Presentation.DevGenerators` | `Cornerstone.Presentation`, `Cornerstone.Presentation.BuildTasks`, and `Cornerstone.Presentation.UnitTests` | No |

`Cornerstone.Presentation.BuildTasks` is not a third generator. It is an MSBuild task assembly. It stays separate because MSBuild cannot load the multi-targeted Presentation runtime. This todo does not fold BuildTasks into the Roslyn analyzer.

Nothing in Roslyn, and nothing in the project graph, stops the DevGenerators classes from living in `Cornerstone.Generators`. Presentation already loads `Cornerstone.Generators`. BuildTasks does not: it is on `SkipCornerstoneGenerators`, and it references DevGenerators only so one shared source file still generates.

`BuildTasks` compile-includes `Cornerstone.Presentation/Media/KnownColors.cs` and `Cornerstone.Presentation/SourceGenerator/*.cs`. `KnownColors` declares a partial method with no body:

```csharp
[GenerateEnumValueDictionary]
private static partial Dictionary<string, KnownColor> GetKnownColors();
```

`EnumMemberDictionaryGenerator` fills that body in both compilations. That generator is already attribute-gated. It is not why the second project exists.

## Ungated generators

These three run without an attribute. They are safe only while they are loaded by Presentation's own compile. Putting them in `Cornerstone.Generators` as they stand would run them on every consumer compile.

### Class name: `X11AtomsGenerator`

`X11AtomsGenerator.cs` selects every class whose identifier is `X11Atoms`. It does not check namespace or an attribute. For each public `IntPtr` field it emits `PopulateAtoms`, which calls `Cornerstone.Presentation.X11.XLib` and `XInternAtoms`.

The real type is `Cornerstone.Presentation/Platforms/X11/X11Atoms.cs`. A consumer class with the same name, and public `IntPtr` fields, would get that method and then fail to compile, or worse, compile against X11 helpers the app never meant to call.

### Filename: `CompositionRoslynGenerator`

`CompositionGenerator/CompositionRoslynGenerator.cs` takes every additional file whose path ends with `composition-schema.xml`, deserializes it as `GConfig`, and emits compositor types (brushes, lists, keyframe animations, property proxies).

Presentation opts in by listing that file:

```xml
<AdditionalFiles Include="composition-schema.xml" />
```

in `Cornerstone.Presentation.csproj`. The generator does not require that item, that project, or a compiler property. Any other additional file with the same suffix is treated as the compositor schema. A different XML shape fails the generator. A matching shape emits compositor source into that compilation.

### Every compile: `CompilerDynamicDependenciesGenerator`

`CompilerDynamicDependenciesGenerator.cs` registers a syntax walk for every invocation whose member name is `GetType` or `FindType` and whose single argument is a string literal. It does not resolve which method was called.

The output step always appends `System.Action`, `Action` through 16 type arguments, and `Func` through 17 type arguments before it checks whether any call was found. The "nothing to emit" branch is therefore unreachable. Every compilation that loads this generator gets `CompilerDynamicDependenciesAttribute.generated.cs`.

The generated type is wrapped in `#if XAML_RUNTIME_LOADER` and is a partial of `Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerDynamicDependenciesAttribute`. Each collected string becomes `[DynamicDependency(..., typeof(...))]`. A consumer who defines `XAML_RUNTIME_LOADER`, or who happens to call `.GetType("Some.Type")` on an unrelated API, grows trimmer roots for those strings. Everyone else still pays for the walk and the extra generated file.

## Already gated

These look up an attribute by metadata name. A compilation that does not use the attribute is unchanged. They can move once the three above are gated the same way.

| Generator | Attribute | What it emits |
|-----------|-----------|----------------|
| `EnumMemberDictionaryGenerator` | `GenerateEnumValueDictionary`, `GenerateEnumValueList` | Enum name/value dictionary or list on a partial method. This is the one BuildTasks needs. |
| `GetProcAddressInitializationGenerator` | `GetProcAddress` on a partial method | Native proc-address initialization. Vulkan interop in Presentation. |
| `SubtypesFactoryGenerator` | `SubtypesFactory` on a method | Factory body that scans subtypes in a namespace. Easings use it. |
| `CrossThreadProxyGenerator` | `GenerateCrossThreadProxy` on an interface | Dispatcher proxy. Void calls are posted. Returning calls become `Task` or `Task<T>`. Mismatched marshaller types report `AVPROXY001`. |
| `DefinitelyNotARecordGenerator` | `DefinitelyNotARecord` on a partial class or struct with a primary constructor | Getter-only properties for those parameters. No equality, `ToString`, or deconstruction. |

Attribute types that Presentation and BuildTasks compile live in `Cornerstone.Presentation/SourceGenerator/`. DevGenerators carries a second copy in `SourceGeneratorAttributes.cs` so the generator assembly can spell the same full names. One analyzer project should own the name constants the way `Cornerstone.Generators/Generator.cs` already does. The attributes themselves stay in the compilation that applies them, because analyzer references set `ReferenceOutputAssembly` to false.

## Goal

One Roslyn analyzer project: `Cornerstone.Generators`. Delete `Cornerstone.Presentation.DevGenerators` after its generators have moved and their triggers are opt-in.

Consumer compilations must not gain source, diagnostics, or trimmer roots unless they apply the matching attribute or set the composition opt-in. Presentation and BuildTasks keep the code they generate today.

## Work

1. **Gate the three unsafe generators before moving them.**
   - `X11AtomsGenerator`: require an attribute on the partial class. Do not select by identifier `X11Atoms`.
   - `CompilerDynamicDependenciesGenerator`: require an attribute (or a compiler property set only by Presentation) before walking calls or calling `AddSource`. Do not append `Action` and `Func` in a way that forces a generated file when nothing opted in. Keep the string-literal `GetType` / `FindType` behavior limited to the opted-in compilation.
   - `CompositionRoslynGenerator`: require a compiler-visible property, or an additional-file identity that only Presentation's build sets. A path suffix of `composition-schema.xml` is not an opt-in.
2. **Move the gated generators into `Cornerstone.Generators`.** Keep their current output. Drop the duplicate attribute classes in `SourceGeneratorAttributes.cs` once nothing references that assembly.
3. **Point BuildTasks at `Cornerstone.Generators` for the enum generator only as an analyzer reference** (`ReferenceOutputAssembly` false, `netstandard2.0`), and remove it from the "must not see any Cornerstone analyzer" assumption. `KnownColors.GetKnownColors` still has to be generated when that file is compiled into the task assembly. The other generators no-op there once they are gated. BuildTasks remains on `SkipCornerstoneGenerators` for the Directory.Build.props blanket reference; the csproj reference is explicit so the skip list stays the default for tooling projects.
4. **Remove DevGenerators** from `Cornerstone.Presentation.csproj`, `Cornerstone.Presentation.BuildTasks.csproj`, `Cornerstone.Presentation.UnitTests.csproj`, `SkipCornerstoneGenerators`, `_CornerstonePresentationIsTooling`, both solution files, and the Visual Studio solution files.
5. **Correct the standing note** in [../Presentation/Projects.md](../Presentation/Projects.md). It currently says not to fold DevGenerators into `Cornerstone.Generators`. That was written as if the compiler surface forbade the merge. After this work, the private project is gone.

## Acceptance

- `Cornerstone.Presentation.DevGenerators` is not a project, and nothing references it.
- Presentation still generates the compositor from its schema, X11 atom setup, Vulkan proc init, subtype factories, cross-thread proxies, `DefinitelyNotARecord` properties, dynamic-dependency roots, and the known-color dictionary.
- BuildTasks still generates `KnownColors.GetKnownColors` and the `.cxaml` task still builds.
- A consumer project that references `Cornerstone.Generators` and contains a class named `X11Atoms`, an additional file named `composition-schema.xml`, or a `GetType("...")` call does not gain generated source from those facts alone.
- No new packed analyzer assembly. The moved generators ship inside `Cornerstone.Generators.dll`, which already ships.

## Out of scope

- Merging `Cornerstone.Presentation.BuildTasks` into the analyzer. Different host (MSBuild, not Roslyn).
- Rewriting the compositor, Vulkan, or X11 generators beyond the opt-in check and the move.
- Renaming `Cornerstone.Presentation.Generators.props`. That file is the `x:Name` / property-generator settings imported by the Presentation build. The old `Cornerstone.Presentation.Generators` project is already gone; its code lives under `Cornerstone.Generators/Presentation/`.
