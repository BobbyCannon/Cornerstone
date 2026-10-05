# Architecture Review: Cornerstone Sync Framework

Review based on `Cornerstone/Sync/*`, EF adapters, and how the pieces actually compose. This is an engineering review, not a product pitch.

---

## Executive take

This is a **serious, production-shaped sync engine**, not a toy. The layering is coherent, the hard problems (identity, paging, soft delete, relationship order, batch→individual fallback, server sanitization) are real and mostly in the right places.

It is also clearly **evolved systems code**: strong core model, a few incomplete features, a few inheritance/API surprises, and some correctness risks that will bite under concurrency, partial failure, or multi-device conflict.

**Overall grade:** solid foundation for client↔server entity sync; **not** yet a multi-master CRDT/vector-clock system, and a few paths look unfinished or inverted.

---

## Open defects (code walk 2026-10-05)

Checked against `Cornerstone/Sync`, the EF and SQL sync repositories, and `Cornerstone.UnitTests/Sync`. Human behavior is in [../Sync.md](../Sync.md). Implementer rules are in [../Agent/Sync.md](../Agent/Sync.md). These are unfixed.

1. **Queue poison after `waitFor` timeout.** `SyncAsync` enqueues the session id, then `StartSyncSession` returns null when this id is not the head before the timeout. That path does not `TryDequeue`. The id stays. When the run ahead finishes, the abandoned id is the head and has no worker. Later `SyncAsync` calls with `waitFor: null` return `CouldNotStart` because the queue is non-empty. Callers that pass `waitFor` enqueue behind the abandoned id and time out the same way. `ConcurrentSyncWaitTimeoutCannotStart` asserts the timeout result and does not start another run.

2. **`Sync()` drops `CouldNotStart`.** It starts `SyncAsync`, waits, and returns the manager's live `SyncSession`. Disabled manager: the task has the `SyncManagerDisabled` issue, and `Sync()` returns the previous live session. Busy manager with `waitFor: null`: `Sync()` waits for the other run and returns that run's session. `SyncAsync` returns the right object. `DisabledManagerDoesNotStartAndCallsPostAction` uses `SyncAsync` only.

3. **`SyncCompleted` while `SyncRunning`.** `ProcessSyncSession` calls `OnSyncCompleted(this)` before `UpdateState(Completed)` on the live session. The result copy is already `Completed`. During the event, `SyncSuccessful` is already decided and `SyncRunning` is still true. `SyncCompletedSeesStoredStampsBeforeDispatcherRuns` calls `RaiseCompleted` directly and does not cover this order.

4. **`ClientNotSupported` is never assigned.** `ServerSyncClient.ValidateSyncClient` false throws `CornerstoneException` (`BabelKeys.SyncClientNotSupported`). `SyncSession.HandleException` records `SyncIssueType.ClientException`. The enum value `ClientNotSupported` has no writer.

5. **Pull page size stops at 1,000.** `SyncRequest` defaults `Take` to 1000. `SyncClient.Sync` builds the pull request without setting `Take`. `GetChanges` uses that `Take` when it is positive and no larger than `ItemsPerSyncRequest`. A hub page of 10,000 still returns 1,000 rows per pull call. Push sets `Take` to `ItemsPerSyncRequest` so the short-page check does not end the session early. Pull still pages via `HasMore`, so the window is not dropped. It just takes more calls than the configured page size.

6. **In-process sanitization mutates the client settings.** One `SyncSettings` instance is passed to the client and to every `server.Sync`. `ServerSyncClient.BeginSync` sets `PermanentDeletions` false, clamps `ItemsPerSyncRequest`, and sets `IncludeIssueDetails` false on that instance. Sample loopback posts the same object (`SampleWebClient`). After the first server call, the client apply sees hub policy. A serializing HTTP client would not write those fields back onto the caller.

7. **Relationship cache is static per entity type.** `SyncClientForDatabase._relationshipCache` is a `ConcurrentDictionary<Type, Relationship[]>` built from the first database's `GetSyncableRepositories()`. A later database in the same process keeps that resolution. Sample loopback uses the same CLR types on both sides, so it does not show this. A narrower database that runs first can hide a relationship for every later database.

