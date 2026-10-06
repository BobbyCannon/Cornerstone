# Storage and SQL mapper

Namespace: `Cornerstone.Storage` / `Cornerstone.Storage.Sql`  
Primary location: `Cornerstone/Storage/`  
Related: `Cornerstone.EntityFramework` (full ORM + sync adapters), `Cornerstone.Generators` (`SqlReflectionsProcessor`, `SourceReflectionProcessor`), [Sync.md](Sync.md) (entity sync protocol)

**Status (2026-09):** two persistence stacks. EF is what graph apps still run on. `SqlDatabase` / `SqlRepository` is a **source-generated table mapper** (not EF). Sample Loopback and SQL unit tests run on it. It is **not** a drop-in EF replacement.

---

## Purpose

- Known entity types → SQL Server or SQLite (including `Mode=Memory`)
- Generated CREATE / INSERT-upsert / DELETE templates and property accessors
- `Where` / `OrderBy` / `Count` / `Any` (`EXISTS`) / `GetById` / `DeleteWhere` via expression **interpretation** (SQL strings), not `Expression.Compile`. Boolean constants are `(1 = 1)` / `(1 = 0)` because SQL Server `WHERE` cannot use a BIT parameter as a condition. DateTime values round-trip as UTC (`DATETIME2` / SQLite have no Kind).
- Name-based reads through generated `SetValue` delegates (`SourceTypeInfo.GetProperty` is a name dictionary)
- SELECT lists only mapped SQL columns; WHERE/ORDER BY use the provider identifier quotes
- `[SqlIndex]` / column `IsUnique` → `CREATE INDEX`; `[SqlForeignKey("Addresses")]` → FK on CREATE TABLE (SQLite rebuild) or `ALTER TABLE ADD CONSTRAINT` (SQL Server). `SyncEntity.ModifiedOn` is indexed (`IX_{table}_ModifiedOn`) for GetChanges time windows.
- `GetSqlColumnType` matches generated CREATE TABLE types (SQLite `DATE` / `TEXT` for DateTime/Guid)
- SQLite `SqlDatabase` keeps one connection for its lifetime (memory and file). `WithConnection` and `ExecuteInTransaction` reuse it; they do not open a new connection per command. SQL Server still opens per command (pool). Catalog reads during migrate use the work connection so they see uncommitted DDL. Nested `ExecuteInTransaction` reuses the open transaction (no inner commit). `Add`/`Delete` are pending; `SaveChanges` and each `Migrate()` step run in **one connection + transaction**.
- `IsDatabaseMigrated()` is true when every registered migration is in `MigrationHistory` (cached per instance after the check or `Migrate()`). No registered migrations → false so `EnsureTableCreated` still runs. `DatabaseManager` skips `Migrate()` when history is already current.
- Integration tests: `Cornerstone.IntegrationTests` for anything that needs SQL Server or other external resources. Unit tests stay on SQLite memory / string generation by default. Flip `IncludeSqlServerDatabase` on `CornerstoneUnitTest` to also run `SyncScenarioTest` matrices (SQL mapper and EF) against unique localhost databases; inconclusive if localhost is down. Integration SQL Server tests drop `Cornerstone.Sample.Tests` before and after and are inconclusive if localhost is down. Migration `CreateTable` / `RebuildTable` create a missing table, add missing columns, or **rebuild on SQLite** when a declared type / nullability / PK changes. SQL Server uses `ALTER COLUMN` for type/null (not identity/PK).

Out of scope (do not grow toward these unless the product explicitly drops EF):

- Full `IQueryable` provider, joins, `Include` / graph fix-up
- Change tracking, identity map, attach-on-read
- Generated per-row `Read(DbDataReader)` ordinal materializers (name loop + dictionary lookup is enough unless a profile says otherwise)
- Native AOT **query precompilation** of arbitrary LINQ (EF’s problem; this stack avoids it)

SQL mapper **does** get versioned migrations (model snapshot vs last snapshot, `Add-SqlMigration` / `Update-SqlDatabase`). That is not EF Core’s `dotnet ef` pipeline. Do not generate migrations from live catalog (`QueryTables`).

---

## Two stacks

