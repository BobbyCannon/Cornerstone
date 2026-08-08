<img align="left" src="https://github.com/BobbyCannon/Cornerstone/blob/master/Cornerstone.png?raw=true"
    height="64" width="64" style="margin-bottom: 8px; margin-right: 8px;" />

**Cornerstone** is a high-performance .NET framework that accelerates  
development of reliable, observable, syncable, and testable applications – without sacrificing speed  
or introducing reflection-based surprises.

It is the complete evolution and replacement of the **Speedy** framework, rebuilt from the ground  
up for .NET 10+ with a strong emphasis on maximum performance, reliability, and modern development practices.

Yes, we may not hit all these goals perfectly but they are still the goal. If you run into any issue  
we would love to hear about them.

![GitHub](https://img.shields.io/github/license/BobbyCannon/Cornerstone?style=flat-square&color=purple)
![.NET](https://img.shields.io/badge/.NET-10+-blueviolet?style=flat-square&color=purple)

---

## Table of Contents

- [Key Features](#key-features)
- [Performance & Reliability](#performance--reliability--by-design)
- [Architecture Overview](#architecture-overview)
- [References](#references)
---

## ✨ Key Features

- **Performance-driven design** – engineered for exceptional speed, minimal allocations, fast startup, and small binaries
- **Source-generation** (via Cornerstone Generators):
  - Binary serialization (`Packable`) – fast, compact, and reflection-free alternative to traditional serializers
  - Automatic property change notifications (`INotifyPropertyChanging` / `INotifyPropertyChanged`)
  - Source-generated reflection helpers (safe, fast metadata access)
  - `RelayCommand` / modern commanding pattern
  - `Updateable` tracking for change detection & syncing
  - Automatic `IComparable` implementation
- **Ultra-fast unit & integration testing** – optimized for high-speed execution and true end-to-end validation
- **Powerful deep comparison & diffing** utilities (excellent for sync, auditing, and testing)
- **Built-in sync framework** – reliable data synchronization across clients, services, and databases

---

## 💎 Performance & Reliability – by Design

Cornerstone is built with three non-negotiable goals:

- **Maximum performance** – source generators, minimal allocations, and careful design deliver outstanding speed
- **Rock-solid reliability** – zero known bugs is the target
- **Performance-first architecture** – every component is optimized for speed while remaining fully usable across all modern .NET scenarios

### How it’s achieved

- Every feature is developed with performance and reliability in mind from day one
- **Cornerstone Generators** power fast, reflection-free implementations
- Tests (unit, integration, performance, and automation) are tuned for maximum execution speed
- Continuous profiling and optimization to eliminate unnecessary allocations and hot paths
- Aim: **100% code coverage + high context coverage** (edge cases, threading, boundary conditions, etc.)

---

## Architecture Overview

Cornerstone layers a host process, runtime infrastructure, domain logic, and optional presentation:

```
Host entry (console / Avalonia / mobile / browser / service)
  └─ AppBootstrap.Initialize(…)     process DI, runtime info, platform
       └─ App services (Keystone, view models, features)
            ├─ Keystone  — Bus : State : Engine
            ├─ Lifecycle — Initialize → Load → Start → Process → Stop → …
            └─ Presentation (optional)
                 ├─ ViewModels + ViewIntegration
                 └─ AppDispatcher (optional hard tick loop)
```

### Keystone (domain core)

At the heart of application logic is **Keystone** – a lightweight pattern that cleanly separates:

- **State** (the model / single source of truth)
- **Engine** (processors that mutate state)
- **Bus** (channels for communication)

This foundation makes applications highly testable, observable, and easy to reason about, while still delivering the performance Cornerstone is known for.

### AppBootstrap (process entry)

**AppBootstrap** is the host-agnostic process bootstrap: call `Initialize` once from `Main` (or platform entry) before resolving services. It creates the dependency provider, runtime information, application arguments, and platform registration, and exposes helpers for infrastructure lifecycle and crash logging. Design-time and tests can use `EnsureInitialized` / `Reset`.

### Who owns what

| Piece | Role |
|-------|------|
| **AppBootstrap** | Process-wide DI and infrastructure (once per process) |
| **Keystone** | Domain Bus / State / Engine and its lifecycle tree |
| **LifecycleTracker** | Hierarchical Initialize → Load → Start / reverse teardown |
| **CornerstoneApplication** | Avalonia shell: wires bootstrap, dispatcher, and Keystone start/stop |
| **AppDispatcher** | Optional UI projection loop (attached ViewModels only) |

Full documentation index: [Cornerstone.Documentation/Readme.md](Cornerstone.Documentation/Readme.md).

---

## References

- [Keystone](Cornerstone.Documentation/Keystone.md) — Bus : State : Engine pattern
- [AppBootstrap](Cornerstone.Documentation/AppBootstrap.md) — Process bootstrap, DI root, infrastructure lifecycle
- [Lifecycle](Cornerstone.Documentation/Lifecycle.md) — Object lifecycle phases and trackers
- [CornerstoneApplication](Cornerstone.Documentation/CornerstoneApplication.md) — Avalonia application shell
- [View Integration](Cornerstone.Documentation/ViewIntegration.md) — Keystone State → MVVM
- [AppDispatcher](Cornerstone.Documentation/AppDispatcher.md) — Optional hard dispatch loop
- [Build](Cornerstone.Documentation/Build.md) — Build process notes