8. **Exclude map bails out when issues outnumber the page.** `ApplyIncoming` returns before updating exclude when `failed.Count >= filtered.Count`. Direct failures store the incoming `SyncId` on the issue. A second-walk relationship failure also stores the related `SyncId`, and `UpdateEntity` adds a wrapper whose `Id` is the incoming `SyncId`, so one failed object often counts as two issues. That count can close the early return and skip exclude for siblings that did apply. Those siblings can be pushed again in this run. When the loop runs, the wrapper id keeps the failed object out of the map. The run stays unsuccessful while any issue remains, so the next session retries the window.

9. **Failed batch replays issues.** Per-object issues are added before `SaveChanges`. The batch `catch` then runs every object again. Rows that already recorded an issue record it again. Saved siblings from the individual pass stay saved. Success is still all-or-nothing for the stamps.

10. **Corrections are one page.** `ProcessCorrections` sends `SyncIssues.Take(ItemsPerSyncRequest)` once. It does not loop. Default `GetCorrections` is still empty, so this only matters when a host overrides it.

Still true, and still not bugs by themselves: `WebSyncClient : ServerSyncClient`, last-write-wins on `ModifiedOn`, filter-as-include, `SyncObject.ToSyncModel` via `Type.GetType`, and FK detection by exception text (`FOREIGN KEY constraint` and `REFERENCE constraint`). `WaitForSyncsToComplete` itself waits while the queue is non-empty or the session is running. The queue leak above is the hole in that fix.

File name `SyncIsssueException.cs` spells the class `SyncIssueException` with an extra s in the file name.

---

## What is genuinely well designed

### 1. Three-layer data model (Entity / Model / Object)

Separating **storage entity**, **wire model**, and **transport envelope** is the right split:

| Layer | Job |
|-------|-----|
| `SyncEntity` | Local PK + global `SyncId` + domain |
| `SyncModel` | Packable DTO |
| `SyncObject` | Typed envelope + status |

That gives you:

- Local IDs that never leak as identity
- Wire format control (`[Packable]`)
- Status independent of full deserialize

This is better than “serialize the EF entity and hope.”

### 2. Global identity via `SyncId`

Using a non-reusable `Guid` as the sync key is the correct baseline for offline-capable clients. Coupling that to optional `DatabaseKeyCache` for FK resolution is pragmatic and performance-aware.

### 3. Session orchestration is understandable

`SyncManager` (queue + settings + UI hooks) vs `SyncSession` (run) vs `SyncClient` (protocol) is a clean separation of concerns. The pull-then-push loop with an exclude map so reverse direction doesn’t echo the same change is good operational thinking.

### 4. Apply pipeline is battle-aware

`SyncClientForDatabase` shows real scars:

- Batch apply → on failure, individual apply
- Status normalization (Added/Updated/Deleted reconciliation)
- Soft-delete that can “add then delete” so peers learn about deletes
- Parent-before-child apply order; deletes reverse when permanent
- Server maintains `ModifiedOn`; clients don’t during apply

That is not accidental architecture. That is experience.

### 5. Server distrust of client settings

`ServerSyncClient.BeginSync` clamping page size and forcing `PermanentDeletions = false` is the right security posture. Client settings are suggestions; server owns policy.

### 6. Property-level sync control via `UpdateableAction`

Integrating sync include/exclude into the same update pipeline used elsewhere is consistent with Cornerstone’s design language. Once you understand the flags, you get fine-grained control without a second metadata system.

---

## Where the architecture is weak or wrong

### 1. Corrections round-trip is an empty placeholder

`SyncClientForDatabase.GetCorrections` / `ApplyCorrections` validate the session and return empty results. `SyncSession` still posts issues after a failed apply; nothing is repaired unless a subclass overrides.

Intentional: issues are reported, not auto-fixed. Do not teach “force older data” via corrections.

### 2. Inheritance inversion: `WebSyncClient : ServerSyncClient`

A remote HTTP client is not a server. Inheriting `ServerSyncClient` pulls in:

- Server semantics (`IsServerClient` via `this is ServerSyncClient`)
- Server `BeginSync` sanitization path (overridden by web posts, but type identity still lies)
- Database provider in a type that primarily posts over the wire

This will confuse every new reader and may cause subtle bugs if any base behavior keys off `is ServerSyncClient`.

**Recommendation:**  
`SyncClient` ← `SyncClientForDatabase` ← `ServerSyncClient`  
`SyncClient` ← `WebSyncClient` (composition of `IWebClient`, not server inheritance)