| Stack | Use when | Identity |
|-------|----------|----------|
| **`Cornerstone.EntityFramework`** | Graphs, `Include`, sync **today**, existing `ISyncableDatabase` | Local `Id` + tracked entities |
| **`Cornerstone.Storage.Sql`** | One table, explicit SQL, AOT, SQLite memory, `ISyncableDatabase` | Same `Entity` / `SyncEntity` types; pending list until `SaveChanges` |

Do not treat Sql\* as “EF but AOT.” Sync apply can use `SqlSyncableDatabase` (`UpsertSync` on `SyncId`). `IQueryable` / `Include` stay on EF.

```
App / SyncClientForDatabase
        │
        ├─ ISyncableDatabase  →  EntityFrameworkSyncableDatabase  (current hosts)
        │
        └─ ISyncableDatabaseProvider  →  SqlSyncableDatabaseProvider
               └─ SqlSyncableDatabase
                      └─ SqlSyncableRepository<T,TKey>  (ISyncableRepository)
                             └─ SqlRepository<T>.UpsertSync / Query
```

---

## Architecture at a glance

| Type | Role |
|------|------|
| **`SqlDatabase`** | Unit of work: one connection, pending `Add`/`Delete`, `SaveChanges` bulk flush. Not `IDatabase` yet (`IQueryable` / `Include`). Workflow tests: `SqlDatabaseWorkflowTests`; todo: [Todo/SqlEfWorkflow.md](../Todo/SqlEfWorkflow.md) |
| **`SqlSyncableDatabase`** | `ISyncableDatabase` over Sql; `SaveChanges` flushes SQL pending and SyncId upserts in one transaction |
| **`SqlSyncableDatabaseProvider`** | `ISyncableDatabaseProvider` for `SyncClientForDatabase` |
| **`DatabaseManager<T>`** | App provider: settings, key cache, `Migrate()` on open. Sample: `SampleSqlDatabaseManager` |
| **`ISampleDatabase`** | Sample contract (`Accounts` as non-generic `ISyncableRepository`). SQL + EF implementations |
| **`SampleSyncManager`** | Client is `SampleSyncClient`; server peer is `WebSyncClient` via `SampleLoopbackServerSyncClientProvider` |
| **`SqlSyncableRepository<T,TKey>`** | Non-generic `ISyncableRepository`: Read/GetChanges/Add/Remove. Factories registered as closed generics |
| **`SqlRepository<T>`** | `Add`/`Delete` queue until `SaveChanges`. `UpsertSync` writes now (flush tool). `GetById`, `Any`, `DeleteWhere`, `Count`, `Where` |
| **`SqlQuery<T>`** | Composable `Where` / `OrderBy` → `Query()` / `Any()`. `T : Entity`. Runs on `SqlDatabase` work connection when constructed from a database |
| **`SqlGenerator`** | Looks up **registered** scripts/extractors; builds WHERE SQL via visitors |
| **`PredicateToSqlVisitor`** / **`OrderByExpressionVisitor`** | Walk `Expression` trees to SQL + parameters (legal under Native AOT) |
| **`[SqlTable]` / `[SqlTableColumn]`** | Schema for the generator |
| **`[SourceReflection]`** | Emits `SourceTypeInfo` + `GetValue` / `SetValue` lambdas |

Entities must be `[SourceReflection]` + `[SqlTable]` or `GetCreateTableScript` / insert / delete throw (no runtime script synthesis).

### What the generator emits

For each `[SqlTable]` type, `SqlReflectionsProcessor` registers **per provider** (Sqlite + SqlServer):

- CREATE TABLE
- INSERT … `ON CONFLICT(PK)` / `MERGE` on **primary key `Id`**
- INSERT … `ON CONFLICT(SyncId)` / `MERGE ON SyncId` with `ModifiedOn` guard (`GetSyncUpsertQuery`), when the type has `SyncId` + `ModifiedOn`
- DELETE by PK
- `GetUpsertParams*` / `GetSyncUpsertParams` / `GetPrimaryKey` (direct property reads)
- `UNIQUE` on `[SqlTableColumn(IsUnique = true)]` (required for SQLite `ON CONFLICT("SyncId")`)

`SourceReflectionProcessor` emits `SourcePropertyInfo` with:

```text
SetValue = static (instance, value) => ((AccountEntity) instance).Name = (string) value
```

