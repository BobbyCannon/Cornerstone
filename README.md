# Cornerstone

<img align="left" src="https://github.com/BobbyCannon/Cornerstone/blob/master/Cornerstone.png?raw=true"
	height="64" width="64" style="margin-bottom: 8px; margin-right: 8px;" />

**Cornerstone** is a high-performance, fully **Native AOT-ready** .NET framework that accelerates
development of reliable, observable, syncable, and testable applications — without sacrificing speed
or introducing reflection-based surprises.

It is the complete evolution and replacement of the **Speedy** framework, rebuilt from the ground
up for .NET 10+ priorities: maximum performance, zero-compromise AOT compatibility, and
uncompromising reliability.

![GitHub](https://img.shields.io/github/license/BobbyCannon/Cornerstone?style=flat-square&color=purple)
![.NET](https://img.shields.io/badge/.NET-10+-blueviolet?style=flat-square&color=purple)

## ✨ Key Features

- **Full Native AOT compatibility** — zero runtime reflection, trimmable, fast startup, tiny binaries
- **Source-generation** (via Cornerstone Generators):
  - Binary serialization (`Packable`) — fast, compact, AOT-safe alternative to BinaryFormatter/MessagePack in many cases
  - Automatic property change notifications (`INotifyPropertyChanging` / `INotifyPropertyChanged`)
  - Source-generated reflection helpers (safe, fast metadata access)
  - `RelayCommand` / modern commanding pattern
  - `Updateable` tracking for change detection & syncing
  - `IComparable` automatic implementation for comparable
- **Ultra-fast unit & integration testing in real AOT** — tests compiled to native executable for true end-to-end validation
- **Powerful deep comparison & diffing** utilities (great for sync, auditing, testing)
- **Built-in sync framework** — reliable data synchronization across clients, services, databases

## 🚀 AOT, Speed & Reliability – by Design

Cornerstone is built with three non-negotiable goals:

- **100% Native AOT safe** — no runtime reflection, no dynamic code, no surprises
- **Maximum performance** — source generators + careful design = minimal overhead
- **Rock-solid reliability** — zero known bugs is the target

### How is this achieved

- Every feature is developed with AOT in mind from day one
- **Cornerstone Generators** compiles **all** unit, integration, performance, and automation tests into a native AOT executable
- Tests run constantly in real AOT context during development (not just JIT)
- Aim: **100% code coverage + high context coverage** (edge cases, boundary conditions, threading scenarios, etc.)
- Continuous profiling to eliminate allocations and hot paths