### 3. `WaitForSyncsToComplete` (loop fixed, queue leak open)

`WaitForSyncsToComplete` returns immediately when the queue is empty and the session is not running. The loop continues while the queue is non-empty or `SyncRunning` is true. Do not restore a wait that continues only while the queue is already empty and the session is already complete.

A `waitFor` timeout still leaves its session id in `_syncQueue`. See “Open defects” item 1. Dequeue on that path, or the wait loop will treat a dead id as a running queue forever.

### 4. Success model is all-or-nothing, but time advancement is easy to get wrong

`Successful` requires no issues. Manager only **persists** last-synced on success—good.

But inside the session, `Settings.LastSyncedOn*` are still written from session start times before success is decided. Any caller reading settings off the live session mid-flight or off a failed response copy can think the window advanced when it didn’t persist—or the inverse if someone copies settings incorrectly.

More importantly: **partial success is not modeled**. If 99/100 objects apply and one fails, the whole session is unsuccessful and you re-sync the whole window next time (minus exclude map within the same run only). That is simple and safe, but can be expensive and re-apply-heavy. Fine for small datasets; painful for large ones.

### 5. Conflict strategy is LWW on `ModifiedOn` only

Update is skipped when:

```text
found.ModifiedOn >= incoming.ModifiedOn && !correction
```

That is last-write-wins by wall clock (or by whoever last set ModifiedOn). There is:

- No vector clock / version counter
- No field-level merge
- No “server always wins” policy switch beyond direction + sanitization
- Clock skew / offline edits can silently drop changes

For many apps (settings, account metadata, admin-owned data) LWW is acceptable. For multi-device concurrent edits of the same entity, **this will lose data without anyone noticing** (no issue raised—just “not newer”).

Be honest about the product domain: if two clients can edit the same row offline, this is not enough.

### 6. Filter API: opt-in by existence, not by intent

```csharp
ShouldSyncRepository => _filters.ContainsKey(typeAssemblyName)
```

So `AddFilter<T>()` with all null predicates means “include everything of type T.” That is powerful and compact, but:

- Naming says “filter,” behavior says “include list + optional predicates”
- Easy to omit a type and get silent non-sync
- Incoming filter polarity (`ShouldFilterIncomingEntity` = true means reject) is easy to reverse mentally

Architecturally fine; **API affordance is footgun-shaped**. A rename or dual API (`IncludeRepository` + `Where`) would reduce mistakes.

### 7. Type-name coupling and AOT fragility

Wire identity is assembly-qualified type names. Converter matching is string equality on model vs entity names. `SyncObject.ToSyncModel()` uses `Type.GetType` + `Activator.CreateInstance`.

That works in full reflection desktop worlds. It is fragile under:

- Trimming / AOT
- Type renames / namespace moves
- Multiple assemblies with similar names

Source generators and `[SourceReflection]` exist elsewhere—sync still leans on classic reflection in hot paths (relationships, converter defaults, model materialization).

### 8. Relationship discovery by reflection convention

`Name` + `NameId` + `NameSyncId` is a strong convention. It avoids mapping tables. Cost:

- Silent failure if naming drifts
- Lookup filters that change identity keys break the mental model (comments already admit this)
- Hierarchy/`ParentSyncId` is a second pattern alongside the first

Convention-over-config is fine if enforced (analyzer/tests). Without that, it is tribal knowledge.

### 9. Exception classification by string matching

FK detection via message contains `"conflicted with the FOREIGN KEY constraint"` is a smell. It works until culture, provider, or EF wording changes. Prefer structured exception types / SQL error numbers where possible.

### 10. Shared mutable `SyncSession` on the manager

One session instance for UI binding is convenient. It is also a concurrency hazard if anything else reads it while a run mutates flags, issues, settings. Mitigated with a result *copy* at the end—good—but the live object is still a race surface for UI and tests.

### 11. Session state as flags for a linear pipeline

`SyncSessionState` is `[Flags]` and states accumulate (`Started | Configuring | Configured | Pulling | …`). For a linear workflow, an exclusive enum (or a small state machine) is clearer. Flags make “what phase am I in?” multi-bit decoding and invite invalid combinations.

Not fatal; just muddier than the rest of the design.

### 12. Inconsistent defaults

