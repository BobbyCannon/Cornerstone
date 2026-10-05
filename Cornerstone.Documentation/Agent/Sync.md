# Sync Framework

Namespace: `Cornerstone.Sync`  
Primary location: `Cornerstone/Sync/`  
Related: `Cornerstone.EntityFramework` (EF adapters), [Storage.md](Storage.md) (SQL mapper + `UpsertSync` apply), `Cornerstone.Storage` (`ClientSyncEntity`, `DatabaseKeyCache`), `Cornerstone.Settings` (`SettingSyncEntity` / `SettingSyncModel`)

This document is the working reference for the entity sync framework: architecture, data shapes, session lifecycle, and how to **build or update** syncable functionality.  
**Not covered here:** `Cornerstone/FileSystem/Sync/` — that is a separate file-system differ/sync utility, not the entity sync engine.

---

## Purpose

Two-way (or one-way) synchronization of **syncable entities** between a **client** database and a **server** database (or remote API that fronts a server).

Goals:

- Identify rows by a stable **global** `SyncId` (`Guid`), not local primary keys
- Detect changes when `CreatedOn` **or** `ModifiedOn` is in the session window `[since, until)`
- Soft-delete by default (`IsDeleted`); optional permanent delete
- Serialize wire payloads as **SpeedyPack** `SyncObject`s via transfer **`SyncModel`s**
- Filter which repositories participate. A type syncs only when a filter is registered for it. An empty set syncs nothing (the manager does not pull or push, and the run is not successful). Ownership is `scopeFilter` (ANDed into GetChanges and apply). Incoming/outgoing are travel policy.
- Resolve local FKs in the **converter** (`*SyncId` → `*Id`); missing converter or failed update is a sync issue
- Recover from batch save failures by individual reprocessing. `ApplyCorrections` is ApplyChanges with last-write-wins skipped. Default `GetCorrections` is empty.

---

## Server scale

Server repositories are expected to hold **100 million+** rows. Every query on a server/hub apply or `GetChanges` path must stay **O(page)**, never O(table).

- Look up by unique `SyncId` (index seek) or a bounded time window (`CreatedOn` in `[since, until)` **or** `ModifiedOn` in `[since, until)`), ordered by ModifiedOn then Id, with `Skip`/`Take` across repositories. Do not page by local `Id` or by type name. A row created in the window still goes out this session if `ModifiedOn` is restamped after `Until`. `SyncEntity.ModifiedOn` has `[SqlIndex]` (`IX_{table}_ModifiedOn`). Hosts on EF should add the same index in their entity maps (Sample does). EF migrations are owner-only. Filter `OrderBy` is not used for GetChanges order.
- An apply page is at most `ItemsPerSyncRequest` (server cap 10,000; values `<= 0` become 1). Convert each `SyncObject` once inside `ProcessSyncObject` — do not convert the page first (that was a Speedy-era cost). Before the payload keep-test, `UpdateLocalRelationships` does one indexed `Read(syncId)` per related `*SyncId` (memoized per group) so local `*Id` is set even when that related type has **no** apply keep-test. `RejectIfRelatedIncomingFiltered` is the separate check, and it reads only when that related type `HasApplyKeepTest`. Client `KeyCache` may supply `*Id` when the related type has no apply keep-test; a hub still `Read(syncId)`. Host converters use `FindBySyncId`, not `GetDatabase()` per row.
- Do not `ReadAllKeys()`, unfiltered `ToList()`, or a scope/incoming expression that cannot use an index (that compiles to a table scan). Hosts should index the scope key (and `ModifiedOn`) on hub tables.
- Client-sized caches (`DatabaseKeyCache` warmed from `ReadAllKeys`) are for local databases, not the hub.

If a new server-side query cannot be explained as “this page’s keys” or “this time window + take”, it does not belong in `Cornerstone.Sync`.

---

## Rules you must not break

These are the engine’s working laws. Speedy is the behavioral source of truth for GetChanges, paging, filters-as-allow-list, pull-then-push, and **update** last-write-wins. Keep Cornerstone’s public surface: `Sync(SyncOperation)`, `SyncSession`, Skip/Take paging. Do not reintroduce `AfterId` / `AfterModifiedOn` / `AfterTypeName`. Isolation extras that reject or merge more than Speedy stay.

### Identity

- **`SyncId` is identity.** Never reuse a Guid. Local `Id` is per-database and is not on the wire as identity. Do not copy another machine’s `FooId`.
- Wire `SyncObject.TypeName` is the **model** assembly name. Filter keys and repository `TypeName` are the **entity** assembly name. They are not interchangeable.
- Status on the wire: Deleted if `IsDeleted`, else Added if `CreatedOn == ModifiedOn`, else Updated. Apply remaps: Added+found → Updated; Updated+missing → Added; payload `IsDeleted` → Deleted.

### Session and protocol

- Default direction is **pull then push**. Pull applies server pages first. Objects applied on pull (same `SyncId` **and** `ModifiedOn`) are excluded from push so the same image is not echoed.
- Both `Until` values must be frozen **before** any GetChanges. The session calls `BeginSync` on the client slot only. The server slot begins inside the first `server.Sync()` (`BeginSync` then the page). That is true in-process and over HTTP. Do not add a second server `BeginSync` from the session. The Speedy law is frozen Until, not Begin order.
- Combined GetChanges uses `Since = LastSyncedOn*` and `Until = that side’s session StartedOn`. If `Since == Until`, extend Until to now. Recapturing `UtcNow` per page walks a moving window.
- Empty `Changes` with PullDown is GetChanges. Non-empty `Changes` is ApplyChanges **only** (no GetChanges on that call). Do not apply a client page on the same call that returns server GetChanges. The server session ends only when `EndSession` is set.
- Session id lives on the **body**, not the URL (`POST api/Sync`).
- Successful means not cancelled and `SyncIssues` empty. Manager persists `LastSyncedOn*` only on success, from each side’s `StartedOn`. Failed/cancelled runs must not advance stored stamps. `ProcessSyncSession` still writes those stamps onto the live session Settings in `finally` before success is decided. `OnSyncCompleted` copies them onto the manager’s per-type settings on the completion thread, then raises `SyncCompleted`. That copy is not dispatched. `StartSyncCommand.Refresh` is the dispatcher post. Subscribers run on the completion thread and must not call `IDispatcher.Dispatch`.
- One session at a time on `SyncManager`. `WaitForSyncsToComplete` waits while the queue is non-empty **or** the live session is running.

### Filters and isolation

- **A repository syncs only when a filter is registered for that entity type.** An empty set syncs nothing. `AddFilter` with null predicates still includes that type. Speedy’s empty set syncs every repository. Cornerstone does not. Do not restore that Speedy rule.
- Filters key on **entity** type. Incoming wire TypeName is the **model**.
- **`scopeFilter` is ownership.** GetChanges WHERE is time window AND scope AND outgoing. Apply keep-test is scope AND incoming (`ShouldFilterIncomingEntity` true = skip). Lookup `Where` is lookup AND scope. Related `*SyncId`: `Read(syncId)` then the related type’s apply keep-test. Identity `Read(SyncId)` stays unscoped so a steal cannot look missing and turn into an insert.
- Incoming/outgoing are **travel policy** (write-only: outgoing `false`; catalog: incoming `false`; date/`CanSync` extras on one side). They do not substitute for scope. Incoming-only does **not** hide pull. Outgoing-only does **not** fence apply. There is no “outgoing null → use incoming” fallback in SQL or EF.
- Lookup filter is **this-row identity** (business key instead of SyncId). It does not walk `*SyncId`. Related bind and `GetEntityPrimaryKey` always `Read(syncId)`. Lookup miss then `Read(SyncId)`: in-scope is a merge, out-of-scope is `SyncEntityFiltered`, not a second insert.
- Payload fields can be rewritten to pass scope/incoming. The **stored row cannot**. Test the stored row too (SyncId steal/delete path).
- The engine has no tenant, account, or `CustomerId` types. Hosts close session identity over `scopeFilter` in `SetSyncSettings` (from authentication on a hub). Client filters do not travel on HTTP (`UpdateWith` omits `_filters`; a deserialized `SyncSettings` starts empty). Do not clone `SyncSettings` on the server and drop `_filters`. Sanitize in place (`ServerSyncClient.BeginSync`).
- `skipDeletedItemsOnInitialSync` defaults true on `AddFilter`. Tombstones are omitted only when `since == DateTime.MinValue` and that type is registered. An unregistered type sends nothing.

### Apply

