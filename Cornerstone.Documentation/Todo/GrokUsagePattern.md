# TODO: Keystone + AppDispatcher leftovers (GrokUsage as the example)

**Status:** open  
**When:** after the 2026-08 review that added `TrackIntent`, projected `TrackCollection` + `remove`, `ProjectFrom`, `[ChannelHandlers]`, `ReleaseTracks`, and period folders with token history only.

GrokMonitor usage is the intended example. Do not bind Views to State or collapse the two property bags.

## Shipped (do not redo)

- `TrackIntent` — user combo/slider → bus; apply is projecting
- Projected `TrackCollection(same, create, update, remove)`
- Host homes list via a child projector (`HomesChanged` / `SyncHomeTabs` gone)
- `[ProjectFrom<TContract>]` destination-bag scalars
- `[ChannelHandlers]` opt-in auto-subscribe (`OnRefreshHome` → `SubscribeTo*`)
- `[ChannelHandlers]` on GrokUsage, Browser, SourceControl, PowerShell, Release, Vault processors (hand-written On* lists removed)
- `ReleaseTracks` — `UninitializeLifecycle` already calls it; rebind hosts call it only when the session/repo id changes
- Replay advances from `ProcessLifecycle` (no feature `Timer`)
- Period dropdown only lists weeks with subscription inferences; session-only archive folders are pruned
- Per-home view clock — `ViewAsOf` / `IsViewLive` / `IsReplayPlaying` on `GrokHomeUsageState`; `SetViewAsOf` / `SetViewLive` / `StartReplay` / `StopReplay` take `HomeId` (no `FindSelected`)
- Nested `*Message` types (`RefreshHomeMessage`); generator infers only the `Message` suffix (no `MessageFor`)

## Still open

| Item | Why |
|------|-----|
| **Feature composition** | Adding a channel/processor still means editing `AppBus` / `AppState` / `AppEngine` ctors. Generate those `Track` properties. |
| **Generated `WireDispatchTracks`** | Same idea as `[ChannelHandlers]`: emit Track* from a recipe so authors do not write Init Track / forget a twin. `ReleaseTracks` on Uninitialize is already framework-owned. |
| **`IAppDispatcher.Track` rename** | Breaking. Only if that interface is already being broken (`AddDispatchable` / `RemoveDispatchable`). |

## Out of scope

- Binding XAML to `*State`
- One property bag
- Docking GrokMonitor
- Changing AppDispatcher idle/active rates

## Related

- [Keystone.md](../Keystone.md)
- [AppDispatcher.md](../AppDispatcher.md)
- [KeystoneFeatureTab.md](../KeystoneFeatureTab.md)