Fallback `PropertyInfo.SetValue` only if the generator skipped the member (init-only, private, struct, missing `[SourceReflection]`).

### Reads (not a per-type materializer)

`SqlQuery<T>.ReadResults` is still a **generic loop**:

1. `reader.GetName(i)` → `SourceTypeInfo.GetProperty` (name dictionary)
2. `reader.GetFieldType(i)` vs destination (unwrap `Nullable<>`; enums use the enum underlying type)
3. Typed getter (`GetString` / `GetInt32` / `GetGuid` / …) when the field type **matches**; varchar uses `GetString` then `ConvertTo` — never `GetInt32` on text
4. Else `GetValue` + `ConvertTo`
5. generated `SetValue`

That is name-dynamic (reorder / extra / missing columns). It is **not** a generated `Read(DbDataReader)` with `GetInt32(0)`. Accessors are generated; the row plan is not. `SetValue(object)` still boxes value types until binders take `(T, DbDataReader, int)`.

---

## Sync vs this mapper

Sync apply is full images + time + filters. `SyncClientForDatabase.ProcessSyncObject` reads by `SyncId` (or lookup filter), copies via converter, then `SaveChanges`. On SQL that flushes pending `UpsertSync` / delete. On EF it is the usual change tracker.

| Need | Sql\* today | What sync needs |
|------|-------------|-----------------|
| Upsert key | Local `Id` **and** `GetSyncUpsertQuery` on **`SyncId`** | Sync apply uses `UpsertSync` (`ON CONFLICT(SyncId)` + `ModifiedOn` guard) |
| Skip older | `ModifiedOn` predicate on the SyncId upsert | `SavePending` turns the guard off for `IsDeleted` rows. EF tombstones `Remove` via `PermanentSyncEntityDeletions`; SQL `Remove` is still hard delete |
| Combined `Where` | Date window AND outgoing / skip-deleted filters | `PredicateToSqlVisitor` start index must continue across predicates (`@p0` then `@p4`, not reset) |
| `SavePending` | Pending add/update/delete | Applies `MaintainCreatedOn` / `MaintainModifiedOn` / `MaintainSyncId` from `DatabaseSettings` using the database `IDateTimeProvider` |
| Local FKs (`AccountId`) | Copied if present on the row | **`UpdateLocalRelationships`** sets `*Id` from one `Read(syncId)`. A stored related row outside scope, or failing the incoming filter, is treated as missing |
| Batch | `UpsertSync` / `Delete` lists, chunks of 64 | `SavePending` uses those list overloads |

`UpdateEntity` **requires** `SyncClientConverter`. `Converter.Update` copies the image and must set local `*Id` from `*SyncId`. Missing converter or `Update == false` → `SyncIssue` (`UpdateException`). Custom converters throw `SyncUpdateException` / `SyncIssueException` when a related row cannot be resolved.

Do not upsert incoming local `*Id` values from another machine.

SQLite `SqlDatabase` holds one connection until dispose. Memory (`Mode=Memory`) needs that keep-alive or the last close drops the DB; file databases reuse it so apply/GetChanges are not one-open-per-command. Sample always uses shared memory (client vs server by `Data Source` name). `CREATE TABLE IF NOT EXISTS` will not add columns to an old file; a leftover `Accounts` table without `AddressId` surfaces as SQLite `no such column: "AddressId"` (double quotes are identifiers, not string literals). `SampleSqlDatabaseManager` holds the keep-alive until Uninitialize. Local `*Id` values stay per-database. Related rows use `FooId` + `FooSyncId`; the converter sets `*Id` after the parent exists (`SyncOrder`).

`GetChanges` includes a row when `CreatedOn` is in `[since, until)` **or** `ModifiedOn` is in `[since, until)`, with `until` exclusive. Combined `Sync` freezes `until` at session start. Order is ModifiedOn then Id. Paging is `Skip`/`Take` across that ordered set, not a local-Id or type-name cursor.

---

## Native AOT

Walking expressions to SQL is AOT-legal. Compiling them to IL is not.