- Convert once inside `ProcessSyncObject`. Do not convert the page first.
- Stamp wire `ModifiedOn` onto the incoming image after convert (default `UpdateWith` on add/update often omits `ModifiedOn`).
- Groups by model TypeName, `SyncOrder` if set. Non-deletes first, then deletes in reverse group order (children before parents). One group, one `SaveChanges`. Batch failure retries one object at a time.
- Converter is required. Missing converter, failed convert, or `update:` returning false is `UpdateException`. Failed `UpdateEntity` must `Discard` so the group save does not persist a rejected copy.
- **Updates** are last-write-wins both ways: skip when stored `ModifiedOn >=` incoming (wire timestamp) unless it is a correction. Ties stay local.
- **Deletes on pull** (client apply): hub tombstone wins even if local `ModifiedOn` is newer. Speedy has no delete LWW.
- **Deletes on push to a hub** (`this is ServerSyncClient`): last-write-wins. Skip when hub `ModifiedOn` is the same or newer and it is not a correction. An older spoke tombstone must not wipe newer hub data from another spoke. A peer in the server slot is **not** `ServerSyncClient` and does not get that skip. `WebSyncClient` inherits `ServerSyncClient` but its `Sync` only POSTs — do not use that type as a local apply client.
- Existing-row delete always `UpdateEntity` (payload copy) then `IsDeleted` / `Remove`. Missing + soft delete: insert, then mark deleted. Permanent delete: `Remove`; missing is a no-op. Hub `ServerSyncClient` forces `PermanentDeletions = false`.
- `MaintainCreatedOn` is off during apply. `MaintainModifiedOn` is on only for `IsServerClient` (`this is ServerSyncClient`). Do not restamp `ModifiedOn` in `ProcessSyncObject` for a storage mapper. Conflict/delete policy lives in apply, not in generated SQL.
- Before the payload keep-test, `UpdateLocalRelationships(enforceMissing: false)` sets local `*Id` from `*SyncId` on the **converted** entity. This read runs even when the related type has no apply keep-test. A missing required id stays at its default and this walk does not throw, so the keep-test can return `SyncEntityFiltered`. Do not make this walk throw: `RejectIfRelatedIncomingFiltered` must still report the incoming SyncObject.
- Added/Updated (not Deleted): reject `RelationshipConstraint` when a related `*SyncId` names a stored row that fails **that type’s** apply keep-test (scope and/or incoming). One indexed `Read(syncId)` only if that related type `HasApplyKeepTest`. Skip the read when there is none. Issue `Id`/`TypeName` pair with the **incoming** SyncObject (model TypeName), not the related entity type. Missing/empty `*SyncId` still passes.
- After a successful copy, `UpdateLocalRelationships` runs again on the **stored** entity. `UpdateWith` does not copy `*Id` (`EverythingExceptSync`). Lookup filters are not used. Keep-test miss is treated as missing: nullable `*Id` nulled; non-nullable → `RelationshipConstraint` (related `SyncId`, related entity type name). Empty `*SyncId` currently clears local `*Id` (Speedy would skip).
- Same-batch new parent then child cannot see the parent `Add` until `SaveChanges` at the end of that type group. The child’s local id stays unset until a later apply of that child runs after the parent is already saved. A client apply keeps the incoming `ModifiedOn`, so the next window does not select that child by itself.
- `ApplyCorrections` is the same pipeline with LWW skipped. Default `GetCorrections` is empty. Session `ApplyIncoming` still uses `ApplyChanges` (LWW on) for server→client corrections — parked Speedy gap.
- `ApplyChanges` / `ApplyCorrections` / `GetChanges` / `GetCorrections` / `EndSync` call `ValidateSession`. `BeginSync` creates the session.

### GetChanges and paging

- A change is `CreatedOn` in `[since, until)` **or** `ModifiedOn` in `[since, until)` (`until` exclusive). Order is ModifiedOn, then local `Id`. Filter `OrderBy` is not used for that order.
- Paging is Skip/Take across included repositories (sync order). Skip is “how many matching changes were already returned,” not a local-Id or type-name cursor. Do **not** page with a `(ModifiedOn, Id)` keyset: it drops rows whose `CreatedOn` is in the window but `ModifiedOn` is before the last page cursor. OFFSET of a **window** is the accepted cost of that rule.
- Count the window once on `Skip == 0` and reuse it. `HasMore` is `(page.Count > 0) && (Skipped + page.Count) < TotalCount`. Pull loops on `HasMore`. Push sets the local request `Take` to `ItemsPerSyncRequest` (`SyncRequest` defaults `Take` to 1000, and `GetChanges` keeps a positive `Take` that does not exceed the page size). The pull request built in `SyncClient.Sync` does not set `Take`, so it stays 1000. A page size above 1000 still pulls at most 1000 rows per call. Push sets `Take` because the short-page check is against `ItemsPerSyncRequest`. A short page is `count < ItemsPerSyncRequest`. `EndSession` on that push call is also `!SyncIssues.Any()` at the moment of the call. A full page does not end the session. An exact full last page still makes one end-only call after the empty local page. Do not stop push on `!HasMore`: that would end the session on a page that filled the page size.
- Do not `ReadAllKeys` on a hub. KeyCache is a client-sized optimization. Null cache is valid. Wrong cached id re-reads by SyncId.

### SQL / EF storage

- SQL live `UpsertSync` writes an existing row only when incoming `ModifiedOn` is **strictly newer**. Do not OR `IsDeleted` into generated upsert SQL. Applied tombstones flush in `SavePending` with `UpsertSync(list, requireNewerModifiedOn: false)` still on SyncId. Do not PK-upsert tombstones (SQLite INSERT omits Id; UNIQUE SyncId fails). Do not add `ISyncableRepository.Tombstone()`.
- SQL apply `Read(SyncId)` attaches INPC so mutations enqueue; GetChanges `Query()` does not attach. A SQL `Read` does not enqueue by itself. Hosts that mutate a loaded row must `Add` then `SaveChanges`.
- EF `SaveChanges` converts `Remove` of a sync entity to a tombstone when `PermanentSyncEntityDeletions` is false (and restamps `ModifiedOn`). SQL `Remove` is still a hard delete. `SqlSyncableDatabase` already implements `IDatabase` via `ISyncableDatabase`. `SqlDatabase` is connection/migrate/`GetRepository<T>`. Do not implement `IQueryable` on SQL until that surface is split off `IRepository`.
- SQLite memory: do not `using GetSyncableDatabase()` as the last connection (`Cache=Shared`). Host converters must not `GetDatabase()` per apply row; use `FindBySyncId`.

### Hosts

- Isolation belongs in `scopeFilter` plus engine enforcement, not hardcoded `CustomerId` or `AccountId` in `Cornerstone.Sync`. Sample `EnsureSameCustomer` is extra policy for **unscoped** sessions (`ApplyAll`) where there is no scope filter.
- Scope/incoming see the converted image after the first `UpdateLocalRelationships`, and before `update:` stamps. A stamp in `update:` or `fromSyncModel` is the wrong layer for a local integer the scope filter compares.
- Sample DI `SampleServerSyncClientProvider` does not pass `getAuthenticatedAccount`; the running Sample app uses `ApplyAll` (unscoped). Tests inject `AuthenticatedAccount`.

### Primary keys do not sync

Sync does not carry local primary keys or local foreign-key integers. Assume every integer `*Id` stays at its default when a sync model is converted into an entity. A row loaded from the database has real keys. An image built from a `SyncObject` does not. `AccountId == 0` on that image is the default, not an owner, and it is not a valid server foreign key.

`SyncId` is on every sync model. It may be `Guid.Empty`, but the property is always there. Relationship identity on the wire is `*SyncId`. Unset means an empty `*SyncId`. A few types may sync a key; do not plan on that.

### Server-forced relationships

A sync model may leave a relationship unset that the server is allowed to fill. The client may not have known it (no session principal yet). The relationship walk only fills a key the packet names. When the model does not carry that key, the incoming value stays unset. Unset is not permission to rewrite every row.

Principals the server may know and the client may not include an authenticated account, a sync device, or a customer. The same rule covers any of them.

The decision is per entity type. For a type that opts in, the session principal and unset are the same owner:

| Relationship on the incoming or stored row | Action |
|--------------------------------------------|--------|
| A different principal (set, and not the session) | Reject. Do not apply. Do not move the row onto the session. |
| Unset | Accept, and assign the session principal on the stored entity. |
| Already the session principal | Accept. Leave that assignment in place. |

Types that have not opted in do not get this treatment. An unset relationship there stays a scope miss or a constraint failure. Do not stamp the session principal onto every synced type.

The scope keep-test runs before `update:` stamps. A scope that requires the session principal rejects unset before that stamp, so the stamp never runs. For an opted-in type, unset has to count as in scope, and the accepted entity still has to be stored under the session principal. Accepting the row without writing the principal leaves it unset for the next sync. Added versus updated does not change the decision: a later edit often arrives as an update with the relationship still unset.

### Local integer scope (`AccountId`)

A client sync and a hub sync scope owned rows with `x => x.AccountId == accountId`. That integer is the GetChanges index on a hub of 100 million+ rows. Do not switch those predicates to `AccountSyncId`. `ClientCredential` has no `AccountId`; its scope stays `AccountSyncId`. The hub credential scopes on `AccountId`.

`AccountId` is `[UpdateableAction(EverythingExceptSync)]`. It is not on the sync model, so incoming `UpdateWith` leaves it at 0. `AccountSyncId` is on the model and is copied. The receiver’s integer is not the sender’s integer. Resolve it from `AccountSyncId` through the normal `*Id` / `*SyncId` pair (`Account` navigation: client `ClientAccount`, hub `AccountEntity`).

`SyncClientForDatabase` must not search for a property named `AccountId` (`CopyAccountId` was rejected). Host `fromSyncModel` must not assign it either. `UpdateExercise` and the other hub `update:` callbacks still assign `AuthenticatedAccount.AccountId` on add, on the **stored** row, after the keep-test. That does not make the keep-test pass.

The first walk (`enforceMissing: false`) fills `AccountId` before `RejectIfIncomingFiltered`. Hub Account scope is `x => x.Id == accountId`, so Account has an apply keep-test. On a hub the walk still reads the related row. `HasApplyKeepTest` only skips the client key-cache shortcut. A missing account stays 0 and the scope filter returns `SyncEntityFiltered`. Sync order saves Account before owned types, and each group saves before the next, so a same-session account is visible. The second walk writes `AccountId` onto the stored row because `UpdateWith` will not.

