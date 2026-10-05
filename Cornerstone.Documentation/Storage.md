# Storage (SQL mapper)

Cornerstone has two persistence stacks. **Entity Framework** is the full ORM used by existing sync and graph apps. **`Cornerstone.Storage.Sql`** is a smaller, source-generated table mapper for SQLite and SQL Server. It is meant for local/AOT-friendly stores and Sample-style sync, not as a drop-in replacement for EF.

## What it does

You mark entity types with `[SqlTable]` / `[SqlTableColumn]` (and optional `[SqlIndex]`, `[SqlForeignKey]`). A source generator emits CREATE TABLE, insert/upsert, and delete SQL for SQLite and SQL Server. Runtime code interprets `Where` / `OrderBy` expressions into SQL instead of compiling LINQ.

`Add` and `Delete` queue in memory, like EF. `SaveChanges` writes them in **one transaction** and sets primary keys from `RETURNING` / `OUTPUT`. Each command holds as many rows as fit in the bind cap (2048 parameters on SQLite, 2100 on SQL Server). `GetById` loads one row by primary key. `Any` is an `EXISTS` check. A list `Contains` on a column becomes `IN`. Queries opened from a `SqlDatabase` share that database’s connection and transaction.

`Migrate()` applies each pending versioned migration in **its own transaction** (schema work plus the `MigrationHistory` row). If either fails, that unit of work is rolled back.

Schema updates are versioned, in the same spirit as EF migrations, but without `dotnet ef`: capture a model snapshot, `Add-SqlMigration` writes `Up`/`Down`, `Update-SqlDatabase` (or `database.Migrate()`) applies what is not yet in `MigrationHistory`. SQLite cannot alter many column shapes, so a type/nullability change rebuilds the table. SQL Server uses `ALTER COLUMN` when it can.

Sample’s SQL databases are **shared in-memory SQLite** (client and server use different `Data Source` names). That avoids leftover file schemas and lets the browser demo run. A keep-alive connection holds the memory database until the manager tears down. The Sample app exercises this on **Data → Loopback**: add an address and account on the client, then **Sync all** copies them to the server database. The server side of that loop is a `WebSyncClient` posting `SyncOperation` through an in-process loopback (same hop as `api/Sync`), not a direct in-process `ServerSyncClient` from the session.

## What it does not do

It does not implement `IQueryable`, `Include`, an identity map, or EF-style change tracking. Reading a sync row does not mark it dirty; call `Add` again after you change it, then `SaveChanges`. Joins and object graphs stay on EF. Do not generate migrations by comparing the live catalog; generation is **current model vs last snapshot**. Queries that only have a connection string (not a `SqlDatabase`) open their own connection and will not see uncommitted work.

## Hosts

- **Tests / tools:** `EnsureTableCreated` if you never call `Migrate()`.
- **Apps:** `DatabaseManager<T>` (Sample: `SampleSqlDatabaseManager`) opens the database and migrates when history is not current.
- **CLI:** PowerShell `Add-SqlMigration`, `Remove-SqlMigration`, `Get-SqlMigration`, `Update-SqlDatabase`. Point `-Assembly` at a host output folder that contains Sample’s dependencies (for example Sample.Desktop).

SQL Server integration tests live in `Cornerstone.IntegrationTests`. They create and drop `Cornerstone.Sample.Tests` on `localhost`. Unit tests stay on SQLite memory and generated SQL strings.

The catalog name comes from **Initial Catalog** or **Database**, not **Data Source**. `Data Source=localhost;Initial Catalog=Hello-World` is database `Hello-World` on server `localhost`. For SQLite, `Data Source` / `Filename` is the file or memory name.
