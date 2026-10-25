# Source reflection (generated)

Compile-time maps so attributed types work under Native AOT and on the browser. Generated construction and member access must not use runtime reflection.

Related: [DependencyInjection.md](DependencyInjection.md) (DI calls `SourceConstructorInfo.Invoke`), [Storage.md](Storage.md) (SQL `GetValue` / `SetValue`).

## Purpose

- Replace `Type.GetConstructor` / `ConstructorInfo.Invoke` / `PropertyInfo.GetValue` on attributed types with compiled lambdas.
- Root members the trimmer would otherwise drop.
- Let `DependencyProvider` construct `[DependencyInjectionConstructor]` types, including private ones, without a reflection invoke.

`[DependencyInjectionConstructor]` by itself does not generate a map. On desktop, `Type.GetConstructors` still sees a public constructor, so a missing `[SourceReflection]` can look fine. On the browser it does not.

## Discovery

The generator runs once per compilation, so once per TFM. `Generator.GetSourceTypeInfoForSourceReflection` collects:

- Types in this compilation marked `[SourceReflection]`.
- `[assembly: SourceReflectionType<T>]` for a type this assembly does not declare. Those infos get `IsSourceReflectionType`.

`SourceReflectionProcessor.ShouldProcess` is true only for those two cases. Other processors (`[DependencyInjected]`, `[Notify]`, packable, and the rest) build their own `SourceTypeInfo` lists. They do not emit the constructor map.

A type that exists only under `Platforms/Browser` is visible only to the `net10.0-browser` compilation. The net10.0 map never mentions it. That is the correct split.

## What is emitted

For each processed type, one file `Cornerstone{Namespace}{Type}.g.cs` under `obj/<Configuration>/<TFM>/generated/Cornerstone.Generators/`. It is not checked in. The map is a `SourceTypeInfo` whose `DeclaredConstructorsInitializer` builds `SourceConstructorInfo` rows.

The constructor marked `[DependencyInjectionConstructor]` is stored with `IsDependencyConstructor = true`. Its `Invoke` is a compiled lambda:

- `static (parameters) => new T(...)` when `CornerstoneGenerated` can call the constructor (public, internal, protected internal).
- `UnsafeAccessorKind.Constructor` plus a lambda that calls that accessor when the constructor is private, protected, or private protected.

`__Cornerstone.Generated.g.cs` has a `[ModuleInitializer]` `CornerstoneGenerated.Initialize` that calls `SourceReflector.Add` for every map. `Add` is first-wins (`Types.TryAdd`). This registration is not `RegisterDependencies`. `RegisterDependencies` only stores DI factories. The type map is already in `SourceReflector.Types` before the host resolves anything.

## How a resolve uses the map

`AddTransient<TService, TImpl>()` with no factory stores a factory that calls `CreateInstanceForDependencyInjection<TImpl>()`. The same method is what `SetTransient<TService, TImpl>()` uses when no factory is passed.

`DependencyProvider.CreateInstanceForDependencyInjectionCore`:

1. `SourceReflector.GetSourceType(type)`.
2. If `Types` has no entry, `CreateSourceTypeInfoUsingReflection` fills one from `Type.GetConstructors` and the other runtime member APIs.
3. Prefer the constructor with `IsDependencyConstructor`.
4. Otherwise a parameterless constructor.
5. Otherwise the single remaining constructor whose parameter types are already registered.
6. `Invoke` the lambda. Resolve each parameter with `GetInstance`. Then property injection.

`GetSourceType` never returns null for a runtime `Type`. A miss becomes the reflection fallback. On browser WASM that fallback's `GetConstructors` returns an empty array: the runtime does not keep constructor metadata that nothing rooted. DI then throws `DependencyInjectorConstructorException`:

`An injectable constructor could not be found for {type}. Source reflection returned 0 constructors.`

Thrown from inside a pointer-up, that exception freezes the page. The click never resumes. See [BrowserDebug.md](BrowserDebug.md).

## Adding a type

1. `[SourceReflection]` on the type. Make it `partial` when another generator also writes into the type.
2. `[DependencyInjectionConstructor]` on the constructor DI should use. It may be private.
3. Rebuild the TFM that compiles the type so `CornerstoneGenerated` rewrites.
4. If the type is also `[DependencyInjected]`, the host must call that assembly's `RegisterDependencies`. A hand `AddTransient<TService, TImpl>()` still needs the source-reflection map. The factory does not skip `CreateInstanceForDependencyInjection`.

Passing an explicit factory (`() => new TImpl(...)`) skips the map. Use that only when the caller already has the arguments. It does not root the type for other reflection.

## BrowserGamepad

`BrowserPlatform` registers `AddTransient<Gamepad, BrowserGamepad>()`. `GamepadsManager.CreateGamepad` calls `TryGetInstance<Gamepad>()` because the registration is not a singleton. That resolves `BrowserGamepad(IDateTimeProvider)`.

`Platforms/Browser/BrowserGamepad.cs` is compiled only for `net10.0-browser`. Without `[SourceReflection]`, the browser map had no entry, the reflection fallback returned 0 constructors, and Inputs → Gamepad froze on the click.

With the attribute, `CornerstonePlatformsBrowserBrowserGamepad.g.cs` contains:

- `IsDependencyConstructor = true`
- `Invoke = static (parameters) => new BrowserGamepad((IDateTimeProvider)parameters[0])`
- `SourceReflector.Add` from the module initializer

`IDateTimeProvider` is already registered by `AppBootstrap.Initialize` before the page resolves the manager.

## AOT rules

Emit only the compiled `Invoke` / get / set paths above. Do not emit `typeof(T).GetConstructor(...)` or bind `ConstructorInfo.Invoke` as the generated invoke. Skip a member that cannot be compiled (`ref` structs, open generics, required members, missing `UnsafeAccessor` on old TFMs).

`ConstructorInfo` / `MethodInfo` / `PropertyInfo` on the generated metadata object are optional trim-rooting handles. They are not the invoke path.

## Pitfalls

- A public constructor plus `[DependencyInjectionConstructor]` is not enough on the browser. The type needs `[SourceReflection]` in the compilation that builds it.
- "Source reflection returned 0 constructors" means the map was missing and the runtime member walk was empty. It does not mean the C# constructor was omitted from the source file.
- DI reads `primaryConstructor.Invoke`. If `_invoke` is missing, the getter would otherwise bind `ConstructorInfo.Invoke` on null or take a non-AOT path.
- `CornerstoneGenerated` is a different type, so it cannot `new` a private constructor. Use `UnsafeAccessor`, not `GetConstructor`.
- Runtime `SourceReflector.GetConstructors` still uses reflection for unmapped types. Do not copy that into generated code.
- `SourceReflector.Add` keeps the first map. A second generated entry for the same `Type` is dropped.

## File map

| Area | Path |
|------|------|
| Attributes | `Cornerstone.Generators/Reflection/SourceReflectionAttribute.cs`, `SourceReflectionTypeAttribute.cs` |
| Discovery | `Cornerstone.Generators/Generator.cs` (`GetSourceTypeInfoForSourceReflection`) |
| Emit | `Cornerstone.Generators/Processors/SourceReflectionProcessor.cs` |
| Map types | `Cornerstone.Generators/Reflection/SourceConstructorInfo.cs`, `SourceTypeInfo.cs` |
| Runtime registry | `Cornerstone/Reflection/SourceReflector.cs` |
| DI consume | `Cornerstone/Runtime/DependencyProvider.cs` (`CreateInstanceForDependencyInjection`) |
| Browser example | `Cornerstone/Platforms/Browser/BrowserGamepad.cs` |
