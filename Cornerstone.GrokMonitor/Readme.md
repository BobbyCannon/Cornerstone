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

## How usage history is kept

Grok Monitor does **not** rely on the Grok CLI log staying around. The CLI rotates or rewrites `logs/unified.jsonl`; if that were the only copy, older weeks would disappear.

Instead, each time the app refreshes a home (startup, **Refresh**, or when it notices the log or sessions change), it **copies new usage into its own archive** and then reads the dashboard from that archive.

- **What is copied:** billing polls and inference (token) events from the CLI log, plus session titles and folders from `sessions/`.
- **Where it lives:** under the app’s application-data folder, in `usage-archive`, one directory per Grok home (for example `.grok` or `.grok-work`). Inside that, history is split by billing period (`periods/…`). Closed weeks stay as-is; only the current period keeps growing.
- **Duplicates are ignored:** importing the same log again does not double-count. Safe to leave the app running or hit Refresh often.
- **After the CLI log is wiped or rotated:** already-imported weeks remain. You will still see past periods in the dropdown.

What this does **not** recover:

- Usage that never made it into a refresh (for example the CLI rotated the log before Monitor ran). There is no download from xAI’s servers.
- Deleting or moving the `usage-archive` folder. That **is** your history.
- A Grok home that the app maps to a *new* archive folder (uncommon; usually if the same folder name is reused for a different path). That looks like a fresh home until you import again.

Practical habit: leave Monitor running, or open it now and then, so new CLI activity is copied before the log turns over. Do not delete the app’s `usage-archive` directory if you care about past weeks.

## Notes

- On startup and on every **Refresh**, re-discovers existing `~/.grok*` folders (plus `GROK_HOME` / `GROK_WORK_HOME` when set), loads usage, and keeps one dashboard tab per home.
- Each known home’s `logs/unified.jsonl` and `sessions/**` tree is watched on disk. Changes trigger a **throttled** refresh of that home (~1s coalesce via `Throttle`: leading edge + one trailing edge). Manual **Refresh** / **Refresh All** still run immediately.
- Refresh **imports** from the CLI home into `usage-archive` (deduped append), then the dashboard reads **only the archive**. See **How usage history is kept** above.
- Tab titles use the folder name with leading dots stripped (`grok`, `grok-work`, …). Full path is on the tab tooltip.
- When only one home is found, the tab header strip is hidden; it reappears if refresh finds a second home.
- Events / summary files open via the shell (not an in-app editor).
- Domain types keep the `GrokUsage*` names; the **app** is Grok Monitor.
