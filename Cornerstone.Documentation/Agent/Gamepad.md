# Gamepad

Sample Inputs → Gamepad (`TabInputsGamepad.cxaml`) on every platform. Status as of 2026-10-09. The tab UI is shared. What moves is the platform `Gamepad` registered with dependency injection.

## Handoff

Next session: make that tab show a live pad on Windows, Linux, macOS, iOS, Android, and the browser. Windows and the browser already had readers and feel immediate. Linux was checked again on the Ally X on 2026-10-09. macOS and iOS readers are in the tree and have not been confirmed on hardware. Android is still `GamepadStub`.

On that check the sample still showed two pads and button response was far behind Windows and the browser. Two bugs:

1. Virtual detection used the `device` symlink. Its target is `../../inputN`, which does not contain `/devices/virtual/`, so the Elite pad and the Steam pad were both treated as hardware.
2. `Update` drained the evdev queue on the UI thread, one event per `read` and `GCHandle`. A moving stick fills that queue and the sample cannot paint until the drain finishes. Windows is one `XInputGetState`. The Linux poll is now the same shape: `EVIOCGKEY` plus `EVIOCGABS` for axes already known.

Device choice is now hardware, then any other virtual pad, then Steam's Xbox 360 pad only if it is the only readable gamepad. On the Ally that is event15, `Microsoft X-Box One Elite 2 pad`. Confirm with a rebuilt sample: one connected row, and a face button moves that row without a visible delay. The console line is `Linux gamepads:` plus the chosen event path.

## How the sample tab works

`TabInputsGamepad` is a `SampleUserControl`. Attach starts monitoring. Detach stops it.

- `GamepadsManager` (Sample, not Relic) owns four slots, indexes 0–3.
- `CreateGamepad` calls `TryGetInstance<Gamepad>()` only when `Gamepad` is not a singleton. Each call gets a new platform pad and sets `State.Index`.
- A singleton registration is the stub. Each slot is then its own `GamepadStub`, and `Update` does nothing. `IsConnected` stays false.
- Monitor starts `GamepadsManager.StartWorking()`, which polls `Update` on each slot.
- `Gamepad.Changed` appends `GamepadState` to `GamepadHistory` (limit 100). The list row shows time, index, and buttons.
- `GamepadView` binds `Gamepad.State` and is visible only while `State.IsConnected` is true.
- Design mode marks slot 0 connected and does not start the worker.

Relic has its own `GamepadsManager` with the same transient-vs-stub rule. Do not treat the two types as one.

## Registration

`Platform.Create` picks the TFM with `#if ANDROID / BROWSER / IOS / WINDOWS`, otherwise `NullPlatform`. Linux and macOS share `net10.0` and are chosen at runtime with `OperatingSystem.IsLinux()` / `IsMacOS()`.

| Platform | Registration | Reader | Hardware check |
|----------|--------------|--------|----------------|
| Windows | `AddTransient<Gamepad, WindowsGamepad>` | XInput `XInputGetState(State.Index)` | Already in use before this work |
| Browser | `AddTransient<Gamepad, BrowserGamepad>` | `navigator.getGamepads` via `GamepadHelper.read` | Already in use. Missing `[SourceReflection]` freezes the page. See [SourceReflection.md](SourceReflection.md) |
| iOS | `AddTransient<Gamepad, IOSGamepad>` | `GCExtendedGamepad` only | Not run on a device |
| Linux | `NullPlatform` → `AddTransient<Gamepad, LinuxGamepad>` | evdev `/dev/input/event*` | Ally X. Elite-only filter and ioctl snapshot copied to `~/Cornerstone.Sample.Desktop` on 2026-10-09. Relaunch and confirm one fast pad |
| macOS | `NullPlatform` → `AddTransient<Gamepad, MacOSGamepad>` | GameController.framework via libobjc | Not run on a device |
| Android | `AddSingleton<Gamepad, GamepadStub>` | none | Tab stays disconnected |

