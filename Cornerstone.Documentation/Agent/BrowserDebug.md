# Browser lockup console trace

How to find the call that does not return when the Cornerstone browser sample locks. Do this before changing the dispatcher, BrowserThreads, or the page that was clicked.

This is the procedure that found the Welcome card lockup. The page swap was fine. `GetInstance<BrowserInteropProxy>()` threw.

## Procedure

1. Do not start with a workaround. Time-slicing `ExecuteJobsCore`, posting the tab change, and staggering hub cards were tried and reverted. None of them named the failing call.
2. Add `Console.WriteLine` on the suspected stack. Prefix every line `TabTrace`. Number by tens (`10`, `20`, `30`) so a step can be inserted. Call `Console.Out.Flush()` after every line. A hang still shows the last flushed line.
3. Put a line before and after each suspect call, not only at the start of a method. The missing number is the call that did not continue.
4. Ask for one action and the console text. Steps that worked: load the page, open the developer tools console, clear the console, click one control. A menu click prints some of the same lines, so the console has to be clear first.
5. The last `TabTrace` number is the step that did not continue. Lines that print later, especially a `finally`, mean the stack unwound. That is an exception, not a loop.
6. When a `finally` prints and the next line inside the `try` does not, catch the exception and print `ex.ToString()` as its own `TabTrace` line. Flush it. That text is the bug.
7. Browser `Console.WriteLine` shows up in the developer tools console from `dotnet.native.*.js`. Copy the `TabTrace` lines and any exception text after them.
8. Remove every `TabTrace` line after the cause is known. Do not leave the traces in the tree.

## Where the Welcome trace sat

| Number | Call |
|--------|------|
| 10 / 12 | Welcome card handler in `TabWelcome.cxaml.cs` |
| 20 / 30 / 40 | `AppViewModel.SelectTabByType` around the `SelectedTab` assignment |
| 50–110 | `ContentPresenter.UpdateChild`: enter, created, remove old, old removed, add new, added, done. Logged only when the content is `TabItemReferenceViewModel` or a `Cornerstone.Sample.Tabs` control |
| 120 / 130 / 140 / 150 | `MouseDevice.MouseUp`: before `RaiseEvent`, after `RaiseEvent`, `finally` before `CaptureLost`, after `CaptureLost` |
| 125 / 128 | `Button.OnPointerReleased` around `OnClick` |
| 160 / 170 | `Pointer.OnCaptureDetached` before and after retargeting capture |
| 180 / 182 | Browser `setPointerCapture` |
| 190 / 192 | Browser `releasePointerCapture` |
| 200–210 | `Cornerstone.Sample.Browser` `Program.AppViewModelOnPropertyChanged` around the URL read and write |
| 135 / 206 | `ex.ToString()` from `MouseUp` and from the URL handler |

The numbers are not printed in numeric order. `125` runs before `50`. `160` runs inside `70`.

## What that run proved

`UpdateChild` created the next tab, removed Welcome, and attached the new page. `60` through `110` all printed. Pointer capture moved from the button `Border` to the `ContentPresenter`, and `setPointerCapture` returned (`160`, `180`, `182`, `170`).

The next missing line was `202`. `200 URL read` had printed. The stack then unwound: `140` through `150` printed, and `40`, `128`, `12`, and `130` did not. The click never resumed, so the browser never got back to a render. The new page was already in the tree, which is why the screen looks frozen rather than unchanged.

`TabTrace 206` was:

`Cornerstone.Runtime.DependencyInjectorConstructorException: An injectable constructor could not be found for Cornerstone.Presentation.Platforms.Browser.CornerstoneBrowserInteropProxy.`

Thrown from `AppBootstrap.GetInstance<BrowserInteropProxy>()` inside `Program.AppViewModelOnPropertyChanged`, wrapped by `TargetInvocationException` because the Welcome click handler is invoked through `DynamicInvoke`.

## Fix

`HostAppBuilderExtensions.UseCornerstone` had registered the proxy with `SetTransient<BrowserInteropProxy, CornerstoneBrowserInteropProxy>()` and no factory. That path runs `SourceReflector` constructor discovery. `CornerstoneBrowserInteropProxy` has no `[DependencyInjectionConstructor]`, so every `SelectedTab` change threw.

Register it with a factory, the same shape as `WebViewAdapter` in that method:

`SetTransient<BrowserInteropProxy, CornerstoneBrowserInteropProxy>(() => new CornerstoneBrowserInteropProxy())`

Startup on Controls still worked because `StartLifecycle` assigns the menu selection before `Program` subscribes to `PropertyChanged`. A later change raises the same handler.

The shell menu is now Home, About, and Settings. `AppViewModel.SelectedTab` is that page. `Program` writes `?Tab=` from `SelectedTab.TabName`. `SelectTabByType` selects Home and pushes the section on `TabWelcome`.

Related: [DependencyInjection.md](DependencyInjection.md) (`SetTransient<T, T2>()` with no factory calls `CreateInstanceForDependencyInjection`).

## File map

| Area | Path |
|------|------|
| URL handler | `Cornerstone/Cornerstone.Sample.Browser/Program.cs` |
| Proxy registration | `Cornerstone/Cornerstone.Presentation/Platforms/Browser/HostAppBuilderExtensions.cs` |
| Proxy type | `Cornerstone/Cornerstone.Presentation/Platforms/Browser/CornerstoneBrowserInteropProxy.cs` |
| Constructor lookup | `Cornerstone/Cornerstone/Runtime/DependencyProvider.cs` (`CreateInstanceForDependencyInjection`) |
| Welcome cards | `Cornerstone/Cornerstone.Sample/Tabs/Welcome/TabWelcome.cxaml.cs` |
| Tab assignment | `Cornerstone/Cornerstone.Sample/AppViewModel.cs` (`Tabs`, `SelectedTab`, `SelectTabByType`) and `Tabs/Welcome/TabWelcome.cxaml.cs` |
| Page swap | `Cornerstone/Cornerstone.Presentation/Controls/ContentPresenter.cs` (`UpdateChild`) |
| Pointer-up | `Cornerstone/Cornerstone.Presentation/Input/MouseDevice.cs` (`MouseUp`) |
| Capture retarget | `Cornerstone/Cornerstone.Presentation/Input/Pointer.cs` (`OnCaptureDetached`) |
| Browser capture | `Cornerstone/Cornerstone.Presentation/Platforms/Browser/BrowserMouseDevice.cs` (`PlatformCapture`) |