`BrowserBookmark` scopes on `AccountId`, and its model does not pack `AccountSyncId`. The walk cannot fill it. Do not add `AccountSyncId` to that model unless asked. Its `update:` assignment is still after the keep-test.

`AccountSetting` is opted in to a server-forced account. The model packs `AccountSyncId` and does not pack `AccountId`. Hub scope keeps the row when `AccountId` is this account, or when `AccountSyncId` is empty and `SyncDeviceSyncId` is this session device. Any other row is rejected. `IsAccountSettingInScope` checks `AccountId` first. Outgoing rows are `CanSync` and either this `AccountId` (with a matching device, or no device) or that same empty-`AccountSyncId` device row. On accept, `update:` writes the session account onto the stored row. Deletes return false.

`EmptyFiltersExcludeOutgoingChanges` expects GetChanges to return nothing when no filter is registered. That is the engine rule. Do not restore the old Speedy behavior where an empty set synced every repository.

Parked Speedy follow-ups stay out of the public contract.

---

## Architecture at a glance

```
┌──────────────────────────────────────────────────────────────────┐
│ SyncManager                                                      │
│  - named sync types + per-type SyncSettings / SyncTimer          │
│  - queue (one active session)                                    │
│  - creates/runs work via SyncSession                             │
└────────────────────────────┬─────────────────────────────────────┘
                             │ ProcessSyncSession
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│ SyncSession                                                      │
│  local Begin → pull GetChanges pages → push Apply pages → End    │
│  pull: empty Changes + GetChangesSkip                            │
│  push: Changes only; EndSession closes the server                │
└───────────────┬───────────────────────────────┬──────────────────┘
                │                               │
       SyncClient (Client)              SyncClient (Server)
                │                               │
   SyncClientForDatabase              ServerSyncClient / WebSyncClient
                │                               │
   ISyncableDatabaseProvider          local DB or HTTP → POST api/Sync
```

| Layer | Role |
|-------|------|
| **SyncManager** | App-facing entry: start/cancel/wait, settings per sync type, timers, enabled flag |
| **SyncSession** | One run: state machine, pull/push orchestration, issue aggregation, success criteria |
| **SyncClient** | Public protocol: `Sync(SyncOperation)`. Stepwise Begin/Get/Apply/End are `protected internal` |
| **SyncClientForDatabase** | DB-backed client: repositories, converters, batch/individual apply |
| **ServerSyncClient** | Optional hub: **sanitizes untrusted** settings (page size, permanent delete, tenant). Not required for peer sync. |
| **WebSyncClient** | HTTP remote peer (`IWebClient` posts to `api/Sync`; session id is in the body). Inherits `ServerSyncClient` for the provider surface; `Sync` only POSTs. Do not treat it as a local hub apply client (`IsServerClient` is `this is ServerSyncClient`). |
| **ISyncClientProvider** | Factory for client/server `SyncClient` instances + database access |

### Profiler

`SyncClient.Profiler` (`Cornerstone.Profiling.Profiler`) times named scopes. Use `Profiler.Start(name)` (`TimedScope` ref struct) on hot paths; `Profiler.Time` allocates a delegate.

`SyncSession` owns client and server profilers and passes them into the clients. Sample loopback puts the server profiler on the inner `SampleServerSyncClient` and on `WebSyncClient`. After a Sample TabSync run, `SyncProcessor` snapshots `Name` / `Count` / `TotalTicks` / % of `session.Elapsed`, appends milliseconds to headline series, then `Refresh()` so the next run does not accumulate.

Child scopes nest inside parents. Do not sum rows to 100%. `ProcessSyncObjectsSaveDatabase` is `ISyncableDatabase.SaveChanges` for the type group — that is the number that tests “most of the time is SaveChanges.”

| Name | Where |
|------|--------|
| `Begin` `Pull` `Push` `End` | `SyncSession` on the **client** profiler |
| `WebSync` | `WebSyncClient.Sync` (`Post`) |
| `GetChanges` `GetChangeCount` `GetChangesQuery` `ConvertOutgoing` | outgoing page |
| `ApplyChanges` `ProcessSyncObjects` `ProcessSyncObject` | incoming page |
| `ConvertIncoming` `ProcessSyncObjectReadEntity` `UpdateEntity` `UpdateLocalRelationships` `ProcessSyncObjectAdded` / `Modified` / `Deleted` | per row |
| `ProcessSyncObjectsGetDatabase` `ProcessSyncObjectsSaveDatabase` | open apply DB / group `SaveChanges` |
| `SaveChangesFlushPending` `SaveChangesSavePending` | SQL `SqlSyncableDatabase.SaveChanges` |
| `SaveChangesProcessEntity` `SaveChangesEfCore` | EF `EntityFrameworkDatabase.SaveChanges` |

`ISyncableDatabase.Profiler` is optional. Apply and GetChanges set it on the open database. Null means `Start` is a no-op.

Do not use leftover `SyncClientProfiler` (Timer bag). Live type is `Profiler`.

### Client vs server roles (session slots)

Each session has a **client** slot and a **server** slot. Those are roles for **this run**, not a required hub topology.

- **Peer ↔ peer ↔ peer** is valid. Any peer may sit in the server slot. `WithEachPair` (two `SampleSyncClient`s) is that shape. `WithEachWebPair` uses the same databases with `WebSyncClient` in the server slot.
- Whoever is in the **server slot wins** that session: apply/reject incoming, return outgoing changes, later `ModifiedOn` still compared per row.
- A **single hub** may use `ServerSyncClient` in the server slot (sanitize settings, force tenant from authentication). That is optional. Incoming `SyncSettings` must not choose the tenant **on a hub**. Peers do not need that type.

Do not treat “SQL client / EF server” as a role invert. That is storage. The slot named server is still the winner for that session.

Two isolated networks can each have spokes plus a local hub. Those hubs then sync with each other: one hub in the **server** slot (master), the other in the **client** slot (slave). Spokes never talk across the island; they only sync to their hub. Tests: `SyncTopologyTests` (`TwoIslands*`).

Rejected pushes stay local on the client slot. After each direction, one corrections round-trip runs if there are issues. Default GetCorrections is empty.

---

## Data shapes (three layers)

Always keep these distinct when building features:

| Shape | Base type | Lives | Wire? | Notes |
|-------|-----------|-------|-------|-------|
| **Entity** | `SyncEntity<TKey>` | Client/server DB | No | Local `Id` + global `SyncId`, soft delete, timestamps |
| **Model** | `SyncModel` | Transfer DTO | Yes (packed) | No local PK; packable; used on the wire |
| **Object** | `SyncObject` | Envelope | Yes | `Data` (bytes), `SyncId`, `TypeName`, `Status`, `ModifiedOn` |

### Entity — `SyncEntity<TKey>`

```csharp
// Cornerstone/Sync/SyncEntity.cs
public abstract partial class SyncEntity<TKey> : Entity<TKey>, ISyncEntity
{
    DateTime CreatedOn { get; set; }
    bool IsDeleted { get; set; }
    DateTime ModifiedOn { get; set; }
    Guid SyncId { get; set; }   // unique; never reuse
}
```

Default updateable rules on the base type:

| Property | Updateable actions |
|----------|--------------------|
| `IsDeleted` | `All` |
| `Id` | `EverythingExceptSync` (never synced as content) |
| `CreatedOn`, `ModifiedOn` | `EverythingExceptSyncUpdate` (set on add, not overwritten on update via default rules) |

**Client variant:** `ClientSyncEntity<TKey>` adds `LastClientUpdate` (`EverythingExceptSync`).

**Settings variant:** `SettingSyncEntity<TKey>` / `SettingSyncModel` for key/value settings with `CanSync`, `Category`, `Name`, `Value`, etc.

### Model — `SyncModel`

Transfer model: `CreatedOn`, `IsDeleted`, `ModifiedOn`, `SyncId`.  
Attributes: `[Packable]`, `[Notifiable]`, `[Updateable(All)]`.  
Concrete models (e.g. `AccountSetting`) extend this and list packable property names explicitly.

### Object — `SyncObject`

Wire envelope produced by converters:

- `Data` — SpeedyPack bytes of the model  
- `TypeName` — assembly-qualified type name of the **model** (incoming side of converters)  
- `SyncId`, `ModifiedOn`, `Status` (`Added` / `Updated` / `Deleted`)

`SyncObject.ToSyncObject(SyncModel)` sets status from `IsDeleted` and whether `CreatedOn == ModifiedOn`.

---

## Identity and relationships

### Global identity

- **`SyncId`** is the cross-device identity. Never reuse GUIDs.
- Local **`Id`** is database-specific and is **not** part of the sync payload for identity (excluded from sync updates).

### FK pattern (required for related entities)

Entities that reference other sync entities should expose the convention trio:

| Property | Purpose |
|----------|---------|
| `FooId` | Local FK (not on the wire) |
| `FooSyncId` | Global FK used during sync |
| `Foo` | Optional EF navigation. **Do not** put a settable entity navigation on `[SqlTable]` types — the SQL generator maps every settable property as a column. |

