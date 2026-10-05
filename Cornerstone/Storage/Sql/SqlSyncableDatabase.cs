#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Storage.Sql.Migrations;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage.Sql;

/// <summary>
/// SQL-backed <see cref="ISyncableDatabase" />. Does not implement <see cref="IQueryable{T}" />
/// repositories; use <see cref="GetSyncableRepository(Type)" /> or <see cref="SqlDatabase.GetRepository{T}" />.
/// </summary>
public class SqlSyncableDatabase : SqlDatabase, ISyncableDatabase
{
	#region Fields

	private readonly Type[] _entityTypes;

	private static readonly Dictionary<Type, Func<SqlSyncableDatabase, ISyncableRepository>> _repositoryFactories;
	private readonly ConcurrentDictionary<string, ISyncableRepository> _syncableRepositories;

	#endregion

	#region Constructors

	public SqlSyncableDatabase(string connectionString, SqlProvider provider, params Type[] entityTypes)
		: this(connectionString, provider, null, null, null, entityTypes)
	{
	}

	public SqlSyncableDatabase(
		string connectionString,
		SqlProvider provider,
		DatabaseSettings settings,
		DatabaseKeyCache keyCache,
		IDateTimeProvider dateTimeProvider,
		params Type[] entityTypes
	)
		: this(connectionString, provider, settings, keyCache, dateTimeProvider, null, entityTypes)
	{
	}

	public SqlSyncableDatabase(
		string connectionString,
		SqlProvider provider,
		DatabaseSettings settings,
		DatabaseKeyCache keyCache,
		IDateTimeProvider dateTimeProvider,
		IReadOnlyList<SqlMigration> migrations,
		params Type[] entityTypes
	)
		: base(connectionString, provider, migrations)
	{
		_entityTypes = entityTypes ?? [];
		_syncableRepositories = new ConcurrentDictionary<string, ISyncableRepository>();
		DateTimeProvider = dateTimeProvider ?? Runtime.DateTimeProvider.RealTime;

		DatabaseSettings = settings ?? new DatabaseSettings();
		KeyCache = keyCache;
		Profiler = null;
		SyncOrder = DatabaseSettings.SyncOrder ?? [];
	}

	static SqlSyncableDatabase()
	{
		_repositoryFactories = [];
	}

	#endregion

	#region Properties

	public DatabaseSettings DatabaseSettings { get; }

	public IDateTimeProvider DateTimeProvider { get; private set; }

	public DatabaseKeyCache KeyCache { get; }

	public Profiler Profiler { get; set; }

	public (string entity, string syncObject)[] SyncOrder { get; set; }

	#endregion

	#region Methods

	public override int DiscardChanges()
	{
		base.DiscardChanges();
		foreach (var repository in _syncableRepositories.Values.OfType<ISqlSyncableRepository>())
		{
			repository.DiscardPending();
		}

		return 0;
	}

	public DatabaseType GetDatabaseType()
	{
		return Provider == SqlProvider.SqlServer
			? DatabaseType.SqlServer
			: DatabaseType.Sqlite;
	}

	public Assembly GetMappingAssembly()
	{
		return GetType().Assembly;
	}

	public IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>
	{
		throw CreateQueryableNotSupported();
	}

	public IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>
	{
		throw CreateQueryableNotSupported();
	}

	public IEnumerable<ISyncableRepository> GetSyncableRepositories()
	{
		foreach (var type in _entityTypes)
		{
			GetSyncableRepository(type);
		}

		if (SyncOrder.Length <= 0)
		{
			return _syncableRepositories
				.Values
				.OrderBy(x => x.TypeName)
				.ToList();
		}

		var rank = SyncOrder
			.Select((key, index) => new { key, index })
			.ToDictionary(x => x.key.entity, x => x.index);

		return _syncableRepositories
			.OrderBy(kvp => rank.TryGetValue(kvp.Key, out var r) ? r : int.MaxValue)
			.ThenBy(kvp => kvp.Key)
			.Select(kvp => kvp.Value)
			.ToList();
	}

	public ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>
	{
		throw CreateQueryableNotSupported();
	}

	public ISyncableRepository GetSyncableRepository(Type syncEntityType)
	{
		var assemblyName = syncEntityType.ToAssemblyName();
		if (_syncableRepositories.TryGetValue(assemblyName, out var cached))
		{
			return cached;
		}

		if (!_repositoryFactories.TryGetValue(syncEntityType, out var factory))
		{
			throw new InvalidOperationException(
				$"No generated sync repository registered for type '{syncEntityType.Name}'. "
				+ "Ensure the type has [SourceReflection], [SqlTable], and inherits SyncEntity<TKey>."
			);
		}

		var repository = factory(this);
		_syncableRepositories.AddOrUpdate(assemblyName, repository, (_, _) => repository);
		return repository;
	}

	public override void Migrate()
	{
		base.Migrate();
		foreach (var type in _entityTypes)
		{
			GetSyncableRepository(type);
		}
	}

	/// <summary>
	/// Registers a closed SqlSyncableRepository&lt;T, TKey&gt; factory. Called from the
	/// source-generated module initializer.
	/// </summary>
	public static void RegisterRepository(Type entityType, Func<SqlSyncableDatabase, ISyncableRepository> factory)
	{
		_repositoryFactories[entityType] = factory;
	}

	public T Remove<T, T2>(T item) where T : Entity<T2>
	{
		if (item is ISyncEntity syncEntity)
		{
			GetSyncableRepository(typeof(T)).Remove(syncEntity);
			return item;
		}

		throw new NotSupportedException("SqlSyncableDatabase.Remove requires an ISyncEntity.");
	}

	public override int SaveChanges()
	{
		if (IsDisposed)
		{
			throw new InvalidOperationException("The database has been disposed.");
		}

		var written = ExecuteInTransaction(() =>
		{
			int count;
			using (Profiler.Start("SaveChangesFlushPending"))
			{
				count = FlushPending();
			}

			using (Profiler.Start("SaveChangesSavePending"))
			{
				foreach (var repository in _syncableRepositories.Values.OfType<ISqlSyncableRepository>())
				{
					count += repository.SavePending();
				}
			}

			return count;
		});

		ChangesSaved?.Invoke(this, new CollectionChangeTracker());
		return written;
	}

	public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
	{
		DateTimeProvider = dateTimeProvider ?? Runtime.DateTimeProvider.RealTime;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !IsDisposed)
		{
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		base.Dispose(disposing);
	}

	private static NotSupportedException CreateQueryableNotSupported()
	{
		return new NotSupportedException(
			"SqlSyncableDatabase does not implement IQueryable repositories. Use GetSyncableRepository(Type) or SqlDatabase.GetRepository<T>()."
		);
	}

	#endregion

	#region Events

	public event EventHandler<CollectionChangeTracker> ChangesSaved;

	public event EventHandler Disposed;

	#endregion
}