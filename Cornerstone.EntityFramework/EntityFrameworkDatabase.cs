#region References

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cornerstone.Compare;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EntityState = Microsoft.EntityFrameworkCore.EntityState;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents an Entity Framework Cornerstone database.
/// </summary>
public abstract class EntityFrameworkDatabase : DbContext, IDatabase
{
	#region Fields

	private readonly CollectionChangeTracker _collectionChangeTracker;
	private int _saveChangeCount;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the database.
	/// </summary>
	protected EntityFrameworkDatabase()
	{
	}

	/// <summary>
	/// Initializes an instance of the database.
	/// </summary>
	/// <param name="startup"> The startup options for this database. </param>
	/// <param name="settings"> The options for this database. </param>
	protected EntityFrameworkDatabase(DbContextOptions startup, DatabaseSettings settings)
		: base(startup)
	{
		_collectionChangeTracker = new CollectionChangeTracker();

		DateTimeProvider = Runtime.DateTimeProvider.RealTime;
		DbContextOptions = startup;
		DatabaseSettings = settings?.DeepClone() ?? new DatabaseSettings();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseSettings DatabaseSettings { get; }

	/// <inheritdoc />
	public IDateTimeProvider DateTimeProvider { get; private set; }

	/// <summary>
	/// Gets the database context options for this database.
	/// </summary>
	public DbContextOptions DbContextOptions { get; }

	/// <inheritdoc />
	public bool IsDisposed { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Discard all changes made in this context to the underlying database.
	/// </summary>
	public int DiscardChanges()
	{
		var count = 0;

		foreach (var entry in ChangeTracker.Entries().ToList())
		{
			switch (entry.State)
			{
				case EntityState.Modified:
				{
					entry.CurrentValues.SetValues(entry.OriginalValues);
					entry.State = EntityState.Unchanged;
					count++;
					break;
				}
				case EntityState.Deleted:
				{
					entry.State = EntityState.Unchanged;
					count++;
					break;
				}
				case EntityState.Added:
				{
					entry.State = EntityState.Detached;
					count++;
					break;
				}
			}
		}

		return count;
	}

	/// <inheritdoc />
	public sealed override void Dispose()
	{
		base.Dispose();
		Dispose(true);
		OnDisposed();
		GC.SuppressFinalize(this);
		IsDisposed = true;
	}

	/// <inheritdoc />
	public DatabaseType GetDatabaseType()
	{
		return Database.ProviderName?.Contains("Sqlite") == true ? DatabaseType.Sqlite : DatabaseType.Sql;
	}

	/// <summary>
	/// Gets the assembly that contains the entity mappings. Base implementation defaults to the implemented types assembly.
	/// </summary>
	public virtual Assembly GetMappingAssembly()
	{
		var assembly = GetType().Assembly;
		//throw new Exception("Here : " + assembly.FullName);
		return assembly;
	}

	/// <inheritdoc />
	public IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>
	{
		return new ReadOnlyEntityFrameworkRepository<T, T2>(this, Set<T>());
	}

	/// <inheritdoc />
	public IRepository<T> GetRepository<T>() where T : Entity
	{
		var entityType = typeof(T);
		var entityKey = entityType.BaseType?.GetGenericArguments().FirstOrDefault();
		var genericMethod = typeof(EntityFrameworkDatabase)
			.GetCachedGenericMethod(nameof(GetRepository), [entityType, entityKey],
				[], ReflectionExtensions.DefaultPublicFlags
			);
		return (IRepository<T>) genericMethod.Invoke(this, []);
	}

	/// <inheritdoc />
	public IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>
	{
		return new EntityFrameworkRepository<T, T2>(this, Set<T>());
	}

	/// <inheritdoc />
	public void Migrate()
	{
		Database.Migrate();
	}

	/// <inheritdoc />
	public T Remove<T, T2>(T item) where T : Entity<T2>
	{
		GetRepository<T, T2>().Remove(item);
		return item;
	}

	/// <summary>
	/// Saves all changes made in this context to the underlying database.
	/// </summary>
	/// <returns>
	/// The number of objects written to the underlying database.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException"> Thrown if the context has been disposed. </exception>
	public override int SaveChanges()
	{
		if (_saveChangeCount++ > 2)
		{
			throw new OverflowException("Database save changes stuck in a processing loop.");
		}

		try
		{
			var first = _saveChangeCount == 1;
			if (first)
			{
				_collectionChangeTracker.Reset();
			}

			var entries = ChangeTracker.Entries().ToList();
			entries.ForEach(ProcessEntity);

			var comparer = new GenericComparer<EntityEntry>(
				(x, y) => x.Entity == y.Entity ? 0 : -1,
				x => x.Entity.GetHashCode()
			);
			var newEntries = ChangeTracker
				.Entries()
				.Except(entries, comparer)
				.ToList();

			if (newEntries.Any())
			{
				newEntries.ForEach(ProcessEntity);
				entries.AddRange(newEntries);
			}

			// The local relationships may have changed. We need keep our sync IDs in sync with any relationships that may have changed.
			entries.ForEach(x => (x.Entity as Entity)?.UpdateLocalSyncIds());

			var response = base.SaveChanges();
			var needsMoreSaving = entries.Any(x => (x.State != EntityState.Detached) && (x.State != EntityState.Unchanged));
			if (needsMoreSaving)
			{
				response += SaveChanges();
			}

			if (first)
			{
				ChangeTracker.AcceptAllChanges();
				OnChangesSaved(_collectionChangeTracker);
			}

			// It's possible that values were added during OnSavedChanges.
			var moreEntries = ChangeTracker.Entries();
			needsMoreSaving = moreEntries.Any(x => (x.State != EntityState.Detached) && (x.State != EntityState.Unchanged));

			if (needsMoreSaving)
			{
				// Consider this loop done?
				_saveChangeCount = 0;
				response += SaveChanges();
			}

			return response;
		}
		catch (Exception ex)
		{
			ProcessException(ex);
			throw;
		}
		finally
		{
			_saveChangeCount = 0;
		}
	}

	/// <summary>
	/// See https://learn.microsoft.com/en-us/archive/technet-wiki/40666.sql-server-2017-identity-cache-feature
	/// </summary>
	/// <param name="enabled"> True to turn on identity cache otherwise false will turn off. </param>
	public void SetIdentityCache(bool enabled)
	{
		var expectedString = enabled ? "ON" : "OFF";
		var currentSetting = Database
			.SqlQueryRaw<bool>("SELECT value FROM sys.database_scoped_configurations WHERE name = 'IDENTITY_CACHE'")
			.FirstOrDefault();

		if (currentSetting == enabled)
		{
			return;
		}

		var query = $"ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = {expectedString}";
		Database.ExecuteSqlRaw(query);
	}

	/// <inheritdoc />
	public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
	{
		DateTimeProvider = dateTimeProvider;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> Should be true if managed resources should be disposed. </param>
	protected virtual void Dispose(bool disposing)
	{
	}

	/// <summary>
	/// Called when an entity is added. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity added. </param>
	protected virtual void EntityAdded(IEntity entity)
	{
	}

	/// <summary>
	/// Called when an entity is added, modified, or deleted. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity modified. </param>
	protected virtual void EntityAddedDeletedOrModified(IEntity entity)
	{
	}

	/// <summary>
	/// Called when an entity is deleted. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity deleted. </param>
	protected virtual void EntityDeleted(IEntity entity)
	{
	}

	/// <summary>
	/// Called when an entity is modified. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity modified. </param>
	protected virtual void EntityModified(IEntity entity)
	{
	}

	/// <summary>
	/// Called when for when changes are saved. <see cref="ChangesSaved" />
	/// </summary>
	protected virtual void OnChangesSaved(CollectionChangeTracker e)
	{
		ChangesSaved?.Invoke(this, e);
	}

	/// <summary>
	/// An invocator for the event when the database has been disposed.
	/// </summary>
	protected virtual void OnDisposed()
	{
		Disposed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// This method is called when the model for a derived context has been initialized, but before the model has been locked
	/// down and used to initialize the context. The default implementation of this method does nothing, but it can be
	/// overridden in a derived class such that the model can be further configured before it is locked down.
	/// </summary>
	/// <remarks>
	/// Typically, this method is called only once when the first instance of a derived context is created. The model for that
	/// context is then cached and is for all further instances of the context in the app domain. This caching can be disabled
	/// by setting the ModelCaching property on the given ModelBuilder, but note that this can seriously degrade performance.
	/// More control over caching is provided through use of the DbModelBuilder and DbContextFactory classes directly.
	/// </remarks>
	/// <param name="modelBuilder"> The builder that defines the model for the context being created. </param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		var assembly = GetMappingAssembly();
		var types = assembly.GetTypes();
		var mappingTypes = types.Where(x => !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(IEntityMappingConfiguration)));
		var mappings = mappingTypes
			.Select(x => x.CreateInstance())
			.Cast<IEntityMappingConfiguration>()
			.ToList();

		foreach (var config in mappings)
		{
			config.Map(modelBuilder);
		}

		ProcessModelTypes(modelBuilder);

		base.OnModelCreating(modelBuilder);
	}

	/// <summary>
	/// Allows data context a chance to process the exception that occurred during save changes.
	/// </summary>
	/// <param name="exception"> The exception that occurred when trying to save changes. </param>
	protected virtual void ProcessException(Exception exception)
	{
		switch (exception)
		{
			case ValidationException ve:
			{
				throw new Exceptions.ValidationException(ve.Message);
			}
			case DbUpdateException ue:
			{
				// Wrap in a Cornerstone exception for consistency
				throw new UpdateException(ue.Message, ue);
			}
		}
	}

	/// <summary>
	/// Processes the model builder for the Cornerstone default types.
	/// </summary>
	/// <remarks>
	/// DateTime values will default to "datetime2" and UTC.
	/// Guid values default to "uniqueidentifier" type.
	/// Strings default to non-unicode.
	/// </remarks>
	/// <param name="modelBuilder"> The model builder. </param>
	protected virtual void ProcessModelTypes(ModelBuilder modelBuilder)
	{
		var isoDateTimeConverter = new ValueConverter<IsoDateTime, DateTime>(
			x => (x.DateTime.Ticks == DateTimeExtensions.MinDateTimeTicks)
				|| (x.DateTime.Ticks == DateTimeExtensions.MaxDateTimeTicks)
					? DateTime.SpecifyKind(x, DateTimeKind.Utc)
					: x.DateTime.ToUniversalTime(),
			x => x
		);

		var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
			x => (x.Ticks == DateTimeExtensions.MinDateTimeTicks)
				|| (x.Ticks == DateTimeExtensions.MaxDateTimeTicks)
					? DateTime.SpecifyKind(x, DateTimeKind.Utc)
					: x.ToUniversalTime(),
			x => DateTime.SpecifyKind(x, DateTimeKind.Utc)
		);

		var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
			x => x.HasValue
				? (x.Value.Ticks == DateTimeExtensions.MinDateTimeTicks)
				|| (x.Value.Ticks == DateTimeExtensions.MaxDateTimeTicks)
					? DateTime.SpecifyKind(x.Value, DateTimeKind.Utc)
					: x.Value.ToUniversalTime()
				: x.Value.ToUniversalTime(),
			x => x.HasValue ? DateTime.SpecifyKind(x.Value, DateTimeKind.Utc) : x
		);

		var timeSpanConverter = new ValueConverter<TimeSpan, long>(
			x => x.Ticks,
			x => TimeSpan.FromTicks(x)
		);

		var nullableTimeSpanConverter = new ValueConverter<TimeSpan?, long?>(
			x => x.HasValue ? x.Value.Ticks : null,
			x => x.HasValue ? TimeSpan.FromTicks(x.Value) : null
		);

		var versionConverter = new ValueConverter<Version, ulong>(
			x => x.ToVersionHash(),
			x => x.FromVersionHash()
		);

		foreach (var entity in modelBuilder.Model.GetEntityTypes())
		{
			var properties = entity.GetProperties();

			foreach (var p in properties)
			{
				switch (p.ClrType)
				{
					case not null when p.ClrType == typeof(IsoDateTime):
					{
						p.SetColumnType("datetime2");
						p.SetValueConverter(isoDateTimeConverter);
						break;
					}
					case not null when p.ClrType == typeof(DateTime):
					{
						p.SetColumnType("datetime2");
						p.SetValueConverter(dateTimeConverter);
						break;
					}
					case not null when p.ClrType == typeof(DateTime?):
					{
						p.IsNullable = true;
						p.SetColumnType("datetime2");
						p.SetValueConverter(nullableDateTimeConverter);
						break;
					}
					case not null when p.ClrType == typeof(decimal):
					case not null when p.ClrType == typeof(decimal?):
					{
						p.SetColumnType("decimal(18, 6)");
						break;
					}
					case not null when p.ClrType == typeof(Guid):
					case not null when p.ClrType == typeof(Guid?):
					{
						p.SetColumnType("uniqueidentifier");
						break;
					}
					case not null when p.ClrType == typeof(TimeSpan):
					{
						p.SetColumnType("bigint");
						p.SetValueConverter(timeSpanConverter);
						break;
					}
					case not null when p.ClrType == typeof(TimeSpan?):
					{
						p.IsNullable = true;
						p.SetColumnType("bigint");
						p.SetValueConverter(nullableTimeSpanConverter);
						break;
					}
					case not null when p.ClrType == typeof(Version):
					{
						p.SetColumnType("bigint");
						p.SetValueConverter(versionConverter);
						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Manages the created on and modified on members of the base entity.
	/// </summary>
	/// <param name="entry"> </param>
	private void ProcessEntity(EntityEntry entry)
	{
		var states = new[] { EntityState.Added, EntityState.Deleted, EntityState.Modified };
		if (!states.Contains(entry.State))
		{
			// This entity is a state that is not tracked.
			return;
		}

		// entry should be of type IEntity, not sure if you can get here if not?
		if (entry.Entity is not IEntity entity)
		{
			return;
		}

		var createdEntity = entity as ICreatedEntity;
		var modifiableEntity = entity as IModifiableEntity;
		var syncableEntity = entity as ISyncEntity;
		var maintainedEntity = DatabaseSettings.UnmaintainedEntities.All(x => x != entry.Entity.GetType());
		var maintainCreatedOnDate = maintainedEntity && DatabaseSettings.MaintainCreatedOn;
		var maintainModifiedOnDate = maintainedEntity && DatabaseSettings.MaintainModifiedOn;
		var maintainSyncId = maintainedEntity && DatabaseSettings.MaintainSyncId;
		var now = DateTimeProvider.UtcNow;

		// Check to see if the entity was added.
		switch (entry.State)
		{
			case EntityState.Added:
			{
				_collectionChangeTracker.AddAddedEntity(entity);

				if ((createdEntity != null) && maintainCreatedOnDate)
				{
					createdEntity.CreatedOn = now;
				}

				if ((modifiableEntity != null) && maintainModifiedOnDate)
				{
					modifiableEntity.ModifiedOn = now;
				}

				if ((syncableEntity != null) && maintainSyncId && (syncableEntity.SyncId == Guid.Empty))
				{
					syncableEntity.SyncId = Guid.NewGuid();
				}

				entity.EntityAdded(now);
				entity.EntityAddedDeletedOrModified(now);

				EntityAdded(entity);
				EntityAddedDeletedOrModified(entity);
				break;
			}
			case EntityState.Modified:
			{
				if (!entity.CanBeModified())
				{
					// Tell entity framework to not update the entity.
					entry.State = EntityState.Unchanged;
					break;
				}

				_collectionChangeTracker.AddModifiedEntity(entity);

				// If Cornerstone is maintaining the CreatedOn date then we will not allow modifications outside Cornerstone
				if ((createdEntity != null) && maintainCreatedOnDate && entry.CurrentValues.Properties.Any(x => x.Name == nameof(ICreatedEntity.CreatedOn)))
				{
					#if NETSTANDARD2_0_OR_GREATER
					// Do not allow created on to change for entities.
					createdEntity.CreatedOn = (DateTime) entry.OriginalValues[nameof(ICreatedEntity.CreatedOn)];
					#else
					// Do not allow created on to change for entities.
					if (entry.OriginalValues.TryGetValue<DateTime>(nameof(ICreatedEntity.CreatedOn), out var createdOn))
					{
						createdEntity.CreatedOn = createdOn;
					}
					#endif
				}

				// If Cornerstone is maintaining the ModifiedOn then we will set it to 'now'
				if ((modifiableEntity != null) && maintainModifiedOnDate)
				{
					// Update modified to now for new entities.
					modifiableEntity.ModifiedOn = now;
				}

				if ((syncableEntity != null) && maintainSyncId && entry.CurrentValues.Properties.Any(x => x.Name == nameof(ISyncEntity.SyncId)))
				{
					#if NETSTANDARD2_0_OR_GREATER
					// Do not allow sync ID to change for entities.
					syncableEntity.SetEntitySyncId((Guid) entry.OriginalValues[nameof(ISyncEntity.SyncId)]);
					#else
					// Do not allow created on to change for entities.
					if (entry.OriginalValues.TryGetValue<Guid>(nameof(ISyncEntity.SyncId), out var syncId))
					{
						syncableEntity.SetEntitySyncId(syncId);
					}
					#endif
				}

				entity.EntityModified(now);
				entity.EntityAddedDeletedOrModified(now);

				EntityModified(entity);
				EntityAddedDeletedOrModified(entity);
				break;
			}
			case EntityState.Deleted:
			{
				if ((syncableEntity != null) && !DatabaseSettings.PermanentSyncEntityDeletions)
				{
					syncableEntity.IsDeleted = true;
					syncableEntity.ModifiedOn = now;
					entry.State = EntityState.Modified;

					_collectionChangeTracker.AddModifiedEntity(entity);

					entity.EntityModified(now);
					entity.EntityAddedDeletedOrModified(now);

					EntityModified(entity);
					EntityAddedDeletedOrModified(entity);
				}
				else
				{
					_collectionChangeTracker.AddRemovedEntity(entity);

					entity.EntityDeleted(now);
					entity.EntityAddedDeletedOrModified(now);

					EntityDeleted(entity);
					EntityAddedDeletedOrModified(entity);
				}

				break;
			}
		}

		#if DEBUG
		if ((entity is ICreatedEntity c && (c.CreatedOn == DateTime.MinValue))
			|| (entity is IModifiableEntity m && (m.ModifiedOn == DateTime.MinValue))
			|| (entity is ISyncEntity s && (s.SyncId == Guid.Empty)))
		{
			Debugger.Break();
		}
		#endif
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler<CollectionChangeTracker> ChangesSaved;

	/// <inheritdoc />
	public event EventHandler Disposed;

	#endregion
}