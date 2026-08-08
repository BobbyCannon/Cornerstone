# Debounce and Throttle

`DebounceThrottleManager` exists primarily to support the richer, multi-instance scenario with these capabilities:

- **Queuing** of triggers (the `SpeedyQueue` + `QueueTriggers`)
- Many `DebounceProxy` / `ThrottleProxy` instances managed by a single efficient background worker
- Shared `IDateTimeProvider`
- Dynamic sleep calculation across all proxies
- Cancellation tokens, `AllowTriggerDuringProcessing`, lifecycle integration, etc.

If you only need a simple fire-and-forget behavior (no payload queue, single instance, no shared worker),
then the full manager is **not necessary**.

| Need                                      | Simple `Throttle` / `Debounce` | Full `DebounceThrottleManager` |
|-------------------------------------------|--------------------------------|--------------------------------|
| Leading-edge throttle (fire then ignore)  | ✅                             | ✅                             |
| Trailing debounce (wait for silence)      | ✅ (with one timer)            | ✅                             |
| Queue multiple triggers / values          | ❌                             | ✅                             |
| Many independent instances efficiently    | ❌ (each has own timer)        | ✅ (one worker)                |
| Shared time provider / advanced control   | ❌                             | ✅                             |

So for the lightweight use cases, the standalone classes are the better fit.
The manager only becomes worthwhile once you want queuing or centralized management of many instances.