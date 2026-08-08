# Sync Framework

Namespace: `Cornerstone.Sync`  
Primary location: `Cornerstone/Sync/`  
Related: `Cornerstone.EntityFramework` (EF adapters), `Cornerstone.Storage` (`ClientSyncEntity`, `DatabaseKeyCache`), `Cornerstone.Settings` (`SettingSyncEntity` / `SettingSyncModel`)

This document is the working reference for the entity sync framework: architecture, data shapes, session lifecycle, and how to **build or update** syncable functionality.  
**Not covered here:** `Cornerstone/FileSystem/Sync/` — that is a separate file-system differ/sync utility, not the entity sync engine.

---

## Purpose

Two-way (or one-way) synchronization of **syncable entities** between a **client** database and a **server** database (or remote API that fronts a server).

Goals:

- Identify rows by a stable **global** `SyncId` (`Guid`), not local primary keys
- Detect changes by `CreatedOn` / `ModifiedOn` windows
- Soft-delete by default (`IsDeleted`); optional permanent delete
- Serialize wire payloads as **SpeedyPack** `SyncObject`s via transfer **`SyncModel`s**
- Filter which repositories and rows participate in a given sync type
- Resolve FK relationships via `*SyncId` properties + `DatabaseKeyCache`
- Recover from batch failures by individual reprocessing and correction rounds

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
│  BeginSync → (Pull) → (Push) → EndSync → issues/corrections      │
│  source.GetChanges  →  destination.ApplyChanges                  │
│  on failure: GetCorrections ↔ ApplyCorrections                   │
└───────────────┬───────────────────────────────┬──────────────────┘
                │                               │
       SyncClient (Client)              SyncClient (Server)
                │                               │
   SyncClientForDatabase              ServerSyncClient / WebSyncClient
                │                               │
   ISyncableDatabaseProvider          local DB or HTTP → api/Sync/*
```

| Layer | Role |
|-------|------|
| **SyncManager** | App-facing entry: start/cancel/wait, settings per sync type, timers, enabled flag |
| **SyncSession** | One run: state machine, pull/push orchestration, issue aggregation, success criteria |
| **SyncClient** | Protocol surface: Begin/End, GetChanges, ApplyChanges, Get/ApplyCorrections |
| **SyncClientForDatabase** | DB-backed client: repositories, converters, batch/individual apply, relationships |
| **ServerSyncClient** | Server-side client: **sanitizes untrusted** settings from the remote peer |
| **WebSyncClient** | HTTP remote peer (`IWebClient` posts to `api/Sync/{action}/{sessionId}`) |
| **ISyncClientProvider** | Factory for client/server `SyncClient` instances + database access |

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
| `Foo` | Navigation (optional at apply time) |
| `FooId` | Local FK |
| `FooSyncId` | Global FK used during sync |

On apply, `SyncClientForDatabase.UpdateLocalRelationships` walks properties assignable to `ISyncEntity`, finds `NameId` + `NameSyncId`, resolves local IDs via `DatabaseKeyCache` or repository `Read(syncId)`, and throws `SyncIssueException` (`RelationshipConstraint`) if a related entity is missing.

**Implication when building types:** declare parent/child entities in **sync order** so parents land before children. Use `ISyncableDatabase.SyncOrder` / `DatabaseSettings.SyncOrder` as `(entity type name, sync model type name)` pairs.

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
| `Sync(type, updateSettings?, waitFor?, postAction?)` | Runs async then `WaitForSyncsToComplete()` |
| `SyncAsync(...)` | Queued `Task.Run`; returns `SyncSession` result |
| `StartSyncCommand` | `RelayCommand` → `SyncAsync(parameter.ToString())` |

Optional `updateSettings` runs while session is **Configuring** (good place to set direction, filters, last-synced stamps for this run).

### Concurrency

- Single active session: queue peeks session id; only the head proceeds.
- If another sync is running and `waitFor` is null → `CouldNotStart`.
- `IsEnabled == false` → `CouldNotStart` + `SyncIssueType.SyncManagerDisabled`.
- Unsupported type → `ConstraintException`.

### Last synced stamps

On **successful** completion, `OnSyncCompleted` dispatches:

```text
UpdateLastSyncedOn(syncType, settings.LastSyncedOnClient, settings.LastSyncedOnServer)
```

Those values are set from each side’s `SyncSessionStart.StartedOn` at end of the run (not “now”), so the next window is consistent with the session boundary.

### Defaults for new settings (`GetOrAddSyncSettings`)

| Setting | Default |
|---------|---------|
| `LastSyncedOnClient/Server` | `DateTime.MinValue` |
| `PermanentDeletions` | `false` |
| `ItemsPerSyncRequest` | `600` (manager default; `SyncSettings.Reset` uses `10000`) |
| `IncludeIssueDetails` | `false` |

**Server overrides matter:** client requests are suggestions; `ServerSyncClient.BeginSync` clamps `ItemsPerSyncRequest` (max 10000), forces `PermanentDeletions = false`, and may reject unsupported clients.

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

### Pull / push process (`Process`)

For a direction:

1. Build `SyncRequest { Since, Until }` from last-synced and session start.
2. Loop: `source.GetChanges` → filter already-applied ids/modified pairs → `destination.ApplyChanges`.
3. Track successes in `exclude` so reverse direction does not re-send the same change.
4. On issues: `GetCorrections` / `ApplyCorrections` both ways (up to `ItemsPerSyncRequest` issues).

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

Repositories only sync if a filter was registered for their type:

```csharp
settings.AddFilter<AccountEntity>(
    outgoingFilter: x => !x.IsDeleted,           // GetChanges
    incomingFilter: x => x.Status != ...,        // ApplyChanges (return true to KEEP)
    lookupFilter: e => x => x.Email == e.Email,  // optional alternate match key
    skipDeletedItemsOnInitialSync: true,
    orderBy: ...
);
```

- **`ShouldSyncRepository`**: true only if a filter exists for that type’s assembly name.  
  **No filter ⇒ repository is excluded entirely.**
- **Outgoing filter**: applied in `GetChanges` queries.
- **Incoming filter**: `ShouldFilterIncomingEntity` — if the compiled predicate fails, entity is skipped with `SyncEntityFiltered`.
- **Lookup filter**: when set, `Read(entity, filter)` uses the custom predicate instead of `SyncId` (and key cache for that path is disabled).
- **`SkipDeletedItemsOnInitialSync`**: when `since == MinValue`, omit soft-deleted rows from outgoing changes.

---

## SyncClient protocol

Abstract `SyncClient` methods:

| Method | Purpose |
|--------|---------|
| `BeginSync(sessionId, settings)` | Bind session; reset stats; `UpdateSyncSettings()`; build converter |
| `EndSync(sessionId)` | Clear session; return statistics |
| `GetChanges(sessionId, request)` | Page of outgoing `SyncObject`s |
| `ApplyChanges(sessionId, changes)` | Apply incoming objects → issues |
| `GetCorrections(sessionId, issues)` | Objects to fix prior issues |
| `ApplyCorrections(...)` | Same as apply with `corrections: true` (can force older → newer updates) |

Subclass hooks:

- `GetConverter()` → `SyncClientConverter` of `SyncObjectConverter`s  
- `UpdateSyncSettings()` → typically add filters for this client’s role/sync type  

### Database client apply pipeline (`SyncClientForDatabase`)

1. Group by `TypeName`, order by `DatabaseProvider.Settings.SyncOrder` if present.
2. Soft-delete mode: process all statuses together. Permanent delete: non-deletes first, then deletes **reversed** order.
3. Batch open one DB context:
   - `MaintainCreatedOn = false`
   - `MaintainModifiedOn = true` only for **server** clients (`this is ServerSyncClient`)
4. Per object: convert incoming → resolve entity (cache / SyncId / lookup) → normalize status → Add / Update / Delete.
5. On batch save failure → reprocess **individually** and map exceptions to `SyncIssueType`.

Update skip rules:

- Update skipped if entity missing or `found.ModifiedOn >= incoming.ModifiedOn` **unless** `correction == true`.

Delete:

- Soft: set `IsDeleted = true` (may add missing entity first so clients learn about deletes).
- Hard (`PermanentDeletions`): `repository.Remove`.

---

## Converters (entity ↔ model ↔ object)

### `SyncObjectConverter<TSyncClient, TSyncModel, TSyncEntity>`

Registered on the client via `GetConverter()`:

```csharp
protected override SyncClientConverter GetConverter() =>
    new SyncClientConverter(
        new SyncObjectConverter<MySyncClient, AccountModel, AccountEntity>(
            fromSyncObject: null,   // default: SyncObject.ToSyncModel()
            fromSyncModel: (c, m, e) => { /* extra mapping */ },
            toSyncModel: (c, e, m) => { /* extra mapping */ },
            toSyncObject: null,     // default: SyncObject.ToSyncObject(model)
            update: (c, src, dest, convert, status) =>
            {
                convert();          // default UpdateWith by status
                return true;        // false = skip save without issue
            }
        ),
        // more converters...
    );