On apply, `SyncClientForDatabase.ProcessSyncObject` converts once, then `UpdateEntity` **requires** `Converter`. `Converter.Update` copies the incoming image onto the destination (and may stamp session fields such as the authenticated account). Before the payload keep-test, `UpdateLocalRelationships` sets each local `*Id` from `*SyncId` on that converted entity (`Read(syncId)`, KeyCache on the client when that related type has no apply keep-test). The engine does not know host property names. A missing parent leaves the integer at its default, so a scope filter that compares it still rejects the row. That first walk does not throw. After convert, Added/Updated objects are rejected (`RelationshipConstraint`) when a related `*SyncId` names a stored row that fails **that type’s** apply keep-test (`HasApplyKeepTest`: scope and/or incoming). One indexed `Read(syncId)` runs only when that related type has a keep-test. The issue `Id` and `TypeName` are the **incoming** `SyncObject` (model type name). After a successful update, **`UpdateLocalRelationships`** runs again on the stored entity and a missing required id is `RelationshipConstraint`. Relationship shapes are cached in a static `ConcurrentDictionary` keyed only by entity type. The first database to build a type supplies `GetSyncableRepositories()` for name resolution for the rest of the process.

Related rows in `UpdateLocalRelationships` are resolved by **indexed `Read(syncId)`** (not lookup filters). On the client, `KeyCache` may supply `*Id` when that related type has no apply keep-test; a hub still reads by `SyncId`. On the post-update walk, a keep-test miss is treated as missing (nullable `*Id` nulled; non-nullable → `RelationshipConstraint`). That issue uses the **related** `SyncId` and the related **entity** type name. The pre-keep-test walk leaves the same miss at the default and does not record it. Empty `*SyncId` currently clears local `*Id`. Use the missing-related path only for a missing related row, or when that type has no keep-test so the earlier reject did not run.

Missing converter, or `Update` returning false → `SyncIssue` (`UpdateException`); the row is not saved. Failed related lookup → `SyncIssueException` (`RelationshipConstraint`). Converter throws `SyncUpdateException` / `SyncIssueException` → recorded as issues; the rest of the batch can continue.

Do not copy another machine’s `FooId`. SQL apply uses `UpsertSync` on `SyncId`: [Storage.md](Storage.md).

**Implication when building types:** declare parent/child entities in **sync order** so parents land before children. Use `ISyncableDatabase.SyncOrder` / `DatabaseSettings.SyncOrder` as `(entity type name, sync model type name)` pairs. Converters do not assign `*Id`. `UpdateLocalRelationships` does, including before the payload keep-test. Session stamps on the stored row still belong in `update:`.

Sample (`Cornerstone.Sample`): SyncOrder is Customer, Address, Account, Bookmark, Setting. Customer is the tenant parent. Address, Account, and Bookmark carry `CustomerId` / `CustomerSyncId` (no SQL navigation). Account also has `AddressId` / `AddressSyncId`. UI: `Tabs/Sync/TabSync.cxaml` (Data → Loopback). The server peer is `WebSyncClient` posting in-process through `SampleWebClient` into `SampleServerSyncClient` (`SampleWebServerSyncClientProvider`). Set `SampleServerSyncClient.AuthenticatedAccount` from server authentication (an `AccountEntity`). Incoming `SyncSettings` cannot choose the tenant. When that account has a `CustomerSyncId`, `ApplyForCustomer` puts ownership on `scopeFilter` (Customer matches `SyncId`; Address, Account, and Bookmark match `CustomerSyncId`). `UpdateLocalRelationships` sets local `*Id` from in-scope related rows. If an account's `AddressSyncId` points at another customer's address, the engine rejects with `RelationshipConstraint` before `update:` (the related address fails that type’s apply keep-test). Sample `EnsureSameCustomer` is extra policy for unscoped sessions (`ApplyAll`) where there is no scope filter. It must resolve related rows with `FindBySyncId` (apply database + related cache). `GetDatabase()` per row opens a new SQLite connection. The running Sample app’s DI provider does not pass an authenticated account, so that path stays `ApplyAll`.

### Hierarchy helper

`IHierarchySyncItem`: `ParentSyncId`, `IsParent`, `Order` — for tree-shaped sync data (used by hierarchy view managers).

---

## SyncManager

Orchestrates named sync types against a client provider and a server provider.

### Construction

```csharp
new SyncManager(
    clientSyncClientProvider,   // ISyncClientProvider
    serverSyncClientProvider,   // ISyncClientProvider (local ServerSyncClient or WebServerSyncClientProvider)
    syncSession,                // shared SyncSession instance for UI state
    runtimeInformation,
    dateTimeProvider,
    dispatcher,
    "Full", "Accounts", ...     // supportedSyncTypes
);
```

Constructor pre-creates `SyncSettings` + `SyncTimer` for each supported type.

### Starting a sync

| API | Behavior |
|-----|----------|
| `Sync(type, updateSettings?, waitFor?, postAction?)` | Starts `SyncAsync`, then `WaitForSyncsToComplete()`, then returns the manager's **live** session. A run that cannot start is not that return value. |
| `SyncAsync(...)` | Queued `Task.Run`; returns that run's `SyncSession`, including `CouldNotStart` |
| `StartSyncCommand` | `RelayCommand` → `SyncAsync(parameter.ToString())` |

Optional `updateSettings` runs while session is **Configuring** (good place to set direction, filters, last-synced stamps for this run).

### Concurrency

- Single active session: queue peeks session id; only the head proceeds.
- If another sync is running and `waitFor` is null → `CouldNotStart` (no enqueue). `SyncAsync` returns that session. `Sync` ignores it, waits for the live run, and returns the manager session.
- `IsEnabled == false` → `CouldNotStart` + `SyncIssueType.SyncManagerDisabled` from `SyncAsync`. `Sync` returns the previous live session.
- Unsupported type → `ConstraintException`.
- `waitFor` that expires before this id is the queue head returns `CouldNotStart` and does **not** dequeue. The id stays. Later runs see a non-empty queue. `ConcurrentSyncWaitTimeoutCannotStart` covers the timeout result and does not start another run afterward.

### Last synced stamps

On **successful** completion, `OnSyncCompleted` writes the stamps on the completion thread, then raises `SyncCompleted`:

```text
UpdateLastSyncedOn(syncType, settings.LastSyncedOnClient, settings.LastSyncedOnServer)
```

Those values are set from each side’s `SyncSessionStart.StartedOn` at end of the run (not “now”), so the next window is consistent with the session boundary. The write is visible to `SyncCompleted` subscribers. Do not post it. A subscriber that reads the dictionary would persist the previous window, and a Keystone processor must not dispatch to catch up. Persist that dictionary only when `session.SyncSuccessful`. `StartSyncCommand.Refresh` is posted and does not run before the event.

### Defaults for new settings (`GetOrAddSyncSettings`)

| Setting | Default |
|---------|---------|
| `LastSyncedOnClient/Server` | `DateTime.MinValue` |
| `PermanentDeletions` | `false` |
| `ItemsPerSyncRequest` | `600` (manager default; `SyncSettings.Reset` uses `10000`) |
| `IncludeIssueDetails` | `false` |

**Server overrides matter:** `ServerSyncClient.BeginSync` mutates the passed `SyncSettings` in place: `PermanentDeletions = false`, `ItemsPerSyncRequest` capped at 10000 and raised to 1 when `<= 0`, `IncludeIssueDetails = false`. Filters and last-synced stamps stay. Do not clone settings and drop `_filters`. `ValidateSyncClient()` may still reject the peer.

---

## SyncSession lifecycle

Flags enum `SyncSessionState` (flags accumulate):

```
Unknown
  → Started
  → Configuring → Configured
  → Beginning
  → Pulling  (if direction has PullDown)
  → Pushing  (if direction has PushUp)
  → Ending
  → Successful? (no cancel + no issues)
  → Completed  (always set in finally path for the live session + response copy)
```

Also: `Cancelled`, `CouldNotStart`.

Derived booleans: `SyncRunning`, `SyncCompleted`, `SyncSuccessful`, `SyncCancelled`, etc.

### Pull / push process

The **wire** protocol is one method: `Sync(SyncOperation)`. Session id is on the operation, not the URL (`POST api/Sync`), so access logs stay one route.

**`SyncOperation`**

| Field | Role |
|-------|------|
| `SessionId` | In the body, never the path |
| `Settings` | First call; server sanitizes |
| `Changes` | Client → server page (empty = idle) |
| `GetChangesSkip` | Server → client page cursor (count already returned) |
| `Issues` | If non-empty, this call is a correction round |
| `EndSession` | Force close (cancel / finally) |
| `ResumeStatistics` | Restore counts on a continued HTTP session |

**`SyncOperationResult`:** `SessionStart`, `Changes`, `AppliedIssues`, `Corrections`, `Statistics`, `SessionEnded`.

Empty `Changes` with PullDown is GetChanges (pull). Non-empty `Changes` is ApplyChanges only (push). The session ends only when `EndSession` is set.

`Sync()` GetChanges uses `Since = LastSyncedOnServer` and **`Until = session StartedOn`**. Recapturing `UtcNow` on every page grew the window while skip walked a moving result set. Rows saved after this session starts land in the next run (`LastSyncedOn` is this `StartedOn`). Local client GetChanges in `SyncSession` uses `clientSession.StartedOn` the same way. If `Since == Until`, GetChanges still extends `Until` to now (empty window).

`SyncSession` follows Speedy pull-then-push:

1. Local client `BeginSync`. The server begins inside the first `server.Sync()` (in-process `BeginSync`, or the HTTP post). The session does not call `server.BeginSync` itself.
2. **Pull:** empty `Changes` → `server.Sync` GetChanges pages → apply locally and record `exclude[SyncId] = ModifiedOn`.
3. Corrections for that direction when issues exist and the server session is still open (`SessionEnded == false`). The last pull page sets `EndSession` only when the client will not push (`ClientHasNoChanges` or pull-only). That skips this round. A pull followed by a push leaves the session open, so pull issues can be corrected before push.
4. **Push:** local `GetChanges`, drop objects already in `exclude` with the same `ModifiedOn`, `server.Sync` apply-only. The local request `Take` is `ItemsPerSyncRequest`.
5. Corrections for that direction when issues exist and `SessionEnded != true`. A short push page sets `EndSession` only when `SyncIssues` is still empty. That decision is made before this page's apply issues are added, so a clean issue list plus a short page skips the round. Issues already on the list keep the session open. A full page, including page size 1, leaves the session open.
6. Local `EndSync`. Server `Sync` with `EndSession` only when that session is still open.