`Cornerstone.csproj` compiles `Platforms/Linux/**` and `Platforms/MacOS/**` for plain `net10.0`, `Platforms/iOS/**` for `net10.0-ios`, `Platforms/Windows/**` for the Windows TFM, and `Platforms/Browser/**` for `net10.0-browser`.

Transient readers need `[SourceReflection]` and `[DependencyInjectionConstructor]`. iOS also needs `[Preserve(AllMembers = true)]`. No field initializers. Inject `IDateTimeProvider`.

`Tests/Cornerstone.UnitTests` is `net10.0-windows` only, so Linux and macOS types are not in that compilation. `GamepadValuesTests` covers axis scaling (6 tests). There is no device test.

## Reader behavior

Slots follow `State.Index` into the platform's device list (0–3). Kernel and GameController Y axes are normalized to positive-up. Triggers are bytes 0–255. Thumbs are shorts.

- **Windows.** Packet number skips an unchanged XInput packet. `ERROR_DEVICE_NOT_CONNECTED` clears the slot.
- **Browser.** One poll reads all `navigator.getGamepads()` entries. Standard mapping. Y is already positive up.
- **iOS.** `GCController.Gamepad` / `GCGamepad` is obsolete on the iOS 26 target (CA1422). Extended profile only. Controllers without `ExtendedGamepad` are skipped. `ButtonOptions` → Back, `ButtonMenu` → Start.
- **macOS.** `dlopen` libobjc and GameController.framework. x64 float returns use `objc_msgSend_fpret`. An `NSAutoreleasePool` wraps each poll. Y is already positive up. A basic `GCGamepad` fallback is still present.
- **Linux.** `BTN_GAMEPAD` (`BTN_SOUTH`) is required. Stick Y from the kernel is positive-down and is inverted. Triggers prefer `ABS_HAT2Y` / `ABS_HAT2X`, then `ABS_Z` / `ABS_RZ`, then the digital trigger buttons. ioctl sizes are `nuint` so the high bit is not sign-extended. `EACCES` is logged once. The process needs read access to the event node (typically the `input` group, or an ACL). Each poll snapshots with `EVIOCGKEY` and `EVIOCGABS`. It does not read the event queue.

Linux device choice, in order:

1. Every readable non-virtual gamepad, if any can be opened.
2. Otherwise every readable virtual gamepad that is not Steam's Xbox 360 pad (InputPlumber's Xbox Elite target, a lab uinput pad, and so on).
3. Otherwise Steam's virtual Xbox 360 pad: vendor `28de`, product `11ff`, or a name that starts with `Microsoft X-Box 360 pad`.

Virtual means `realpath` of `/sys/class/input/eventN` contains `/devices/virtual/`. The `device` symlink alone is not enough.

## Ally X (SteamOS)

Host `bobbys-allyx` (`192.168.2.2`), SSH user `deck` with `~/.ssh/id_ed25519`. `bobby@` is denied. `sudo` asks for a password. Do not stop `inputplumber` or reboot as a probe. A previous stop logged `Unable to un-hide devices` and left nodes unreadable until it was started again.

SteamOS 3.8.28 (holo), kernel `6.18.50-valve2-1-neptune-618`. Board RC72LA. Physical pad USB `0b05:1b4c`, driver `asus_rog_ally`, name `ASUS ROG Ally X Gamepad`.

What boot does:

1. The kernel creates the Ally evdev pad.
2. InputPlumber loads `/usr/share/inputplumber/devices/50-rog_ally_x.yaml` and opens an Xbox Elite target (`045e:0b00`, `Microsoft X-Box One Elite 2 pad`) plus a virtual mouse and keyboard. It hides the physical node (mode `000`).
3. About two seconds later, `steamos-manager` 26.4.1 forces that composite device to `deck-uhid` because `/usr/share/steamos-manager/devices/rog-ally-series.toml` has no `[inputplumber]` `target_devices`. The Rust default is only `deck-uhid`. `make_deck` runs on `InterfacesAdded` and once at user-daemon start. It does not re-apply after a later `SetTargetDevices`.
4. `deck-uhid` is uhid `28de:12fd` (`ROG Ally X Controller` on hidraw). `hid-steam` does not claim `12fd`, so hid-generic creates no joystick. Steam logs `Unrecognized controller using V1 HID protocol`. Settings → Game Controller stays empty.
5. Steam also creates `Microsoft X-Box 360 pad 0` (`28de:11ff`). That node stays readable.