```

| Direction | Flow |
|-----------|------|
| **Outgoing** (GetChanges) | Entity → Model (`UpdateWith` + `SyncOutgoing`) → SyncObject (`TypeName` = model assembly name) |
| **Incoming** (Apply) | SyncObject → Model → Entity (`UpdateWith` + SyncIncoming*) |
| **Update** | Source entity → destination entity using `SyncIncomingAdd` or `SyncIncomingUpdate` |

`SyncClientConverter` picks the first converter that `CanConvertIncoming/Outgoing/Update` matches.

**Type names:** outgoing conversion is keyed by **entity** assembly name; wire `TypeName` is the **model** name. Filters for repositories use **entity** type names.

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
[Updateable(UpdateableAction.EverythingExceptSyncAddAndUpdate, [
    nameof(EmailAddress), nameof(LastLoginDate), nameof(Name), ...
])]
public partial class AccountEntity : SyncEntity<int>, IAccount { ... }
```

That means those properties are **not** updated via default sync incoming add/update unless you also allow them for specific sync actions (or handle them in a custom converter `update` / `fromSyncModel`).

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
- Ordered by `Id` for stable paging

### Provider

`ISyncableDatabaseProvider` / `SyncableDatabaseProvider<T>` / `SyncableDatabaseProvider2<T>` supply short-lived DB instances with shared settings + key cache.