Request shape (`server.Sync` calls, which is one HTTP post for `WebSyncClient`):

- `PullDown`: one call per server page. `Changes` is empty. `ClientHasNoChanges` is true even when the client database has rows (those rows are not pushed). The last page sets `SessionEnded` inside that call. No later end-only call. The session never enters `Pushing`.
- `PushUp`: no pull phase. One call per non-empty client page (`Changes` set, apply only; server `Statistics.Changes` does not increase). A short page (`count < ItemsPerSyncRequest`) sets `EndSession` on that call. A full page does not. The following empty local page does not call the server; `finally` then makes one end-only `Sync`. When the client has nothing that passes the outgoing filters, the server is not called at all.
- `PullDownThenPushUp`: pull pages first. If the client has no outgoing rows, the last pull page ends the server session and push is skipped. If the client has outgoing rows, pull does not end the session. Push then sends pages that are not the echo of an applied `SyncId` + `ModifiedOn`. If every local row was just applied, push sends nothing and the session makes one end-only `Sync` (`EndSession`, empty `Changes`, no GetChanges). A full push page leaves the same end-only call after the empty local page.

An applied server row replaces an older client row before the client queries, so that client image is not pushed. A newer client row is not replaced, then it is pushed. A tie (`ModifiedOn` equal) is not applied and is not pushed: the pull records that `SyncId` and `ModifiedOn`, which matches the client row, so each side keeps its own values. SQL upsert also refuses an update that is not strictly newer.

Progress: `Percent` from `TotalCount` vs skipped count on change pages.

---

## SyncSettings

Per-run / per-type options:

| Property | Meaning |
|----------|---------|
| `SyncType` | Named scenario (“Full”, “Settings”, …) |
| `SyncDirection` | `PullDown`, `PushUp`, or `PullDownThenPushUp` (default) |
| `LastSyncedOnClient` / `LastSyncedOnServer` | Change windows |
| `LastSyncAttemptedOn` | Set when session starts |
| `ItemsPerSyncRequest` | Page size (client request; server may reduce) |
| `PermanentDeletions` | Hard delete vs soft `IsDeleted` |
| `IncludeIssueDetails` | Append exception details to issues |
| `Values` | Extra string bag for custom options |

### Filters (critical for “what syncs”)

A type participates only when `AddFilter` registered it (`ShouldSyncRepository`). `HasFilters` is false when the set is empty. `ShouldExcludeRepository` is then true for every type. After `BeginSync` (hosts add filters in `SetSyncSettings`), `SyncSession` skips pull and push when `HasFilters` is false, records `RepositoryFiltered` (“Sync requires at least one repository filter.”), and does not count the run as successful, so `LastSyncedOn*` is not stored.

```csharp
settings.AddFilter<AccountEntity>(
    scopeFilter: x => x.CustomerSyncId == tenant,  // ownership: pull AND apply
    outgoingFilter: x => !x.IsDeleted,             // extra GetChanges travel
    incomingFilter: x => x.Status != ...,          // extra apply travel (true to KEEP)
    lookupFilter: e => x => x.Email == e.Email,    // optional alternate match key (AND scope)
    skipDeletedItemsOnInitialSync: true,
    orderBy: ...
);
```

- **`HasFilters`**: true when the allow-list has at least one type. False means the manager syncs nothing.
- **`ShouldSyncRepository`**: true only when a filter exists for that type’s assembly name.
- **`HasFilter`**: true only when a filter is registered for that type. Sample `ApplyAll` uses this so a missing type still receives its filter, including Bookmark `OrderBy`.
- **Scope filter**: ownership keep-test. ANDed into GetChanges, apply, lookup, and related `*SyncId`. Identity `Read(SyncId)` stays unscoped.
- **Outgoing filter**: extra travel on `GetChanges`. Incoming-only filters do **not** hide pull.
- **Incoming filter**: extra travel on apply. `ShouldFilterIncomingEntity` — if scope or incoming fails, skip with `SyncEntityFiltered`.
- **Lookup filter**: when set, `Read(entity, filter)` uses the custom predicate AND scope instead of SyncId for **this** incoming row. Related `*SyncId` bind and `GetEntityPrimaryKey` always `Read(syncId)`. On lookup miss, `Read(syncId)` is still used: in-scope becomes an update; out-of-scope is `SyncEntityFiltered`, not an add.
- **`SkipDeletedItemsOnInitialSync`**: defaults **true** when a filter is added. When `since == MinValue`, omit soft-deleted rows from outgoing changes for that registered type.

---

## SyncClient protocol

Public:

| Method | Purpose |
|--------|---------|
| `Sync(SyncOperation)` | One round-trip. `SessionId` and `Settings` on the body. Empty `Changes` = idle. `GetChangesSkip` pages server output. `EndSession` forces close. |

`ISyncServerProxy` is only `Sync`. `WebSyncClient` posts the operation to `api/Sync`.

`protected internal` (used by `Sync` and `SyncSession` locally):

| Method | Purpose |
|--------|---------|
| `BeginSync(sessionId, settings)` | Bind session; reset stats; `SetSyncSettings()`; build converter. Does not `ValidateSession` (it creates it). |
| `EndSync(sessionId)` | `ValidateSession`; clear session; return statistics |
| `GetChanges(sessionId, request)` | `ValidateSession`; page of outgoing `SyncObject`s |
| `ApplyChanges(sessionId, changes)` | `ValidateSession`; apply incoming objects → issues |
| `GetCorrections(sessionId, issues)` | `ValidateSession`; placeholder empty collection. Override only if a client can actually repair issues. |
| `ApplyCorrections(...)` | `ValidateSession`; same as ApplyChanges with last-write-wins skipped. |

Subclass hooks:

- `GetConverter()` → `SyncClientConverter` of `SyncObjectConverter`s  
- `SetSyncSettings()` → typically add filters for this client’s role/sync type  

### Database client apply pipeline (`SyncClientForDatabase`)

1. Group by `TypeName`, order by `DatabaseProvider.Settings.SyncOrder` if present.
2. Non-deletes first, then deletes **reversed** group order (children before parents).
3. Batch open one DB context:
   - `MaintainCreatedOn = false`
   - `MaintainModifiedOn = true` only for **server** clients (`this is ServerSyncClient`)
4. Per object: convert incoming → `UpdateLocalRelationships` on that image (`enforceMissing: false`; missing required `*Id` stays default) → apply keep-test on payload (scope AND incoming) → related keep-test (Added/Updated; indexed `Read(syncId)` only if that type `HasApplyKeepTest`) → resolve entity (cache / SyncId / lookup; lookup miss then `Read(syncId)`) → normalize status → Add / Update / Delete. `UpdateEntity` calls `UpdateLocalRelationships` again on the destination (`enforceMissing: true`). Failed `UpdateEntity` **Discard**s the destination so the group `SaveChanges` does not persist a rejected copy. **Deleted** always runs `UpdateEntity` (payload copy) then sets `IsDeleted` or `Remove`. A missing row with soft delete is inserted first, then updated and marked deleted.
5. On batch save failure → reprocess **individually** and map exceptions to `SyncIssueType`.
6. GetChanges `TotalCount` is the window count (sum of per-repository `GetChangeCount`). Count once on the first page (`Skip == 0`) of that `Since`/`Until` window and reuse it for later pages; do not open a second database just to count. `HasMore` is skip plus this page versus that total.

Update skip rules:

- Update skipped if entity missing or `found.ModifiedOn >= syncObject.ModifiedOn` unless the apply is a **correction**. Compare the **wire** timestamp. Default `UpdateWith` on `SyncIncomingAddOrUpdate` often omits `ModifiedOn`. After convert, apply copies `syncObject.ModifiedOn` onto the incoming image. Outgoing convert forces `CreatedOn` / `ModifiedOn` onto the model before pack. SQL `UpsertSync` requires `incoming.ModifiedOn > stored` for live updates.
- After a successful update or soft delete, destination `ModifiedOn` is set from the incoming image and the entity is queued for `SaveChanges`. SQL `Read` does not enqueue upserts; apply must queue the write.

Delete:

- Soft existing row on a **client** (pull): always `UpdateEntity` (payload copy) then `IsDeleted = true`. The hub tombstone wins even when local `ModifiedOn` is newer. Storage persists the tombstone; apply does not restamp for SQL.
- Soft existing row on a **server** (push): skip when hub `ModifiedOn` is the same or newer and it is not a correction. An older spoke tombstone must not overwrite newer hub data from another spoke.
- Soft missing row: insert, `UpdateEntity`, then `IsDeleted = true`.
- Hard (`PermanentDeletions`): `UpdateEntity` then `repository.Remove`. Missing row is a no-op.
- SQL `UpsertSync` writes an existing live row only when incoming `ModifiedOn` is strictly newer. `SavePending` flushes `IsDeleted` rows with that guard off, still on `SyncId`. Do not restamp in apply. EF converts `Remove` to a tombstone in `SaveChanges` when `PermanentSyncEntityDeletions` is false; SQL `Remove` is still a hard delete. `SqlSyncableDatabase` is already `IDatabase`; `SqlDatabase` is not.