| Piece | AOT |
|-------|-----|
| Generated CUD SQL + extractors | Yes |
| Generated `GetValue` / `SetValue` | Yes (named members). Source reflection construction and accessors must stay AOT-safe; see [SourceReflection.md](SourceReflection.md) |
| `PredicateToSqlVisitor` | Yes (interpret, don’t `Compile`) |
| `ConvertTo(Type)` / `GetValue` | Mostly; trim-sensitive on open `Type` |
| `typeof(T).GetProperty` on generated metadata | Keep members rooted via `[SourceReflection]` |
| `Microsoft.Data.SqlClient` | Provider-dependent; watch trim warnings |
| EF Core | Experimental; not this stack |

---

## How to add a mapped type

1. Entity: `[SourceReflection]`, `[SqlTable]`, `[SqlTableColumn]` on stored properties. PK: `IsPrimaryKey` / `IsAutoIncrement` as needed. Sync types already set `Id` + unique `SyncId` on `SyncEntity<TKey>`.
2. Rebuild so the generator registers scripts (missing registration throws with those attribute names in the message).
3. `database.GetRepository<T>()` then `EnsureTableCreated` / `Add` / `Where` / `GetById`.
4. Predicates must be translatable (`PredicateToSqlVisitor`). No joins.

Do not leak `IQueryable`. Keep `SqlQuery<T>` as a closed builder.

---

## Common pitfalls

1. **Settable entity navigation on `[SqlTable]`** — generator maps every settable property as a column. FK trio is `*Id` + `*SyncId` only.
2. **Copying `AccountId` from the wire** — local autoincrement; converter must rewrite.
3. **`GetInt32` on varchar** — dispatch on `GetFieldType`, not property type or `GetDataTypeName`.
4. **Assuming reads are a generated materializer** — only setters are; the loop is shared.
5. **Type without `[SqlTable]`** — runtime “no generated script registered”.
6. **Chasing EF features** — `Include`, tracker, and open LINQ rejoin EF’s AOT wall.
7. **Silent converter `false`** — apply now records `UpdateException` instead of skipping.
8. **SQLite `"AddressId"` is an identifier** — missing column, often stale `CREATE TABLE IF NOT EXISTS`; not a string-literal quote bug.
9. **Catalog reads during `Migrate` must share the work connection** — another connection cannot see uncommitted DDL.
10. **PowerShell `Update-SqlDatabase`** — `-Assembly` should be a host output folder (Sample.Desktop) so natives and MicroCom load; `GetTypes()` must skip unloadable types.
11. **`GetDatabaseName` is the catalog, not the server** — prefer `Initial Catalog` then `Database` then `AttachDbFilename`; only then `Data Source` / `Filename` (SQLite). `Data Source=localhost;Initial Catalog=Hello-World` is `Hello-World`. `GetMasterString` rewrites `Database` / `Initial Catalog` to `master` (including quoted values).
12. **`Read` does not enqueue upsert** — mutate then `Add` (or apply’s `repository.Add` after update/soft-delete) before `SaveChanges`.
13. **`SqlQuery` without a `SqlDatabase`** — opens a new connection; it will not see an uncommitted work transaction. `SqlRepository.Where` passes the database.
14. **LIKE wildcards** — user `%` / `_` / `\` are escaped; SQL uses `ESCAPE '\'`. `string.Length` is `LEN` (SQL Server) or `LENGTH` (SQLite).
15. **Visitors default to no provider** — pass `SqlProvider`; do not assume SQL Server brackets.

---

## File map

