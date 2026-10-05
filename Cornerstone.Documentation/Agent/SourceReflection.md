# Source reflection (generated)

Compile-time maps so attributed types work under Native AOT. That is the whole point. Generated construction and member access must not use runtime reflection.

Related: [DependencyInjection.md](DependencyInjection.md) (DI uses `SourceConstructorInfo.Invoke`), [Storage.md](Storage.md) (SQL `GetValue` / `SetValue`).

## Purpose

- Replace `Type.GetConstructor` / `ConstructorInfo.Invoke` / `PropertyInfo.GetValue` on attributed types with compiled lambdas.
- Root members the trimmer would otherwise drop.
- Let `DependencyProvider` construct `[DependencyInjectionConstructor]` types, including private ones, without a reflection invoke.

Out of scope: runtime `SourceReflector` fallbacks for types that were never generated. Those maps are not Native AOT.

## Architecture at a glance

| Piece | Role |
|-------|------|
| `[SourceReflection]` | Type in this compilation is generated |
| `[SourceReflectionType<T>]` | Generate a map for a type you do not own (e.g. `Version`) |
| `SourceReflectionProcessor` | Emits `SourceTypeInfo` + compiled `Invoke` / `GetValue` / `SetValue` |
| `SourceConstructorInfo.Invoke` | DI and `CreateInstance` call this; must be a compiled lambda on generated types |
| `UnsafeAccessor(Constructor)` | Private / protected constructors: AOT-legal access from `CornerstoneGenerated` |

## AOT rules

Emit only:

- `Invoke = static (parameters) => new T(...)` when `CornerstoneGenerated` can call the constructor (public, internal, protected internal).
- `UnsafeAccessorKind.Constructor` plus a compiled `Invoke` that calls that accessor when the constructor is private, protected, or private protected.
- Direct property/field get/set lambdas the same way.

Do **not** emit `typeof(T).GetConstructor(...)` or bind `ConstructorInfo.Invoke` as the generated invoke. That is classic reflection and fails Native AOT. Skip the member if it cannot be compiled (`ref` structs, open generics, required members, missing `UnsafeAccessor` on old TFMs).

`ConstructorInfo` / `MethodInfo` / `PropertyInfo` on the generated metadata object are optional trim-rooting handles. They are not the invoke path.

## Adding a type

1. `[SourceReflection]` on the type (already `partial` if other generators touch it).
2. `[DependencyInjectionConstructor]` on the ctor DI should use (may be private).
3. Rebuild so `CornerstoneGenerated` rewrites. Hosts still call `RegisterDependencies` for DI.

## Pitfalls

- DI reads `primaryConstructor.Invoke`. If `_invoke` is missing, the getter would otherwise bind `ConstructorInfo.Invoke` on null or take a non-AOT path.
- `CornerstoneGenerated` is a different type, so it cannot `new` a private constructor. Use `UnsafeAccessor`, not `GetConstructor`.
- Runtime `SourceReflector.GetConstructors` still uses reflection for unmapped types. Do not use that as a model for generated code.

## File map

| Area | Path |
|------|------|
| Attributes | `Cornerstone.Generators/Reflection/SourceReflectionAttribute.cs`, `SourceReflectionTypeAttribute.cs` |
| Maps | `Cornerstone.Generators/Reflection/SourceConstructorInfo.cs`, `SourceTypeInfo.cs` |
| Generator | `Cornerstone.Generators/Processors/SourceReflectionProcessor.cs` |
| Runtime registry | `Cornerstone/Reflection/SourceReflector.cs` |
| DI consume | `Cornerstone/Runtime/DependencyProvider.cs` (`CreateInstanceForDependencyInjection`) |