---

## Converters (entity ↔ model ↔ object)

A hub sync client is the template: register `new SyncObjectConverter<..., TModel, TEntity>(update: UpdateX)` only. No `CopyXEntityToModel` / `CopyXModelToEntity`.

### `IUpdateable` (why copies are unnecessary)

Incoming convert does `entity.UpdateWith(model, ...)`. Outgoing convert does `model.UpdateWith(entity, ...)`. That only copies 1:1 fields if the generator emitted `UpdateWith` for the **other** type.

| Type | Implement |
|------|-----------|
| Entity | `IUpdateable<TEntity>`, `IUpdateable<TModel>`, `IUpdateable<IFoo>` |
| Model | `IUpdateable<TModel>`, `IUpdateable<TEntity>`, `IUpdateable<IFoo>` |

Reference: `BrowserBookmarkEntity` / `BrowserBookmark`. Sample Address/Account/Bookmark now follow that. If the model interface does not include `ISyncEntity` (`IAddress` does not), the model **must** implement `IUpdateable<TEntity>` or `IsDeleted` / `SyncId` will not round-trip.

### `update:` / `processUpdate`

`SyncObjectConverter.Update` builds:

```csharp
Action processUpdate = () => destination.UpdateWith(source,
    status == SyncObjectStatus.Added
        ? UpdateableAction.SyncIncomingAdd
        : UpdateableAction.SyncIncomingUpdate);
```

Then `update(client, source, destination, processUpdate, status)`. Switch on **Added / Updated / Deleted**. Call `processUpdate()` — do not assign mapped properties. After that, only:

- Session fields (authenticated account)
- Reject (`return false`)
- True defaults (`Roles` empty → `",,"`)
- Optional extra checks (`GetEntityPrimaryKey` still exists for custom validation)

`UpdateLocalRelationships` sets local `*Id` on the converted entity before the payload keep-test, and again on the stored entity after `update:`. The first walk leaves a missing required id at its default. The second walk reports `RelationshipConstraint`.

### `GetEntityPrimaryKey<T, TKey>(Guid syncId)`

On `SyncClientForDatabase`. KeyCache first; miss → one `Read(syncId)` (never the lookup-filter stub). During apply this uses the open database and the related-row cache. Store in KeyCache; return local `Id`. Returns default when the sync id is empty, the parent row is missing, or no converter can emit that type. Host `update:` callbacks use this for custom validation. Engine `UpdateLocalRelationships` sets `*Id` before the incoming keep-test and again after `update:`. Incoming **filter** is not a copier.

### `SyncRepositoryFilter<T>`

Not a mapper. Independent knobs, keyed by **entity** assembly name. A type without a registered filter is excluded. An empty set excludes every repository.

| Knob | Used by |
|------|---------|
| Scope `Expression<Func<T,bool>>` | Ownership. ANDed into GetChanges, apply keep-test, lookup `Where`, and related `*SyncId` keep-test. Identity `Read(SyncId)` stays unscoped. |
| Outgoing `Expression<Func<T,bool>>` | Travel for `GetChanges` / `GetChangeCount` only (AND with scope). Null outgoing means the whole scoped type. Incoming is **not** a pull fallback. |
| Incoming `Expression<Func<T,bool>>` | Travel for apply, ANDed with scope. `ShouldFilterIncomingEntity` — **true means skip**. Does not hide GetChanges. |
| Lookup `Func<T, Expression<Func<T,bool>>>` | `Read(entity, filter)` instead of SyncId for **this** row only (AND with scope). Not `GetEntityPrimaryKey` / related bind. |
| `OrderBy` | Not used by GetChanges (order is ModifiedOn, then Id). Kept on the filter for hosts that query the same expression elsewhere. |
| `SkipDeletedItemsOnInitialSync` | Outgoing only when `since == DateTime.MinValue` |

Incoming `TypeName` on the wire is the **model**. Filters and `GetEntityPrimaryKey` use the **entity** type. `ShouldFilterIncomingEntity` must look up by entity type (`syncEntity.GetRealType()`), not `syncObject.TypeName`.

### Apply save

`ProcessSyncObjects` saves the **group once**. Do not `SaveChanges` per row. Parent `*Id` is set on the converted image before the keep-test and again on the stored entity after `update:`. `UpdateWith` does not copy `*Id`, so the second walk is the one that is saved (KeyCache / `Read(syncId)` of rows that **already exist** on this side: previous type in SyncOrder, or a prior session). Host `update:` may also call `GetEntityPrimaryKey`. Same-batch new parent+child may leave `ParentId` unset until the parent is already stored.

### Sample

`Cornerstone.Sample`: Address, Account, Bookmark (`IHierarchySyncItem`). GetChanges order is ModifiedOn then Id. `SampleServerSyncClient` / `SampleSyncClient` converters are `update:` only. Tests: `Cornerstone.UnitTests/Sync/`.

---

## UpdateableAction and property control

When building entities/models, control what sync copies with `[Updateable]` / `[UpdateableAction]`:

| Action | When used |
|--------|-----------|
| `SyncIncomingAdd` | New row applied on destination |
| `SyncIncomingUpdate` | Existing row updated |
| `SyncOutgoing` | Building transfer model from entity |
| `EverythingExceptSync` | Local-only (e.g. `Id`, `LastClientUpdate`) |
| `EverythingExceptSyncUpdate` | Allow on add/outgoing but not overwrite on sync update (e.g. `CreatedOn`) |
| `EverythingExceptSyncAddAndUpdate` | Local + non-sync-add/update scenarios |

Example from sample `AccountEntity`:

```csharp
[Updateable(UpdateableAction.All, [
    nameof(AddressSyncId), nameof(EmailAddress), nameof(LastLoginDate), nameof(Name), ...
])]
[Updateable(UpdateableAction.EverythingExceptSync, [nameof(AddressId)])]
public partial class AccountEntity : SyncEntity<int>, IAccount { ... }
```

`All` on the wire fields means `processUpdate()` copies them on incoming add and update. `AddressId` is local-only; the `update` callback sets it from `AddressSyncId` via `GetEntityPrimaryKey`. `EverythingExceptSyncAddAndUpdate` is the opposite: those names are **not** included in `SyncIncomingAdd` / `SyncIncomingUpdate`.

**Rule of thumb when adding properties:**

1. Decide if the property is **local-only**, **server-authoritative**, or **fully bidirectional**.
2. Put the correct `[UpdateableAction]` on the entity (and pack the model property if it goes on the wire).
3. If mapping is not 1:1 name/type, implement converter hooks.

---

## Databases and repositories

### `ISyncableDatabase`

- `GetSyncableRepositories()` — ordered by `SyncOrder` when provided  
- `GetSyncableRepository(Type)` / `GetSyncableRepository<T,TKey>()`  
- `KeyCache` — SyncId → primary key  
- `SyncOrder` — `(entity assembly name, sync model assembly name)[]`

### EF implementation

`EntityFrameworkSyncableDatabase` + `EntityFrameworkSyncableRepository<T,TKey>`:

- Change detection: `CreatedOn` or `ModifiedOn` in `[since, until)`
- Soft-deleted skip on initial sync when filter requests it
- Ordered by `ModifiedOn`, then `Id`

### Provider

`ISyncableDatabaseProvider` / `SyncableDatabaseProvider<T>` / `SyncableDatabaseProvider2<T>` supply short-lived DB instances with shared settings + key cache.

### Database settings relevant to sync

| Setting | Default | Notes |
|---------|---------|-------|
| `MaintainSyncId` | true | Auto-assign SyncId if empty on save |
| `PermanentSyncEntityDeletions` | false | DB-level hard delete policy |
| `MaintainCreatedOn` / `MaintainModifiedOn` | true | During apply, client code forces CreatedOn off; ModifiedOn only for `ServerSyncClient`. SQL `SavePending` also applies these flags using the database `IDateTimeProvider`. |
| `SyncOrder` | null | Entity/model type order for apply |

---

## Server vs client

| Concern | Client | Server (`ServerSyncClient`) |
|---------|--------|-----------------------------|
| Trust settings | Local settings | **Sanitize in place** (keep filters) |
| Permanent delete | Optional | Forced `false` for remote sessions |
| Page size | Requested | Capped at 10000 |
| ModifiedOn maintenance | Off during apply | On during apply |
| Key cache optimizations | Used for lookup when safe | Treated as server (`IsServerClient`) |
| Remote transport | `WebSyncClient` implements protocol over HTTP | Host implements `ISyncServerProxy` endpoints |

`WebSyncClient` posts to `{syncUri}` (default `api/Sync`). Session id is on `SyncOperation`, not the path.  
`WebServerSyncClientProvider` builds web clients from `IWebClient` + local provider for DB access when needed.

---

## Caller identity

Two different shapes share the word “session.”

| Shape | Type | What it is |
|-------|------|------------|
| The run | `SyncSession` : `ISyncSessionStatus` | This pull-then-push. Elapsed, percent, started, stopped, running, cancelled, successful, sync type. AppDispatcher projects `ISyncSessionStatus`. |
| The caller | `ISyncSession` in `IServerSyncSession.cs` | Location plus client details. `SyncDevice` is the packable model (`ISyncDevice` : `ISyncSession`, `ISyncEntity`). |
| The stored run | `IServerSyncSession` | Caller fields plus session id, direction, started, stopped, completed, and issues / settings / statistics stored as strings. A host stores that as its session entity and session-history entity. |

`SyncClient` constructs an empty `SyncDevice` and never fills it. Hosts own the device row (`ClientSyncDevice` : `ClientSyncEntity<int>`, `ISyncDevice`).

