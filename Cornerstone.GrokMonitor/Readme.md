# Cornerstone.GrokMonitor

Desktop-only sample app that tracks local **Grok CLI** usage (discovered `~/.grok*` homes) using **Keystone** and **AppDispatcher**.

## Why this exists

A small, complete host for a Keystone feature without docking:

| Layer | Role |
|-------|------|
| `AppBus.GrokUsage` | Typed intent (refresh, select period, view clock) |
| `GrokUsageProcessor` | Reads disk, mutates state only |
| `AppState.GrokUsage` | UI-free domain snapshot |
| `GrokUsageTabViewModel` | One dashboard per home; projects state via AppDispatcher |
| Shell | One tab per discovered Grok home (folder-name labels); tab strip hidden when only one home |

See [KeystoneFeatureTab.md](../Cornerstone.Documentation/KeystoneFeatureTab.md) for the docking variant used in Editor; this app is the multi-page host without DockingManager.

## Run

```text
dotnet run --project Cornerstone.GrokMonitor
```

Windows desktop only (`net10.0-windows…`).

## Notes

- On startup and on every **Refresh**, re-discovers existing `~/.grok*` folders (plus `GROK_HOME` / `GROK_WORK_HOME` when set), loads usage, and keeps one dashboard tab per home.
- Each known home’s `logs/unified.jsonl` and `sessions/**` tree is watched on disk. Changes trigger a **throttled** refresh of that home (~1s coalesce via `Throttle`: leading edge + one trailing edge). Manual **Refresh** / **Refresh All** still run immediately.
- Tab titles use the folder name with leading dots stripped (`grok`, `grok-work`, …). Full path is on the tab tooltip.
- When only one home is found, the tab header strip is hidden; it reappears if refresh finds a second home.
- Events / summary files open via the shell (not an in-app editor).
- Domain types keep the `GrokUsage*` names; the **app** is Grok Monitor.
