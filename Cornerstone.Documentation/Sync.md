# Entity sync

Cornerstone can keep **syncable entities** aligned between a local client database and a server (another database, or a web API in front of one). Rows are identified by a stable global `SyncId` (`Guid`), not by local primary keys. A change is a row whose `CreatedOn` or `ModifiedOn` falls in the session window. Deletes are soft (`IsDeleted`) unless a client asks for permanent delete — a **server** client refuses that and always soft-deletes.

A server database is expected to hold **100 million or more** rows per repository. Sync never loads a whole table. Each request works one page (`ItemsPerSyncRequest`, at most 10,000 on the server; zero or negative is raised to 1) and finds rows by unique `SyncId` or a time window (`CreatedOn` or `ModifiedOn` in range) ordered by `ModifiedOn` (then `Id` when two rows share a timestamp). The next page uses `Skip` (how many changes were already returned), not a local primary key or type-name cursor. Sync entities index `ModifiedOn` for that window. “Are there more pages?” is skip plus this page versus the window count.

This is the entity engine under `Cornerstone.Sync`. File-system differ code under `Cornerstone/FileSystem/Sync/` is a different utility.

## What a host does

The app owns a `SyncManager` with named types (for example `SyncAll`). Each type has `SyncSettings` (direction, last-synced stamps, page size, filters).

- `Sync(type)` — start the run and wait, then return the manager's live session
- `SyncAsync(type)` — queue; only one session runs at a time. The task is that run's result, including a run that could not start
- `CancelSync` / `StopSync` — abort the live session

Optional `updateSettings` runs while the session is configuring: set direction, last-synced times, and which repositories participate. A repository syncs only when a filter is registered for it. With no filters, the manager does not pull or push, the run is not successful, and last-synced stamps stay where they are. Pull uses the outgoing predicate only; an incoming-only filter does not hide rows on GetChanges.

On success, last-synced stamps are taken from each side’s session start time, so the next run’s window matches that boundary. GetChanges for a session uses that start as `Until` (not “now” on every page), so paging does not walk a moving window. Rows saved after the session starts are picked up on the next successful run.

Start sync from `StartLifecycle` (or later), not from Initialize or Load.

## One request on the wire

The public protocol is a single `Sync(SyncOperation)` call. Session id is in the **body**, not the URL, so access logs stay one route (`POST api/Sync`).

A session **pulls** first (empty `Changes`, server GetChanges pages), then **pushes** (client pages applied with no GetChanges on that call). Echo of the same `SyncId` and `ModifiedOn` is dropped on the push. The server session ends when `EndSession` is set.

Pull only does not push. Each page of server changes is one call. The last of those pages ends the server session, because the client will not push. The session does not make another call only to end. The client may still have local rows; they stay local.

Push only does not ask the server for changes. When the client has nothing to send, the server is not called. One short page of client changes is one call. That call ends the server session when the run has no issues yet. Issues already recorded leave the session open. A page that fills the page size does not end the session, because another local page may follow. After that next local page is empty, one more call ends the session and still does not ask the server for changes.

On a normal sync the server rows are applied on the client before the client looks for its own changes. A row that was just applied is not sent back. A newer row on the client is kept and then pushed. An older row on the client is replaced by the server row. The same timestamp stays on each side and is not pushed.

If either side has more than a page, the same endpoint is called again with `GetChangesSkip` (and further client pages). Corrections use the same method with `Issues` set. Cancel ends the server with `EndSession`.

`WebSyncClient` posts that payload. A host API implements `ISyncServerProxy` with only `Sync`. Stepwise Begin / GetChanges / Apply / End still exist as `protected internal` building blocks on `SyncClient`.

## Data on the wire

Three layers stay distinct:

| Layer | Role |
|-------|------|
| Entity (`SyncEntity<TKey>`) | Stored row: local `Id`, global `SyncId`, timestamps, `IsDeleted` |
| Model (`SyncModel`) | Packable DTO; no local PK |
| Object (`SyncObject`) | Envelope: packed bytes, `SyncId`, model type name, `ModifiedOn`, Added/Updated/Deleted |

Related rows use `FooId` (local) plus `FooSyncId` (global). Apply sets each local `FooId` from `FooSyncId` before the keep-test, and again on the stored row after the copy. Parents are applied before children (`SyncOrder`). Do not treat another machine’s local `Id` as identity.

## Rules

These are the engine’s working rules. A session has a **client** slot and a **server** slot. Those are roles for this run. A hub uses a server client in the server slot; two peers can use ordinary clients in both slots.

### Session

Default direction is **pull** from the server, then **push** from the client. One-way pull or push is a settings flag.