### Client details

`SyncClientDetails` / `ISyncClientDetails` : `ISupportedSyncClient` plus `DeviceId`, `DeviceName`, `DevicePlatformVersion`.

`SupportedSyncClient` is the smaller allow-list: `ApplicationName`, `ApplicationVersion`, `DevicePlatform`, `DeviceType`. No device id.

`SyncClientDetailsExtensions` copies the seven client fields onto `SyncSettings.Values` and onto HTTP headers. `Validate` requires all seven (name, version, device id, device name, platform, platform version, device type). Location keys (`Altitude`, `AltitudeReference`, `Latitude`, `Longitude`, `LocationSource`, `LocationUpdatedOn`) are constants on that type. The engine copy helpers do not write them. The hub reads those headers back onto a `SyncDevice`.

Hosts put runtime details on the web client headers with `AddOrUpdateSyncClientDetails(IRuntimeInformation)`. `IRuntimeInformation` implements `ISyncClientDetails`.

Filters do not travel on HTTP. Client-detail values do, because they live in `Values` and headers. A hub must not treat those values as the tenant. Ownership stays on `scopeFilter` closed over authentication.

### Rejecting a caller

`ServerSyncClient.BeginSync` sanitizes page size, `IncludeIssueDetails`, and `PermanentDeletions` on the settings instance it was given, calls `base.BeginSync` (which runs `SetSyncSettings`), then `ValidateSyncClient`. The base returns true. A host returns false to reject the caller. That throws `CornerstoneException` (`BabelKeys.SyncClientNotSupported`). `HandleException` records `SyncIssueType.ClientException` with that message. Nothing assigns `SyncIssueType.ClientNotSupported`.

The session passes one `SyncSettings` instance into the client `BeginSync` and into every `server.Sync`. In-process `ServerSyncClient.BeginSync`, and the Sample loopback (`SampleWebClient` posts the same operation object), mutate that instance. The client slot then sees `PermanentDeletions` false, the page-size cap, and `IncludeIssueDetails` false. An HTTP serializer that copies the body does not do this to the caller's object.

### Hierarchy metadata

`IHierarchySyncItem` : `ParentSyncId`, `IsParent`, `Order`. Bookmarks and hierarchy view managers use it (`HierarchyModelManager`, Sample `IBookmark`). The apply walk does not. Relationships are still `*Id` / `*SyncId` properties from `GetRelationshipConfigurations`.

### `SyncTimes`

`SyncTimes` is the named pair `LastSyncedOnClient` / `LastSyncedOnServer` plus `SyncType`. It is not a session and not a device.

---

## Issues

`SyncIssue`: `Id` (entity SyncId), `IssueType`, `Message`, `TypeName`.

| Type | Typical cause |
|------|----------------|
| `RelationshipConstraint` | Missing related SyncId / FK conflict |
| `ConstraintException` | Unique index / DB constraint |
| `RepositoryFiltered` | Type not in settings filters |
| `SyncEntityFiltered` | Failed incoming filter |
| `UpdateException` | `SyncUpdateException` from custom update |
| `ValidationException` | Validation failed |
| `ClientException` | Unhandled client/session exception |
| `Unauthorized` / `ServiceUnavailable` | HTTP from web client |
| `ClientNotSupported` | Defined for a rejected caller. No code assigns it. `ValidateSyncClient` returning false is recorded as `ClientException` |
| `SyncManagerDisabled` | Manager disabled |

Session is **Successful** only if not cancelled and `SyncIssues` is empty after processing (including correction attempts that may add new issues).

---

## Statistics and profiling

Per side (`StatisticsForClient` / `StatisticsForServer`):

- `Changes` — objects returned from GetChanges  
- `AppliedChanges` / `AppliedCorrections`  
- `Corrections` — unused. Nothing increments it. Correction writes increment `AppliedCorrections`.
- `IndividualProcessCount` — batch failed, fell back to per-item  

`SyncTimer` per type: average duration + successful/cancelled/failed counts.

---

## How to add a new syncable type

Checklist for building new functionality:

### 1. Entity

```csharp
[SourceReflection]
[Notifiable(["*"])]
[Updateable(... appropriate actions for domain props ...)]
public partial class WidgetEntity : SyncEntity<int>  // or ClientSyncEntity
{
    // domain props
    // related: ParentId + ParentSyncId if needed
}
```

### 2. Transfer model

```csharp
[Packable(1, [ nameof(CreatedOn), nameof(IsDeleted), nameof(ModifiedOn),
               nameof(SyncId), nameof(Name), /* ... */ ])]
[SourceReflection]
public partial class WidgetModel : SyncModel
{
    public string Name { get; set; }
}
```

### 3. Database

- Expose `IRepository<WidgetEntity, int>` (or syncable repository) on the DB type that implements `ISyncableDatabase`.
- EF: inherit `EntityFrameworkSyncableDatabase`; detection finds repository properties of `ISyncEntity` types.
- Set `SyncOrder` so dependencies apply first.

### 4. Converter on the sync client

Entity + model: `IUpdateable` both ways (see Converters). Register `new SyncObjectConverter<MyClient, WidgetModel, WidgetEntity>(update: UpdateWidget)` only. In `UpdateWidget`, `processUpdate()` then `GetEntityPrimaryKey` for local FKs. No copy helpers. A scope filter that compares a local `*Id` sees the value `UpdateLocalRelationships` set before the keep-test.

### 5. Filters in `SetSyncSettings` (or manager `updateSettings`)

```csharp
SyncSettings.AddFilter<WidgetEntity>(/* optional predicates */);
```

Register the type with `AddFilter`, or it does not sync. Null predicates still include that type. An empty set makes the manager sync nothing. Ownership goes on `scopeFilter`. Travel limits go on the incoming and outgoing predicates.

### 6. Sync type + manager

- Include the type name string in manager `supportedSyncTypes`.
- Optionally specialize settings in `GetOrAddSyncSettings` or at sync start.

### 7. Server surface

- If remote: ensure API implements `ISyncServerProxy` and server client converters/filters match.
- Override `ValidateSyncClient()` if device/app gating is required.

---

## How to update existing sync behavior

| Goal | Where to change |
|------|-----------------|
| Include/exclude entity type for a sync | `SyncSettings.AddFilter` / remove filter |
| Lock ownership on pull and apply | `scopeFilter` |
| Change which rows go out (travel) | Outgoing expression on filter |
| Reject certain incoming rows (travel) | Incoming expression on filter |
| Match on business key not SyncId | `lookupFilter` (ANDed with scope) |
| Map non-matching property names | Converter `fromSyncModel` / `toSyncModel` / `update` |
| Local `*Id` seen by the incoming keep-test | `UpdateLocalRelationships` before the keep-test |
| Stop a property from overwriting locally | `[UpdateableAction]` exclude sync update |
| Soft vs hard delete | `PermanentDeletions` (client); server forces soft for remote |
| Parent-before-child apply | `SyncOrder` on database/settings |
| One-way only | `SyncDirection` PullDown or PushUp |
| Force apply older data | `ApplyCorrections` skips last-write-wins. Default `GetCorrections` is empty. |
| Reduce payload size | `ItemsPerSyncRequest`, filters, packable property list |
| Debug failures | `IncludeIssueDetails = true`, profilers, `IndividualProcessCount` |

---

## Common pitfalls