- Manager seed: `ItemsPerSyncRequest = 600`
- `SyncSettings.Reset()`: `10000`
- Server clamp: max `10000`

Small, but it signals the settings object has multiple authors over time without a single “defaults of record.”

### 13. Sync types as free-form strings

`supportedSyncTypes` as `string[]` is flexible. It also means no compile-time safety, easy typos, and no structured composition of “what filters belong to Full vs Accounts.” A typed registry (`ISyncProfile`) would scale better as scenarios grow.

---

## Architectural tensions (not bugs, but tradeoffs to own)

| Tension | Current choice | Cost |
|--------|----------------|------|
| Simplicity vs multi-master | LWW + direction | Silent lost updates under concurrent edit |
| Performance vs safety | Batch then individual | Individual path is slow; good for correctness |
| Flexibility vs discoverability | Filters as include list | Silent omissions |
| Generality vs speed | Reflection converters/relationships | Harder AOT, harder to follow |
| UI convenience vs purity | Shared session + dispatcher | Mutation visibility races |
| Protocol completeness vs implementation | Corrections API exists | Default client no-ops |

None of these are automatically wrong. They are choices. The problem is when the *code implies* a richer system than you actually run (corrections, multi-device safety).

---

## Security / trust notes

**Good**

- Server sanitizes page size and permanent delete
- `ValidateSyncClient` hook for allowlisting
- Web path separates transport

**Gaps to be aware of**

- Filters and sync direction are partly client-influenced. `ServerSyncClient.BeginSync` keeps the caller's `SyncDirection` and last-synced stamps and only overwrites page size, `IncludeIssueDetails`, and `PermanentDeletions`. There is no server-side re-home of the window. A hostile client can influence *what window* they claim. Server should re-derive or clamp last-synced from server-side session store if that matters. In-process, those three overwrites hit the same instance the client is still using (open defect 6).
- Authorization beyond “client supported” is not visible in this layer (must live in web pipeline / credentials).
- `IncludeIssueDetails` can leak internals if ever enabled server-side for clients.

---

## What to fix first (priority)

1. **Dequeue on `waitFor` timeout** — an abandoned session id blocks every later run. See open defect 1.
2. **`Sync()` should return the `SyncAsync` result** — today it returns the live session and hides `CouldNotStart`.
3. **Mark the live session `Completed` before `SyncCompleted`** — subscribers currently observe `SyncRunning`.
4. **Assign `ClientNotSupported`** or stop documenting it as the session's issue type.
5. **Set pull `Take` from `ItemsPerSyncRequest`** — pull is capped at the `SyncRequest` default of 1000.
6. **Decide the story for corrections** — implement or demote the protocol noise. The round is also a single page of issues.
7. **Break `WebSyncClient : ServerSyncClient`** — type model currently lies.
8. **Document conflict model as LWW** in product terms; add a version column if better conflict handling is needed.
9. **Rename the filter API** toward include semantics; keep predicates secondary.
10. **Cover the open defects with tests.** Filter omission, relationship order, soft-delete create-then-delete, last-synced only on success, and cancel mid-page already have coverage under `Cornerstone.UnitTests/Sync`. The holes are the queue id after timeout, `Sync()` when the run cannot start, `SyncCompleted` ordering, `ClientNotSupported`, and pull `Take` above 1,000.
11. **Harden type materialization** for AOT if mobile or browser targets matter.

---

## What not to rewrite

Do not throw away:

- Entity / Model / Object separation  
- `SyncId` + soft delete  
- Manager / Session / Client split  
- Batch→individual apply  
- Server sanitization of dangerous options  
- SyncOrder for dependency apply  

That core is sound. The framework needs **tightening and finishing**, not a greenfield redesign.

---

## Bottom line

This is a **client/server entity sync engine with offline-friendly identity, pragmatic apply semantics, and real failure handling**—not a marketing “sync” wrapper.

Where it is wrong or incomplete:

- A timed-out waiter leaves its session id in the queue and blocks later runs.
- `Sync()` can report a different run than the one it started.
- Corrections look real; the default path is empty, and the round is one page.
- Web client inheritance says “server” when it isn’t.
- Conflict handling is weaker than the rest of the system’s sophistication implies.
- Filter-as-include and string type names are power tools that punish small mistakes.

Where it is right:

- Layering, identity, apply pipeline, server distrust of client settings, and the general pull/push orchestration.