The client begins during Beginning, before any page is read, so the client `Until` is frozen at that start. The server begins inside the first `Sync` call, in-process or over HTTP, and that call freezes the server `Until` before it reads. Pull pages of server changes are applied locally first. Objects applied on pull (same `SyncId` and `ModifiedOn`) are not echoed on push. Then the client’s changes are applied on the server. A corrections round runs only while the server session is still open and the run already has issues. The last pull page ends the session inside that call when the client will not push, so issues from that page do not get a corrections round. A pull that will be followed by a push leaves the session open. A short push page sets `EndSession` when the issue list is still empty. That decision happens before the page’s own apply issues are known. Issues already on the list keep the session open, which is when the push corrections round can run. A full push page leaves the session open. Default corrections are empty.

The wire is one `Sync` call. Empty changes with pull means “give me a page.” Non-empty changes means apply only. The server session ends when `EndSession` is set. Do not apply a client page on the same call that returns server GetChanges.

On **success** (not cancelled, no remaining issues), the manager stores last-synced stamps from each side’s session start. The session settings object is given those start times at the end of every run that actually began, including a failed or cancelled run. The manager leaves its stored stamps alone unless the run succeeded. Rows saved after the session starts land in the next successful run.

### Identity and payload

Rows match by global `SyncId` (`Guid`). Never reuse a `SyncId`. Local primary keys are per database and are not identity.

The wire carries a packed **model** inside a **sync object** (`SyncId`, model type name, `ModifiedOn`, Added / Updated / Deleted). Stored rows are **entities**. Status is Deleted if `IsDeleted`, else Added when `CreatedOn` equals `ModifiedOn`, else Updated. Apply may rewrite status: a found row turns Added into Updated; a missing row turns Updated into Added; `IsDeleted` on the payload forces Deleted.

Related rows use `FooId` (local, not identity) plus `FooSyncId` (global). Do not copy another machine’s local `FooId`.

`DatabaseKeyCache` on the provider and open database is optional. Null means do not cache local primary keys; lookups go to the store by `SyncId` (or a lookup filter). A cache is a client-sized optimization, not required for correctness. Apply still works when the cache is missing, and when a cached id is wrong it re-reads by `SyncId`. Do not use a warmed key cache on a hub.

### Change window and paging

A change is a row whose `CreatedOn` **or** `ModifiedOn` falls in `[since, until)` (`until` exclusive). Order is `ModifiedOn`, then local `Id` when two rows share a timestamp. Filter `OrderBy` is not used for that order.

GetChanges walks included repositories in sync order, subtracts remaining skip from earlier repositories, then takes a page. “Are there more?” is skip plus this page versus the window count. Pull follows that flag. Push continues until a local page is shorter than the session page size.

A server database is expected to hold 100 million or more rows. Lookups are by unique `SyncId` or this bounded window. Sync never loads a whole table. Page size is `ItemsPerSyncRequest` (manager default 600; a hub caps at 10,000 and raises zero or negative to 1). Push asks for that many rows. A pull leaves `Take` at the request default of 1,000, and the reader keeps a positive `Take` that fits in the page size, so a configured page above 1,000 still returns at most 1,000 rows on pull.

If `Since` equals `Until`, GetChanges extends `Until` to now so the window is not empty.

### Filters

A repository syncs only when a filter is registered for that entity type. An empty set syncs nothing. `AddFilter` with no predicates still includes that type. Hosts usually register the list in `SetSyncSettings` while the session is beginning. The manager checks after that. If the list is still empty, it records a repository-filtered issue and does not pull or push.

`scopeFilter` is ownership: it is ANDed into GetChanges and into apply. Outgoing predicates are extra travel limits on GetChanges. Incoming-only predicates do **not** hide pull. Incoming predicates run on apply with scope: the keep test fails → `SyncEntityFiltered`. Lookup predicates find **this** incoming row by a business key instead of `SyncId` (ANDed with scope). Related `*SyncId` bind always reads by `SyncId`, never by lookup, then the related type’s scope/incoming keep-test.

On first sync (`since` is `DateTime.MinValue`), a registered filter defaults to omitting tombstones from outgoing changes. After that, tombstones for that type go out so the other side can delete. A type that is not registered sends nothing.

Filters key on the **entity** type. Wire type name is the **model**.

### Apply

Apply groups by model type name, using database sync order when set. Non-deletes run first, then deletes in reverse group order (children before parents). Each group saves once; a failed group is retried one object at a time. A converter is required.

Per object: convert once, copy the wire `ModifiedOn` onto the incoming image, then reject if the type is not in the session. Then set each local `*Id` from `*SyncId`. The keep-test reads that entity, so a scope filter that compares a local integer sees the id on this side. A missing related row leaves the integer at 0 and the keep-test rejects the row. Then reject if the **payload** fails scope and incoming, then (for add/update) reject if a related `*SyncId` names a stored row that fails **that type’s** scope/incoming keep-test. Then find the stored row (optional key cache / `SyncId` / lookup; lookup miss then `Read(SyncId)`). A rewritten payload that passes the keep-test must not update a stored row that fails it — the stored row is tested too. Lookup miss plus an in-scope `SyncId` row is a merge, not a second insert.

Failed update discards the destination so the group save does not persist a rejected copy. A missing converter, failed convert, or converter that refuses the update is `UpdateException`.

