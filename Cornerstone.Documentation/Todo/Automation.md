# TODO: Cornerstone.Automation leftovers

**Status:** open  
**When:** after the 2026-08 modernization of `Cornerstone.Automation` (phases 0–3).

UI automation for Windows desktop (UI Automation COM) and browsers (CDP / injected JS). In-process Cornerstone.Presentation `WebView` automation is a separate package.

## Shipped (do not redo)

- Collection `Remove` mutates the real list; string indexer / `Contains` use `FirstOrDefault`
- `DesktopElement.Visible` uses clickable point or focus when offscreen
- `GetWindows` does not `yield` under `lock`; windows de-duped by handle; no desktop-root UIA walk (that walk could stall ~60s)
- `First` / `FirstOrDefault` wait on `Application.Timeout`; lookup is AutomationId, FullId, Id, and Name
- COM refresh retries are bounded
- One cached `IUIAutomation` (`DesktopAutomation`)
- Desktop ID/name find via UIA `FindFirst`; typed find via `FindAll`; `Refresh` loads immediate children only
- Web ID/name find via `Cornerstone.findElements` (`getElementById` / `querySelector`); attribute snapshot on refresh
- Injected JS is minified once (`ResourceService`)
- Attach waits for a real HWND; `MoveWindow` restores then waits for bounds; location/size from `GetWindowRect`
- `TimeProvider`, `Keyboard`, and `Mouse` are injectable on `ElementHost` (defaults: real time, `WindowsInput`)
- Chrome/Edge navigate waits on CDP page events (`Page.loadEventFired` and kin) with `document.readyState` fallback; Firefox waits on `readyState` / `listTabs`
- Core `Cornerstone.Automation` does not reference Cornerstone.Presentation. In-process WebView is `Cornerstone.Automation.Presentation` (`WebViewAutomation`)
- One web model: `Cornerstone.Automation.Web` (`WebAutomation` / `WebElement`) for WebView and Chrome/Edge/Firefox
- Typed web elements only where behavior exists (Button, Form, Input, Link, Select, Option, Table, TextArea, CheckBox, RadioButton)
- Create/find is tag (and input type) to typed element, otherwise `WebElement`

## Still open

### Optional polish (not a named phase)

| Item | Why |
|------|-----|
| `WaitForComplete` | Still a delay, not UI idle. Desktop could wait on UIA events; browsers already have load/readyState. |
| Injected JS ids | `ensureId` still stamps `id="cornerstone-N"` on the page. Prefer a data attribute or weak map so host CSS/JS is not rewritten. |
| `JavaScriptLibrary` | Still detects AngularJS, Bootstrap 2/3, jQuery, Moment, Vue. Does not detect React or modern Angular/Bootstrap. |
| Click / type pacing | Desktop clicks still `Delay(100)`; typing delays are real-time only. Could follow UIA or input completion instead. |

## Out of scope

- Putting `WebViewAutomation` back into `Cornerstone.Automation` (that would pull Cornerstone.Presentation/WebView2 into every consumer)
- Putting `WebViewAutomation` into `Cornerstone.Presentation.Theme` (the UI library would then reference Automation)

## Packages

| Project | Role |
|---------|------|
| `Cornerstone.Automation` | Desktop UIA, out-of-proc browsers, shared element host |
| `Cornerstone.Automation.Presentation` | In-process `WebViewAutomation` only |

In-process WebView tests need a reference to `Cornerstone.Automation.Presentation`.

## Related

- `Cornerstone/Cornerstone.Automation/`
- `Cornerstone/Cornerstone.Automation.Presentation/`
- `Cornerstone/Tests/Cornerstone.AutomationTests/`
