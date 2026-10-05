# RuntimeInformation

`Cornerstone.Runtime.RuntimeInformation` is the process snapshot of the running application and the device it is on. Hosts create one instance during bootstrap, bind the entry assembly, and drive it through lifecycle. Feature code takes `IRuntimeInformation` by injection.

The interface and the class live in `Cornerstone/Runtime/RuntimeInformation.cs`. `IRuntimeInformation` is also an `IReadOnlyDictionary<string, object>` of values already cached on that instance, and an `ISyncClientDetails` so sync can copy client identity onto a session or request headers. See [Sync.md](Sync.md).

Who constructs it and when it starts: [AppBootstrap.md](AppBootstrap.md). Phase rules: [Lifecycle.md](Lifecycle.md).

---

## What you inject

`AppBootstrap.Initialize` builds the instance, sets the application name as a platform override, binds the entry assembly, and runs `Initialize` (Initialize + Load). `SetupCornerstoneDependencies` then registers that same object as both `RuntimeInformation` and `IRuntimeInformation`.

The class is also marked `[DependencyInjected]` for itself and for `IRuntimeInformation`. The bootstrap registration is the instance that already has the assembly and the name override.

Design-time stubs register `RuntimeInformationData.GetSample()` instead of a live detector. Tests that need a fixed snapshot do the same.

Views bind a `RuntimeInformationData` copy. Fill it with `Copy()` or with AppDispatcher `TrackProperties`. See [AppDispatcher.md](AppDispatcher.md).

```csharp
RuntimeInformationData snapshot = (RuntimeInformationData) runtime.Copy();
```

`Copy()` reads the snapshot properties, including the slow lookups below, and returns a `RuntimeInformationData`. `ApplicationRuntime` stays on the live instance.

---

## Cache

Each public fact is computed on first read and stored on the instance. `ApplicationRuntime` is the exception: it is the elapsed time since this object was constructed, and it is never stored.

Two maps sit in front of the factories:

| Map | Scope | Write | Read |
|-----|--------|-------|------|
| Platform overrides | Process-wide static | `SetPlatformOverride(name, value)` | On a cache miss, the override is copied into this instance and the factory is skipped |
| Instance cache | This object | `SetOverride(name, value)`, or a factory result | Returned as-is, including when a platform override also exists |

`SetPlatformOverride` updates the process-wide map and this instance's cache. Another instance keeps a value it already cached until that key is removed.

`ResetCache()` clears this instance. On a Windows development build it then writes `ApplicationName` back as the current name plus `.Development`. That suffix is an instance override. The development flag is the `DEBUG` compile of Cornerstone, and the platform check is `DevicePlatform.Windows`.

`ResetCache(name)` removes one key. The next read copies a platform override again and does not re-apply the `.Development` suffix. Call `ResetCache()` when that suffix should come back.

`ContainsKey`, `TryGetValue`, `Keys`, `Values`, `Count`, and the indexer see the instance cache only. They do not run factories. The indexer throws when the key is missing. `PlatformOverrides` is a read-only view of the process-wide map.

`ToString()` calls `Refresh()` (slow lookups included) and writes one `name: value` line per cached entry.

---

## Lifecycle

`Initialize(assembly)` binds the assembly, then runs Initialize and Load when those phases have not run. A later `Initialize` leaves the assembly in place and skips phases that already ran. `SetApplicationAssembly` stores an assembly only while the field is empty. Uninitialize clears that field, so a later bind can set a new one.

| Phase | Effect |
|-------|--------|
| `LoadLifecycle` | `ResetCache()`, then `ApplicationIsLoaded = true`, then `Refresh(false)`. Slow lookups stay uncached. |
| `StartLifecycle` | If `ApplicationStartup` is still zero, store the elapsed time since construction. A second start leaves that value. |
| `StopLifecycle` | `ApplicationIsShuttingDown = true`. |
| `UnloadLifecycle` | `ApplicationIsLoaded = false`. Other cached facts stay. |
| `UninitializeLifecycle` | Drop the application assembly. |

`CompleteStartup()` calls `StartLifecycle()`. `StartShutdown()` calls `StopLifecycle()`. Prefer the lifecycle methods. The aliases remain for existing callers.

Startup and shutdown flags are instance cache writes. They are not platform overrides.

`ApplicationIsLoaded` and `ApplicationIsShuttingDown` factory defaults are `false`. `ApplicationStartup` factory default is `TimeSpan.Zero`. Load and Start replace them through `SetOverride` before anything else reads them.

---

## Refresh

`Refresh(includeSlowLookups)` reads the cheap facts so they enter the cache. Load passes `false`. The parameter defaults to `true`.

Slow lookups, skipped when the flag is false:

- `ApplicationIsReadyToRunBuild` — PE ReadyToRun header
- `DeviceId`
- `DeviceManufacturer`
- `DeviceModel`

---

## Application facts