### Database settings relevant to sync

| Setting | Default | Notes |
|---------|---------|-------|
| `MaintainSyncId` | true | Auto-assign SyncId if empty on save |
| `PermanentSyncEntityDeletions` | false | DB-level hard delete policy |
| `MaintainCreatedOn` / `MaintainModifiedOn` | true | During apply, client code forces CreatedOn off; ModifiedOn only for server client |
| `SyncOrder` | null | Entity/model type order for apply |

---

## Server vs client

| Concern | Client | Server (`ServerSyncClient`) |
|---------|--------|-----------------------------|
| Trust settings | Local settings | **Sanitize** remote settings |
| Permanent delete | Optional | Forced `false` for remote sessions |
| Page size | Requested | Capped at 10000 |
| ModifiedOn maintenance | Off during apply | On during apply |
| Key cache optimizations | Used for lookup when safe | Treated as server (`IsServerClient`) |
| Remote transport | `WebSyncClient` implements protocol over HTTP | Host implements `ISyncServerProxy` endpoints |

`WebSyncClient` posts to `{syncUri}/{Method}/{sessionId}` (default `api/Sync`).  
`WebServerSyncClientProvider` builds web clients from `IWebClient` + local provider for DB access when needed.

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
| `ClientNotSupported` | Server rejected client |
| `SyncManagerDisabled` | Manager disabled |

Session is **Successful** only if not cancelled and `SyncIssues` is empty after processing (including correction attempts that may add new issues).

---

## Statistics and profiling

Per side (`StatisticsForClient` / `StatisticsForServer`):

- `Changes` — objects returned from GetChanges  
- `AppliedChanges` / `AppliedCorrections`  
- `Corrections` — (corrections sent)  
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