While the Elite target is also up, evdev shows two gamepads. On 2026-10-09 both were readable: event15 `Microsoft X-Box One Elite 2 pad` (`045e:0b00`, virtual) and event18 `Microsoft X-Box 360 pad 0` (`28de:11ff`, virtual). event14, the physical Ally pad, stays mode `000`. Idle event rate on both virtual nodes was zero, so the lag is not a resting flood. The sample should keep event15 only. Steam's 360 node is a second hop for the same pad.

A deck user service keeps the Elite target across login, because `/usr` is read-only and `/etc/steamos-manager` is root-only:

- `/home/deck/.local/bin/ally-gamepad-target.sh`
- `/home/deck/.config/systemd/user/ally-gamepad-target.service` (enabled)

It waits for `CompositeDevice0`, then if the gamepad type is `deck-uhid` calls `SetTargetDevices` with `xbox-elite`, `mouse`, and `keyboard`, and writes `GamepadOrder` back to `CompositeDevice0`. InputPlumber 0.78.0 drops that order when the old target stops (the "still attached" skip is not in this build). The script polls every 20 seconds and backs off 10 minutes if a switch does not stick. Turn it off with `systemctl --user disable --now ally-gamepad-target.service` (`XDG_RUNTIME_DIR=/run/user/1000`).

The durable OS fix is an `[inputplumber]` section on the Ally device toml, `target_devices = ["xbox-elite", "keyboard", "mouse"]`. That file is on the read-only usr, and a SteamOS update would replace it.

`busctl` calls from PowerShell must use a single-quoted here-string so remote `$` is not expanded locally.

## Still open

- Relaunch `~/Cornerstone.Sample.Desktop` on the Ally and confirm one connected pad (the Elite node, event15) and that a face button shows up as fast as Windows. Console: `Linux gamepads: /dev/input/event15`.
- macOS: exercise `MacOSGamepad` with a controller. The basic `GCGamepad` path is still compiled.
- iOS: exercise `IOSGamepad` on a device or simulator with an extended controller.
- Android: there is no reader. `AndroidPlatform` still registers the stub. A reader has to be transient, with `[SourceReflection]`, or every slot stays a stub.
- `GamepadValues` scaling is tested. Platform `Update` paths are not.
- Do not edit client, entity, or sync models for this work.

## File map

| Piece | Path |
|-------|------|
| Sample tab | `Cornerstone/Cornerstone.Sample/Tabs/Inputs/TabInputsGamepad.cxaml` (+ `.cxaml.cs`) |
| Sample slots | `Cornerstone/Cornerstone.Sample/Tabs/Inputs/GamepadsManager.cs` |
| Relic slots | `Cornerstone.Relic/Cornerstone.Relic/Core/Input/GamepadsManager.cs` |
| Face widget | `Cornerstone/Cornerstone.Presentation/Controls/GamepadView.cs` |
| Shared state | `Cornerstone/Cornerstone/Input/Gamepad.cs`, `GamepadState.cs`, `GamepadValues.cs` |
| Windows | `Platforms/Windows/WindowsGamepad.cs` |
| Browser | `Platforms/Browser/BrowserGamepad.cs` |
| iOS | `Platforms/iOS/IOSGamepad.cs`, `Platforms/iOS/IOSPlatform.cs` |
| Linux | `Platforms/Linux/LinuxGamepad.cs`, `Platforms/Linux/NativeLinuxEvdev.cs` |
| macOS | `Platforms/MacOS/MacOSGamepad.cs`, `Platforms/MacOS/NativeMacGameController.cs` |
| Runtime switch | `Platforms/NullPlatform.cs` |
| Android stub | `Platforms/Android/AndroidPlatform.cs` |
| Scaling tests | `Tests/Cornerstone.UnitTests/Input/GamepadValuesTests.cs` |