1. **No `AddFilter` for a type** — that repository does not sync. An empty filter set skips pull and push and the run is not successful.  
2. **Wrong type name in filter** — filters key on assembly name of **entity** type.  
3. **Missing model pack properties** — SpeedyPack won’t round-trip new fields.  
4. **Assigning mapped fields in `update:`** — bypasses `processUpdate()` / `[Updateable]`. Call `processUpdate()` then only set local `*Id` (or reject).  
4b. **`CopyXEntityToModel`** — wrong. Add `IUpdateable<TModel>` / `IUpdateable<TEntity>`.  
4c. **`SaveChanges` per incoming row** — forbidden. One bulk save per group.  
5. **FK without `GetEntityPrimaryKey`** — `*Id` stays as the sender’s local key. Lookup uses KeyCache then the repository filter.  
5c. **Ownership id stamped in `fromSyncModel` or by naming a host property in the framework** — `UpdateLocalRelationships` sets every local `*Id` from `*SyncId` before the keep-test.  
5b. **Incoming filter keyed by model `TypeName`** — miss; use entity type.  
6. **Wrong SyncOrder** — children applied before parents.  
7. **Treating local `Id` as shared** — never identity-match on `Id` across devices.  
8. **Reusing SyncId** — breaks identity and merge.  
9. **Assuming client settings win on a hub** — `ServerSyncClient` sanitizes page size and permanent delete. Peer-as-server does not.  
10. **Incoming filter polarity** — `ShouldFilterIncomingEntity` returns true when the entity should **not** be processed (fails the keep predicate).  
11. **Update skipped as “not newer”** — same or older `ModifiedOn` is ignored. Compare `syncObject.ModifiedOn`, not the converted entity’s default timestamp.  
12. **Custom lookup + key cache** — cache path disabled when lookup filter present.  
13. **UI session vs response session** — `ProcessSyncSession` returns a **copy** already marked Completed. `OnSyncCompleted` is invoked with the **live** session before that live session is marked Completed, so `SyncRunning` is still true during `SyncCompleted`. The live session is marked Completed after the event returns.  
14. **SQL `Where` parameter indexes** — `PredicateToSqlVisitor` must honor the start index. A date window plus an outgoing filter used to bind both clauses to `@p0`.  
15. **SQL upsert `ModifiedOn` guard** — `ON CONFLICT(SyncId) … WHERE incoming > stored`. Do not restamp in `ProcessSyncObject` for SQL. EF `SaveChanges` tombstones `Remove` when `PermanentSyncEntityDeletions` is false; SQL `Remove` is still hard delete. `SqlDatabase` is not `IDatabase`; `SqlSyncableDatabase` already is.  
16. **Mutate after `Add`+`Save`** — pending list is cleared. `Read` does **not** re-queue. `Add` the mutated entity (apply already does this after update/soft-delete) or the next `SaveChanges` writes nothing.  
17. **Settable entity navigation on `[SqlTable]`** — generator maps it as a column and throws (`AddressEntity` as a SQL type). Use `*Id` / `*SyncId` only.  
18. **Cloning `SyncSettings` on the server** — dropping `_filters` loses scope / lookup / skip-deleted / travel predicates. Sanitize fields in place.  
19. **`using GetSyncableDatabase()`** — SQLite memory needs a keep-alive connection (`Cache=Shared`). Disposing the last connection drops the DB. Host converters must not `GetDatabase()` per apply row; use `FindBySyncId` so related lookups share the apply connection.  
20. **Lookup on pull vs push** — a pull of a same-email different-`SyncId` row can add a second local row; push-only is the reliable lookup test. Lookup miss on apply now `Read(syncId)` and updates when that row is in scope.  
21. **Pull then push** — Speedy order. Pull first, exclude `SyncId`+`ModifiedOn`, then push. Do not apply a client page in the same `Sync()` that returns server GetChanges.  
22. **Hub vs peer** — `ServerSyncClient` is only for an untrusted hub. Peer sync uses a normal `SyncClient` in the server slot; that peer still wins the session.
23. **Server table size** — hub repositories are 100 million+ rows. No `ReadAllKeys` on the server, no filter that scans the table, no convert-the-page-then-apply pass. GetChanges is CreatedOn-or-ModifiedOn in the window, ordered by ModifiedOn then Id, paged with `Skip`/`Take` like Speedy. Do not resume with local `Id` or entity type name. Index `ModifiedOn` (`[SqlIndex]` on `SyncEntity`; EF maps must add `HasIndex` — Sample does). Related keep-tests are indexed `Read(syncId)` after convert, and only when that type `HasApplyKeepTest`.
24. **Scope vs travel** — `scopeFilter` fences pull and apply. Outgoing-only still leaves apply open. Incoming-only still leaves pull open. Write-only is scope + outgoing `false`. Catalog is incoming `false` (often no scope).
25. **Lookup filter is this-row identity** — do not resolve `*SyncId` / `GetEntityPrimaryKey` with a stub that only has `SyncId` set.
26. **Failed update must Discard** — SQL `Read` attaches INPC; a false `UpdateEntity` still queued an upsert until Discard.
27. **Delete copies payload** — existing-row delete runs `UpdateEntity` then sets `IsDeleted`. Client pull does not last-write-wins on deletes (hub wins). Server push still skips when hub `ModifiedOn` is newer so an older spoke tombstone cannot wipe another spoke’s hub row.
28. **Hook name is `SetSyncSettings`** — there is no `UpdateSyncSettings` method. `BeginSync` calls `SetSyncSettings`.
29. **`Sync()` is not the run result** — it returns the live manager session after the wait. `SyncAsync` returns `CouldNotStart` when the manager is disabled or when `waitFor` is null and a run is already active.
30. **Timed-out `waitFor` leaks the queue id** — `StartSyncSession` returning null does not `TryDequeue`. The abandoned id becomes the head after the run ahead of it finishes, and every later start sees a non-empty queue.
31. **`SyncCompleted` vs `SyncRunning`** — the event runs before the live session gains `Completed`. Subscribers that treat the event as “no longer running” are early.
32. **`ClientNotSupported` is unassigned** — rejection is `ClientException`. Do not switch callers to the unused value until a writer exists.
33. **Pull `Take` stays 1000** — `SyncClient.Sync` does not set `Take`. Do not describe pull page size as `ItemsPerSyncRequest` when that value is above 1000.
34. **Shared `SyncSettings`** — hub sanitization writes the session object. Sample loopback posts that same object. Do not assume the client keeps `PermanentDeletions` or `IncludeIssueDetails` after the first server `Sync`.
35. **Relationship map is process-static** — `_relationshipCache` is keyed only by entity type and closed over the first database’s repository list. A second database in the same process does not rebuild it.
36. **Exclude map bails out on issue count** — `ApplyIncoming` skips the exclude update when `failed.Count >= filtered.Count`. A second-walk relationship failure adds the related-id issues and a wrapper whose `Id` is the incoming `SyncId`, so one failed object often counts as two issues. That can skip exclude for siblings on the same page that did apply, and those siblings can be pushed again in this run. When the loop does run, the wrapper id keeps the failed object out of the exclude map. The run stays unsuccessful while any issue remains.
37. **Batch failure keeps earlier issues** — `ProcessSyncObjects` records per-object issues, then `SaveChanges`. On a save failure the individual pass runs the whole group again and can add those issues a second time.

---

## File map (`Cornerstone/Sync`)

| File | Responsibility |
|------|----------------|
| `SyncManager.cs` | Queue, start/stop, settings/timers, events |
| `SyncSession.cs` | Run orchestration, pull/push, issues |
| `SyncSessionState.cs` / `SyncSessionStart.cs` | State flags / begin payload |
| `SyncSettings.cs` / `SyncRepositoryFilter.cs` | Options + filters |
| `SyncDirection.cs` | Pull/push flags |
| `SyncClient.cs` | Abstract protocol |
| `SyncClientForDatabase.cs` | DB get/apply pipeline |
| `ServerSyncClient.cs` | Sanitize settings in place; `ValidateSyncClient` |
| `WebSyncClient.cs` / `WebServerSyncClientProvider.cs` | HTTP remote |
| `ISyncClientProvider.cs` / `ISyncServerProxy.cs` | Factories / server contract |
| `SyncEntity.cs` / `SyncModel.cs` / `SyncObject.cs` | Data layers |
| `SyncObjectConverter.cs` / `SyncClientConverter.cs` | Mapping pipeline |
| `SyncableDatabase.cs` / `SyncableDatabaseProvider.cs` / `SyncableRepository.cs` | DB contracts |
| `SyncIssue.cs` / `SyncIssueType.cs` | Failure model |
| `SyncOperation.cs` / `SyncOperationResult.cs` | Combined wire request / result (`SessionId` in body) |
| `SyncRequest.cs` / `SyncStatistics.cs` / `SyncTimer.cs` | Request paging / metrics |
| `SyncDevice.cs` / `SyncClientDetails.cs` / `SyncClientDetailsExtensions.cs` / `SupportedSyncClient.cs` | Caller identity, header and `Values` copy (see Caller identity) |
| `SyncClientSettings.cs` | Per-client options (`IsServerClient`, primary-key cache flag). Separate from `SyncSettings`. |
| `SyncClientStub.cs` | DI stand-in. `DependencyProvider` registers it as `SyncClient`. |
| `SyncClientProfiler.cs` | Legacy timer bag. Live timing is `Profiler`. Do not wire this type back in. |
| `SyncTimes.cs` | `LastSyncedOnClient` / `LastSyncedOnServer` pair plus `SyncType`. |
| `ISyncSessionStatus.cs` / `IServerSyncSession.cs` | Run status vs stored server log shape |
| `SyncException.cs` / `SyncUpdateException.cs` / `SyncIsssueException.cs` | Exception bases. The issue type’s **file** name has three s characters; the class is `SyncIssueException`. |
| `SyncObjectStatus.cs` / `SyncObjectExtensions.cs` / `SyncExtensions.cs` / `SyncModelView.cs` | Status enum, empty object, small helpers, view |
| `SyncSerializerContext.cs` | Source-generated serializer context for the wire types |
| `IHierarchySyncItem.cs` | Tree metadata |

EF: `Cornerstone.EntityFramework/EntityFrameworkSyncableDatabase.cs`, `EntityFrameworkSyncableRepository.cs`.  
SQL apply: `Storage/Sql/SqlSyncableRepository.cs` (`SavePending` + batched `UpsertSync` / `Delete`).  
Sample loopback: `Cornerstone.Sample/Sync/SampleWebClient.cs`, `SampleWebServerSyncClientProvider.cs` (`WebSyncClient` posts in-process into `SampleServerSyncClient`; no JSON).  
Sample: `Cornerstone.Sample/Models/AddressEntity.cs`, `AccountEntity.cs`; converters in `SampleSyncClient.cs` / `SampleServerSyncClient.cs`; loopback UI `Tabs/Sync/TabSync.cxaml`.  
Tests: `Cornerstone.UnitTests/Sync/` (`SyncEngineScenarioTests`, `SyncClientFilterTests`, `SyncScenarioTest`).

---

## Mental model for agent work

When the user asks to **build** sync for a domain type:

1. Entity (+ optional client base) with SyncId/timestamps/IsDeleted  
2. Packable SyncModel with the wire fields  
3. `IUpdateable` both ways; converter `update:` only (`processUpdate` + `GetEntityPrimaryKey`)  
4. Filter registration for that entity type (`AddFilter`)  
5. SyncOrder if relationships exist  
6. Manager sync-type string and settings  
7. Server trust/API if remote  

When the user asks to **update** sync behavior, prefer changing filters, Updateable attributes, converters, and SyncOrder before rewriting the manager/session pipeline.