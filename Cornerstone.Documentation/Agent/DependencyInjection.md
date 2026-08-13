# Dependency injection (generated)

How hosts register `[DependencyInjected]` types. No runtime type scan.

## Purpose

- Default factories come from `{Assembly}.CornerstoneGenerated.RegisterDependencies(DependencyProvider)` (concrete provider, not `IDependencyProvider`).
- Hosts **choose which assemblies** to call. There is **no filter** on the generated method.

Out of scope: ASP.NET `IServiceCollection` (websites still register that separately).

## First-wins is priority

`DependencyProvider.AddSingleton` / `AddTransient` use `FactoriesGetOrAdd`. The **first** factory for a type key stays. A later `Add*` for the same key is a **no-op**.

Call **highest-priority bindings first**, then broader generated lists.

`SetSingleton` is the explicit override (tests). It is not the host default.

Registration only stores factories. Nothing is constructed until `GetInstance`. Unused extra keys are cheap.

## What first-wins does not fix

If the host never claims a key, a later `RegisterDependencies` still owns it. Example: calling a desktop assembly’s `RegisterDependencies` after website bindings still registers `IWebClient` → `AppWebClient` unless the host already registered `IWebClient`.

Do **not** add a generator filter or runtime predicate. Mixed desktop+shared graphs belong in **separate assemblies**, or the host simply **does not call** that assembly’s generated method.

## Host checklist

1. `AppBootstrap.Initialize` (core instances: runtime, time, provider).
2. Host-specific instances / design stubs (`AddDesignStubs`, `IWebClient`, DB, HTTP context).
3. Generated methods, nearer assembly first if keys can overlap:
   - `Cornerstone.CornerstoneGenerated`
   - `Cornerstone.Avalonia.CornerstoneGenerated` (UI hosts only)
   - product / plugin `CornerstoneGenerated`
4. Finish all `Add*` before first feature resolve.

Websites: Cornerstone + the website assembly only. Do **not** call desktop or Avalonia generated methods.

## Adding a service

1. `[SourceReflection]` + `[DependencyInjected]` (and `typeof(IService)` / `TypeLifetime.Transient` as needed).
2. `[DependencyInjectionConstructor]` on the DI ctor.
3. Host already calls that assembly’s `RegisterDependencies`.

Do not hand-`AddTransient<T>()` for types the generator already emits.

## Pitfalls

- Two assemblies registering the same interface: order is the contract; second `Add*` is ignored.
- Calling a desktop assembly’s `RegisterDependencies` from ASP.NET leaves unused desktop keys (`AppKeystone`, `IWebClient`) that blow up on first resolve.
- Instance `AddSingleton(value)` after a generated factory for the same type is a no-op — register the instance **first**.

## File map

| Area | Path |
|------|------|
| Attribute | `Cornerstone/Runtime/DependencyInjectedAttribute.cs` |
| Provider | `Cornerstone/Runtime/DependencyProvider.cs` |
| Generator | `Cornerstone.Generators/Processors/DependencyInjectedProcessor.cs` |
| Agent host | `Cornerstone.Agent/App.axaml.cs` |
| Sample host | `Cornerstone.Sample/App.axaml.cs` |
| GrokMonitor host | `Cornerstone.GrokMonitor/App.axaml.cs` |
| Template host | `Cornerstone.Templates/content/avalonia-keystone/Company.AppName/App.axaml.cs` |