| Path | Responsibility |
|------|----------------|
| `Storage/Sql/SqlDatabase.cs` | Connections, work transaction, `Migrate()`, `QueryTables` |
| `Storage/Sql/ConnectionStringParser.cs` | Catalog name + master connection string (SQL Server create/drop) |
| `Storage/Sql/SqlSyncableDatabase.cs` | `ISyncableDatabase` (no `IQueryable`) |
| `Storage/Sql/SqlSyncableDatabaseProvider.cs` | `ISyncableDatabaseProvider` factory |
| `Storage/DatabaseManager.cs` | App-facing provider; migrates on `GetSyncableDatabase` |
| `Cornerstone.Sample/Storage/SampleSqlDatabaseManager.cs` | `DatabaseManager<ISampleDatabase>` for Sample SQL files |
| `SampleClientSqlDatabaseManager` / `SampleServerSqlDatabaseManager` | DI singletons; `SyncProcessor` Tracks them |
| `Cornerstone.Sample/Tabs/Sync/TabSync.cxaml` | Sample loopback UI (Data → Loopback) |
| `Cornerstone.Sample/Sync/SampleLoopbackWebClient.cs` | In-process `IWebClient.Post` → `SampleServerSyncClient.Sync` |
| `Cornerstone.Sample/Sync/SampleLoopbackServerSyncClientProvider.cs` | Server `GetSyncClient` returns `WebSyncClient` |
| `Storage/Sql/SqlSyncableRepository.cs` | Sync repository + pending `SaveChanges` (batched upsert/delete) |
| `Storage/Sql/SqlRepository.cs` | CUD + `GetById` / `Any` + single/list `UpsertSync` / `Delete` |
| `Storage/Sql/SqlQuery.cs` | SELECT builder, `Any`/`EXISTS`, `ReadResults`, `ConvertTo` (null `Nullable<T>` from DBNull) |
| `Storage/Sql/PredicateToSqlVisitor.cs` | Expression → SQL; stacked `Where` indexes; `IN` / LIKE escape / `LEN`/`LENGTH` / `LOWER`/`UPPER` |
| `Storage/Sql/SqlGenerator.cs` | Registry + WHERE/COUNT/EXISTS/batch upsert/delete SQL |
| `Storage/Sql/OrderByExpressionVisitor.cs` | Order-by columns (`SqlProvider` required) |
| `Storage/Sql/SqlTableAttribute.cs` / `SqlTableColumnAttribute.cs` | Schema attributes |
| `Storage/Sql/Migrations/` | Snapshot, differ, `Migrate()`, scaffolder, `SqlMigrationHost` (cmdlets) |
| `Storage/Entity.cs` / `Sync/SyncEntity.cs` | Entity bases; `SyncId` unique |
| `Cornerstone.Generators/Processors/SqlReflectionsProcessor.cs` | CUD script emission |
| `Cornerstone.Generators/Processors/SourceReflectionProcessor.cs` | `GetValue` / `SetValue` |
| `Cornerstone.EntityFramework/*` | EF database/repository/sync adapters |

Tests: `Tests/Cornerstone.UnitTests/Storage/Sql/` (memory / generated SQL). Live SQL Server: `Cornerstone.IntegrationTests`.

Product overview: [../Storage.md](../Storage.md).

---

## Current mapper behavior (2026-09)

- Versioned schema (snapshot, differ, `SqlMigration` / `MigrationBuilder`, scaffolder, PowerShell cmdlets). No live-catalog migration generation. Stub `Compare` / `GenerateAlterScript` removed.
- `SqlQuery` constructed from `SqlDatabase` shares that instance’s SQLite keep-alive (or the work transaction when inside `ExecuteInTransaction`). A second `SqlDatabase` is a second connection; do not construct one per apply row.
- `GetById` attaches `INotifyPropertyChanged` after load (not a proxy). Property changes queue an UPDATE by primary key on `SaveChanges`. `Query` / sync `Read` do not attach on the SQL repository — apply `Discard` and GetChanges must not enqueue a mapper update.
- `Add` / `Delete` queue; `SaveChanges` bulk-flushes and sets PK from `RETURNING` / `OUTPUT`. Chunk size is parameters-per-row against a bind cap (SQLite 2048, SQL Server 2100), not SQLite’s 32766 ceiling — that many binds is slower than more, smaller commands. `UpsertSync` still writes immediately (apply flush). List sync upserts use SQLite `excluded.*` or SQL Server `MERGE`; list deletes use `IN`. Returned Ids are matched by SyncId dictionary, not a scan of the chunk.
- Predicates: stacked `Where` parameter indexes; `IN` from collection `Contains`; LIKE escape; provider `LEN`/`LENGTH` and `LOWER`/`UPPER`; AOT equality for `Guid` / `DateTimeOffset` / `DateTime` / `decimal`. Visitors require `SqlProvider`.
- `GetById` / `Any` (`EXISTS`). `SqlQuery<T> : Entity`.
- Sample Data → Loopback: two in-memory SQLite DBs; server peer is `WebSyncClient` + `SampleLoopbackWebClient`.

---

## Next (intentional order)

1. **Do not** add per-query ordinal materializers unless profiling says the remaining name loop is the hot path.