Register `SyncObjectConverter<MyClient, WidgetModel, WidgetEntity>` in `GetConverter()`.

### 5. Filters in `UpdateSyncSettings` (or manager `updateSettings`)

```csharp
SyncSettings.AddFilter<WidgetEntity>(/* optional predicates */);
```

Without this, the repository will **not** sync.

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
| Change which rows go out | Outgoing expression on filter |
| Reject certain incoming rows | Incoming expression on filter |
| Match on business key not SyncId | `lookupFilter` |
| Map non-matching property names | Converter `fromSyncModel` / `toSyncModel` / `update` |
| Stop a property from overwriting locally | `[UpdateableAction]` exclude sync update |
| Soft vs hard delete | `PermanentDeletions` (client); server forces soft for remote |
| Parent-before-child apply | `SyncOrder` on database/settings |
| One-way only | `SyncDirection` PullDown or PushUp |
| Force apply older data | Corrections path (`ApplyCorrections` sets correction=true) |
| Reduce payload size | `ItemsPerSyncRequest`, filters, packable property list |
| Debug failures | `IncludeIssueDetails = true`, profilers, `IndividualProcessCount` |

---

## Common pitfalls

1. **Forgot `AddFilter`** — repository silently skipped (`ShouldSyncRepository` false).  
2. **Wrong type name in filter** — filters key on assembly name of **entity** type.  
3. **Missing model pack properties** — SpeedyPack won’t round-trip new fields.  
4. **FK without SyncId property** — relationships won’t resolve; children fail with `RelationshipConstraint`.  
5. **Wrong SyncOrder** — children applied before parents.  
6. **Treating local `Id` as shared** — never identity-match on `Id` across devices.  
7. **Reusing SyncId** — breaks identity and merge.  
8. **Assuming client settings win on server** — server sanitizes page size and permanent delete.  
9. **Incoming filter polarity** — `ShouldFilterIncomingEntity` returns true when the entity should **not** be processed (fails the keep predicate).  
10. **Update skipped as “not newer”** — same or older `ModifiedOn` is ignored unless correction.  
11. **Custom lookup + key cache** — cache path disabled when lookup filter present.  
12. **UI session vs response session** — `ProcessSyncSession` returns a **copy** with final state; live manager session is also marked Completed.

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
| `ServerSyncClient.cs` | Trusted server begin |
| `WebSyncClient.cs` / `WebServerSyncClientProvider.cs` | HTTP remote |
| `ISyncClientProvider.cs` / `ISyncServerProxy.cs` | Factories / server contract |
| `SyncEntity.cs` / `SyncModel.cs` / `SyncObject.cs` | Data layers |
| `SyncObjectConverter.cs` / `SyncClientConverter.cs` | Mapping pipeline |
| `SyncableDatabase.cs` / `SyncableDatabaseProvider.cs` / `SyncableRepository.cs` | DB contracts |
| `SyncIssue.cs` / `SyncIssueType.cs` | Failure model |
| `SyncRequest.cs` / `SyncStatistics.cs` / `SyncTimer.cs` | Request paging / metrics |
| `SyncDevice.cs` / `SyncClientDetails.cs` / `SupportedSyncClient.cs` | Device identity helpers |
| `IHierarchySyncItem.cs` | Tree metadata |

EF: `Cornerstone.EntityFramework/EntityFrameworkSyncableDatabase.cs`, `EntityFrameworkSyncableRepository.cs`.  
Sample entity: `Cornerstone.Sample/Models/AccountEntity.cs`.  
Sample model: `Cornerstone.Sample/Sync/Models/AccountSetting.cs`.

---

## Mental model for agent work

When the user asks to **build** sync for a domain type:

1. Entity (+ optional client base) with SyncId/timestamps/IsDeleted  
2. Packable SyncModel with the wire fields  
3. Converter registration on the client  
4. Filter registration for that entity type  
5. SyncOrder if relationships exist  
6. Manager sync-type string and settings  
7. Server trust/API if remote  

When the user asks to **update** sync behavior, prefer changing filters, Updateable attributes, converters, and SyncOrder before rewriting the manager/session pipeline.