# SQL mapper: EF unit-of-work workflow

Drive `SqlDatabase` toward EF’s **Add / modify / remove → SaveChanges**, **Dispose / DiscardChanges throws away pending work**, and eventually `IDatabase`. Not EF’s graph stack: no proxies, no `Include`, no compiled LINQ `IQueryProvider`.

Living spec: `Tests/Cornerstone.UnitTests/Storage/Sql/SqlDatabaseWorkflowTests.cs`.

## Out of scope

- Model proxies, lazy navigations, `Include`
- Identity map (same instance on every `GetById`)
- `IQueryable` LINQ against SQL until `IRepository` is split

## 1. Atomic SaveChanges

- [x] Two adds, second unique `SyncId` conflict: `SaveChanges` throws, `Count() == 0`
- [x] Two distinct `SyncId`s: `SaveChanges` returns 2, `Count() == 2`

## 2. Dispose / discard

- [x] `Add` then `Dispose` without save: shared-memory reopen `Count() == 0`
- [x] `Add`, `DiscardChanges()`, `SaveChanges()`: 0 written, `Count() == 0`
- [x] `Add`, `SaveChanges()`, `Add` another, `Dispose`: only the first row exists
- [x] `SaveChanges` after `Dispose` throws

## 3. Add / modify / remove

- [x] `Add` A and B, `Delete` B, `SaveChanges`: only A
- [x] `Add`, `SaveChanges`, `GetById`, change `Name`, `SaveChanges`: name persisted (attach on `GetById`, not on `Query`)
- [x] `Add`, `SaveChanges`, `Delete`, `SaveChanges`: `Count() == 0`
- [x] Two databases, same store: modify in A without save; B still sees the old value

## 4. SqlDatabase : IDatabase (last)

- [ ] Split `IQueryable` / `Include` off `IRepository` (EF keeps them; SQL does not pretend)
- [ ] `SqlDatabase` implements `IDatabase` (`DatabaseSettings`, `DateTimeProvider`, `IsDisposed`, events, `GetRepository<T,T2>`, `Remove`, …)
- [ ] Tests use `IDatabase` for Add / SaveChanges / DiscardChanges / Dispose
- [ ] `Include` is not required on SQL