| Property | Value |
|----------|--------|
| `ApplicationName` | Entry assembly name, or empty when no assembly is bound. A platform override (the host name from `AppBootstrap.Initialize`) replaces that. Windows development builds append `.Development` in `ResetCache`. |
| `ApplicationVersion` | Entry assembly version. `1.2.3.4` when no assembly is bound. |
| `ApplicationLocation` | `AppContext.BaseDirectory`, with a leading `file:\` removed and trailing separators trimmed. |
| `ApplicationDataLocation` | On Windows, LocalApplicationData plus `ApplicationName`. On other platforms, LocalApplicationData. |
| `ApplicationFilePath` | Base directory plus `ApplicationName` plus `.exe`. A path that ends in `.dll` has that suffix replaced with `.exe`. Used when `Assembly.Location` is empty (single-file publish). |
| `ApplicationFileName` | File name of `ApplicationFilePath`. |
| `ApplicationBitness` | Process architecture, mapped to `Bitness`. |
| `ApplicationRuntime` | Elapsed time since this instance was constructed. |
| `ApplicationStartup` | Zero until Start, then the elapsed time at the first Start. |
| `ApplicationIsDevelopmentBuild` | `true` when Cornerstone itself was compiled `DEBUG`. |
| `ApplicationIsElevated` | `true` when the platform is Windows and `Environment.IsPrivilegedProcess` is set. |
| `ApplicationIsNativeBuild` | `true` when `RuntimeFeature.IsDynamicCodeSupported` is false (Native AOT). |
| `ApplicationIsReadyToRunBuild` | `true` when the application assembly image contains a ReadyToRun header. See below. |
| `ApplicationIsLoaded` | `true` after Load, `false` after Unload. |
| `ApplicationIsShuttingDown` | `true` after Stop. |
| `CornerstoneRuntimeVersion` | `1.2.3.4` from the virtual getter. |
| `DotNetRuntimeVersion` | `Environment.Version`. |

### ReadyToRun

`ApplicationIsReadyToRunBuild` is false in the browser, and false when dynamic code is unsupported. Native AOT is not ReadyToRun.

Otherwise the check opens `_applicationAssembly.Location` when that file exists and looks for a ReadyToRun PE header (`ReadyToRunImage`). When location is empty, it scans the apphost at `Environment.ProcessPath` (or `ApplicationFilePath`) for that assembly inside a single-file bundle, including a composite `.r2r.dll`. Any failure returns false.

The flag reports that the image contains ReadyToRun native code. It does not report whether a given method ran from that code.

### Bitness

`ApplicationBitness` uses the process architecture. `DevicePlatformBitness` uses the OS architecture.

| Architecture | `Bitness` |
|--------------|-----------|
| X86 | `X86` |
| X64 | `X64` |
| Arm, Armv6 | `Arm32` |
| Arm64 | `Arm64` |
| Wasm | `Wasm64` when `IntPtr.Size` is 8, otherwise `Wasm32` |
| Anything else | `Unknown` |

---

## Device facts

`DevicePlatform` is chosen at compile time: `Android`, `Browser`, `IOS`, or `Windows`. Any other build reports `Unknown`. The `MacOS` and `Linux` enum values are not returned by this getter.

`DevicePlatformVersion` is `Environment.OSVersion.Version`.

`DeviceType` on Android, iOS, and browser follows MAUI `DeviceInfo` idiom: tablet, phone, or watch. A missing idiom, a failure, or any other idiom reports `Desktop`. Desktop builds always report `Desktop`. The `TV` and `Browser` device-type values are not returned by this getter.

| Property | Value |
|----------|--------|
| `DeviceName` | MAUI `DeviceInfo.Name` on Android and iOS. Elsewhere `Environment.MachineName`, or `"Unknown"` if that throws. |
| `DeviceId` | On Windows, a SHA-256 / Base32 hash of machine name, user name, machine GUID, system UUID, motherboard serial, and system drive serial. Elsewhere, the `DeviceId.VendorId` string when it is not null (including empty). When `VendorId` is null, the same style of hash over machine name, user name, and vendor id. |
| `DeviceManufacturer` | MAUI `DeviceInfo.Manufacturer` on Android and iOS. On Windows, the registry value, then WMI. Empty on other platforms. |
| `DeviceModel` | MAUI `DeviceInfo.Model` on Android and iOS. On Windows, the registry value, then WMI. Empty on other platforms. |
| `DeviceMemory` | Physical memory on iOS. Zero bytes on every other platform. |
| `DeviceDisplayRefreshRate` | `0`. |
| `DeviceDisplaySize` | `Size.Empty`. |

`DeviceId`, manufacturer, and model are the slow lookups. Windows manufacturer and model can touch the registry and WMI. Windows device id reads machine identity. Call `Refresh(false)` or read the other properties when Load must stay cheap. The first read of `DeviceId` still runs that work.

---

## Subclassing

`Refresh`, `GetCornerstoneRuntimeVersion`, `GetDeviceId`, `GetDeviceName`, and `GetDotNetRuntimeVersion` are virtual. A subclass replacement runs only when the cache and the platform-override map have no entry for that name. An override or a value already cached wins.

Display size, display refresh rate, and device memory have private getters. A subclass still sees the values in the table above.

---

## Sync identity

These members are the `ISyncClientDetails` surface: `ApplicationName`, `ApplicationVersion`, `DevicePlatform`, `DeviceType`, `DeviceId`, `DeviceName`, and `DevicePlatformVersion`. `AddOrUpdateSyncClientDetails` copies them onto sync headers. Reading `DeviceId` there pays the slow lookup.
