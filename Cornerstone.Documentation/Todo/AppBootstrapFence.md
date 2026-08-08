# TODO: Fence AppBootstrap static access

**Status:** open  
**Why:** Static process-root APIs are convenient, so app authors and tooling keep using them as a service locator. That fights constructor injection and makes dependencies invisible.

## Problem

`AppBootstrap` exposes process-wide statics (`GetInstance<T>`, `DependencyProvider`, `DateTimeProvider`, `RuntimeInformation`, …). They exist for **host bootstrap** (entry point, platform shell, design-time/test setup).

In practice, new code “just grabs” them because they are available. Example class of mistake: resolving `IDispatcher` from `AppBootstrap.DependencyProvider` inside `PopupViewModel` instead of injecting `IDispatcher`.

## Goal

Make the wrong thing hard (or impossible) for feature / library code, while host bootstrap can still initialize the process.

## Fence ideas (pick when implementing)

1. **API surface**
   - Split bootstrap into a host-only type (e.g. internal or `Cornerstone.Hosting`) vs a narrow public bootstrap façade.
   - Or mark consumer-facing resolve APIs `[Obsolete]` / analyzer-forbidden outside allowed assemblies.
2. **Analyzers / source generators**
   - Roslyn analyzer: ban `AppBootstrap.GetInstance`, `AppBootstrap.DependencyProvider`, etc. outside allow-listed namespaces (host, tests, Avalonia application shell).
   - Same rule as “no service locator in feature code.”
3. **Docs & repo rules**
   - Rule already updated: [framework-primitives.md](../../../.grok/rules/framework-primitives.md) (Dependency injection / AppBootstrap statics).
   - Tighten [AppBootstrap.md](../AppBootstrap.md) so `GetInstance` is documented as host/design-time only, not a general pattern.
4. **Migration**
   - Inventory existing `AppBootstrap.GetInstance` / static usages.
   - Convert call sites to constructor or method injection; leave only host/test shell.

## Acceptance

- New feature code cannot resolve services via `AppBootstrap` without an explicit allow-list exception.
- New work following repo rules never introduces static `AppBootstrap` lookups for runtime dependencies.
- Tests supply `IDispatcher`, time, runtime info, etc. via injection, not bootstrap statics.

## Related

- Repo rule: `.grok/rules/framework-primitives.md` — “AppBootstrap statics — do not use from feature code”
- Host docs: [AppBootstrap.md](../AppBootstrap.md)