After a successful copy, the relationship walk runs again on the stored row. The update does not copy local ids, so the saved row gets them from that second walk. A missing required id then fails. Same-batch new parent then child may leave the child’s local id unset. A later apply of that child can bind it once the parent is already saved. The next sync does not select that child again on its own when the client kept the incoming ModifiedOn.

During apply, created-on maintenance is off. Modified-on restamp is **on** only for a hub server client. Clients keep the incoming `ModifiedOn`.

### Conflicts

**Updates** are last-write-wins on both slots. Incoming update is skipped when the stored `ModifiedOn` is the same or newer, unless it is a correction. Ties stay local. Compare the **wire** timestamp.

**Deletes on pull** (apply to the client): the incoming tombstone wins even if the local row is newer. A hub delete must land on the spoke.

**Deletes on push to a hub** (apply on `ServerSyncClient`): last-write-wins. Skip when the hub `ModifiedOn` is the same or newer and it is not a correction. An older spoke tombstone must not overwrite newer hub data written by another spoke.

A peer sitting in the server slot is not a hub server client, so that peer does not get the hub delete skip.

Deletes copy the incoming payload, then set `IsDeleted`. Permanent delete removes the row instead; a missing row is a no-op. A hub server client forces soft delete (`PermanentDeletions` false), caps the page size, and clears issue details on the settings object for the run. An in-process server, including the Sample loopback, shares that object with the client, so the client half of the run sees the same flag, cap, and cleared details. A missing row with soft delete is inserted, then marked deleted. Storage persists that tombstone; apply does not restamp for a particular mapper.

Corrections are the same apply path with last-write-wins skipped (updates and hub deletes). Default GetCorrections is empty.

### Relationships and isolation

Add/update is rejected (`RelationshipConstraint`) when `*SyncId` points at a stored related row that fails that related type’s scope/incoming keep-test. Missing or empty `*SyncId` still passes (not synced yet, or nullable). After a successful copy, a related row that is missing — or treated as missing because it fails the keep-test — leaves a nullable local id null, or fails if that id is required.

Isolation is `scopeFilter`. Incoming and outgoing are travel policy. The engine has no tenant or account types.

### SQL writes

SQL apply upserts live rows on `SyncId` only when incoming `ModifiedOn` is strictly newer. An applied tombstone (`IsDeleted`) is flushed on `SaveChanges` without that age guard, still keyed by `SyncId`. After a successful apply, destination `ModifiedOn` comes from the incoming image and the row is queued for save. A SQL read does not mark the entity dirty by itself. A rejected update is dropped from the pending set.

EF `SaveChanges` converts `Remove` of a sync entity to a tombstone when `PermanentSyncEntityDeletions` is false (and restamps `ModifiedOn`). SQL `Remove` is still a hard delete. `SqlSyncableDatabase` already implements `IDatabase`; `SqlDatabase` is the connection and migrate host.

An older **non-delete** correction can still lose at SQL even when apply skipped last-write-wins.

## Who is calling

The in-memory run is a `SyncSession`. It is what the screen binds: elapsed time, percent, running, cancelled, successful, and the sync type.

The caller is a separate record: application name and version, device id and name, platform, platform version, and device type. Hosts copy those seven fields onto the sync settings values and onto HTTP headers. All seven are required when the details are validated. Location (altitude, latitude, longitude, source, and when it was updated) can ride along on the headers. The website reads it onto a `SyncDevice`. The engine’s copy helpers do not write location.

`SyncDevice` is the packable model of that caller, including location. A stored device row is a host entity such as `ClientSyncDevice`. Each `SyncClient` starts with an empty `SyncDevice`. The engine does not fill it.

A smaller allow-list, `SupportedSyncClient`, is application name, application version, platform, and device type, with no device id. After a hub begins a session it can reject the caller. The base check accepts everyone. A host that returns false throws. The session records `ClientException` with the not-supported message. `SyncIssueType.ClientNotSupported` has no writer.

A hub can also store each run: session id, direction, started, stopped, completed, and the issues, settings, and statistics as text, together with the same caller fields. That stored row is the server’s log of a run. A host keeps it as its own session entity and session-history entity.

Tree rows can carry `ParentSyncId`, `IsParent`, and `Order` (`IHierarchySyncItem`). Bookmarks and hierarchy views use that. Apply still binds relationships through `FooId` and `FooSyncId`.

## Sample and tests

`Cornerstone.Sample` syncs `Address` then `Account` (account holds `AddressId` / `AddressSyncId`). The Sample **Data → Loopback** tab (`Tabs/Sync/TabSync.cxaml`) runs two in-memory SQLite databases in the same process. The client is a database sync client; the server peer is `WebSyncClient` over an in-process loopback into `SampleServerSyncClient`. Add an address and account on the client, then sync to copy them to the server. The tab can show profiler charts and a last-run breakdown of where that sync spent time. Scenario tests live in `Cornerstone.UnitTests/Sync` and go through `SyncManager`.
