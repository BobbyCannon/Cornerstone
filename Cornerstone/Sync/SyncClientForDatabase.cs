#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync client.
/// </summary>
public abstract class SyncClientForDatabase : SyncClient
{
	#region Fields

	private ISyncableDatabase _applyDatabase;
	private (DateTime Since, DateTime Until, List<(string TypeName, int Count)> Counts)? _changeCountCache;
	private Dictionary<(string TypeName, Guid SyncId), ISyncEntity> _relatedBySyncId;
	private static readonly ConcurrentDictionary<Type, Relationship[]> _relationshipCache;
	private List<string> _syncOrder;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a sync client.
	/// </summary>
	protected SyncClientForDatabase(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		SyncStatistics syncStatistics,
		Profiler syncClientProfiler,
		Logger logger = null)
		: base(name, dateTimeProvider, syncStatistics, syncClientProfiler, logger)
	{
		DatabaseProvider = databaseProvider;
	}

	static SyncClientForDatabase()
	{
		_relationshipCache = new ConcurrentDictionary<Type, Relationship[]>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The database provider to use during a sync session.
	/// </summary>
	public ISyncableDatabaseProvider DatabaseProvider { get; }

	/// <summary>
	/// True if the client is a server client.
	/// </summary>
	private bool IsServerClient => this is ServerSyncClient;

	#endregion

	#region Methods

	/// <summary>
	/// Read a row by SyncId. During apply this uses the open apply database and
	/// the per-group related cache. Host converters must use this instead of
	/// GetDatabase per row; a new database is a new SQLite connection.
	/// </summary>
	public ISyncEntity FindBySyncId(Type type, Guid syncId)
	{
		if ((type == null) || (syncId == Guid.Empty))
		{
			return null;
		}

		if (_applyDatabase != null)
		{
			return GetRelatedBySyncId(_applyDatabase, type, syncId);
		}

		using var database = GetDatabase();
		return database.GetSyncableRepository(type)?.Read(syncId);
	}

	/// <summary>
	/// Read a row by SyncId. See <see cref="FindBySyncId(Type, Guid)" />.
	/// </summary>
	public T FindBySyncId<T>(Guid syncId) where T : class, ISyncEntity
	{
		return FindBySyncId(typeof(T), syncId) as T;
	}

	/// <summary>
	/// Opens a new database instance (a new SQLite connection). Do not call this
	/// per apply row; use <see cref="FindBySyncId(Type, Guid)" /> during apply.
	/// </summary>
	public ISyncableDatabase GetDatabase()
	{
		return DatabaseProvider.GetSyncableDatabase();
	}

	/// <summary>
	/// Opens a new database instance. See <see cref="GetDatabase()" />.
	/// </summary>
	public T GetDatabase<T>() where T : class, ISyncableDatabase
	{
		return (T) DatabaseProvider.GetSyncableDatabase();
	}

	/// <summary>
	/// Get the local primary key for an entity by SyncId.
	/// KeyCache is checked first; a miss reads by SyncId (lookup filters are for this-row identity, not FK resolution).
	/// During apply, the open database is reused so related rows are not loaded on a second connection.
	/// </summary>
	public TKey GetEntityPrimaryKey<T, TKey>(Guid syncId)
		where T : SyncEntity<TKey>, new()
	{
		if (syncId == Guid.Empty)
		{
			return default;
		}

		var type = typeof(T);
		var cache = DatabaseProvider.KeyCache;
		if (cache?.GetEntityId(type, syncId) is TKey cached)
		{
			return cached;
		}

		if ((Converter != null) && !Converter.CanConvertOutgoing(type.ToAssemblyName()))
		{
			return default;
		}

		ISyncEntity found;
		if (_applyDatabase != null)
		{
			found = GetRelatedBySyncId(_applyDatabase, type, syncId);
		}
		else
		{
			using var database = GetDatabase();
			var repository = database.GetSyncableRepository(type);
			found = repository?.Read(syncId);
		}

		if (found is not T typed || EqualityComparer<TKey>.Default.Equals(typed.Id, default))
		{
			return default;
		}

		cache?.AddEntityId(type, syncId, typed.Id);
		return typed.Id;
	}

	protected internal override ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes)
	{
		ValidateSession(sessionId);
		return ApplyChanges(changes, false);
	}

	/// <summary>
	/// Apply issue-driven corrections. Same pipeline as ApplyChanges with last-write-wins skipped.
	/// </summary>
	protected internal override ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections)
	{
		ValidateSession(sessionId);
		return ApplyChanges(corrections, true);
	}

	/// <summary>
	/// Count outgoing changes without adding them to statistics.
	/// </summary>
	protected internal int CountOutgoingChanges(Guid sessionId, SyncRequest request)
	{
		ValidateSession(sessionId);

		if (request.Since == request.Until)
		{
			request.Until = DateTimeProvider.UtcNow;
		}

		using var database = DatabaseProvider.GetSyncableDatabase();
		database.Profiler = Profiler;
		return GetChangeCount(database, request).Total;
	}

	/// <summary>
	/// Gets the changes from this client.
	/// </summary>
	protected internal override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
	{
		ValidateSession(sessionId);

		using var getChanges = Profiler.Start(nameof(GetChanges));

		// if the [since] and [until] are equal that means we should get all changes from since to now
		if (request.Since == request.Until)
		{
			request.Until = DateTimeProvider.UtcNow;
		}

		var take = (request.Take <= 0) || (request.Take > SyncSettings.ItemsPerSyncRequest) ? SyncSettings.ItemsPerSyncRequest : request.Take;
		using var database = DatabaseProvider.GetSyncableDatabase();
		database.Profiler = Profiler;
		var included = GetChangeCount(database, request);
		var response = new ServiceResult<SyncObject>
		{
			Skipped = request.Skip,
			TotalCount = included.Total
		};
		if (response.TotalCount == 0)
		{
			return response;
		}

		var remainingSkip = request.Skip;
		foreach (var entry in included.Repositories)
		{
			if (entry.Count <= remainingSkip)
			{
				remainingSkip -= entry.Count;
				continue;
			}

			var remainingTake = take - response.Collection.Count;
			if (remainingTake <= 0)
			{
				break;
			}

			IEnumerable<ISyncEntity> changes;
			using (Profiler.Start("GetChangesQuery"))
			{
				changes = entry.Repository.GetChanges(
					request.Since,
					request.Until,
					remainingSkip,
					remainingTake,
					SyncSettings.GetFilter(entry.Repository));
			}

			using (Profiler.Start("ConvertOutgoing"))
			{
				foreach (var entity in changes)
				{
					response.Collection.Add(Converter?.ConvertOutgoing(this, entity));
				}
			}

			remainingSkip = 0;
			if (response.Collection.Count >= take)
			{
				break;
			}
		}

		Statistics.Changes += response.Collection.Count;
		return response;
	}

	/// <summary>
	/// Placeholder for issue-driven outgoing objects. Returns an empty collection.
	/// Override when a client has a real repair path.
	/// </summary>
	protected internal override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
	{
		ValidateSession(sessionId);
		return new ServiceResult<SyncObject>();
	}

	private (int Total, List<(ISyncableRepository Repository, int Count)> Repositories) GetChangeCount(
		ISyncableDatabase database,
		SyncRequest request)
	{
		using var getChangeCount = Profiler.Start("GetChangeCount");
		if (request.Skip == 0)
		{
			_changeCountCache = null;
		}

		var repositories = new List<(ISyncableRepository Repository, int Count)>();
		if (_changeCountCache is { } cached
			&& (cached.Since == request.Since)
			&& (cached.Until == request.Until))
		{
			var byName = new Dictionary<string, ISyncableRepository>();
			foreach (var repository in database.GetSyncableRepositories())
			{
				byName[repository.TypeName] = repository;
			}

			var total = 0;
			foreach (var (typeName, count) in cached.Counts)
			{
				if (!byName.TryGetValue(typeName, out var repository))
				{
					continue;
				}

				repositories.Add((repository, count));
				total += count;
			}

			return (total, repositories);
		}

		var counts = new List<(string TypeName, int Count)>();
		var counted = 0;
		foreach (var repository in database.GetSyncableRepositories())
		{
			if (SyncSettings.ShouldExcludeRepository(repository.TypeName)
				|| ((Converter != null) && !Converter.CanConvertOutgoing(repository.TypeName)))
			{
				continue;
			}

			var count = repository.GetChangeCount(request.Since, request.Until, SyncSettings.GetFilter(repository));
			repositories.Add((repository, count));
			counts.Add((repository.TypeName, count));
			counted += count;
		}

		_changeCountCache = (request.Since, request.Until, counts);
		return (counted, repositories);
	}

	private ServiceResult<SyncIssue> ApplyChanges(ServiceRequest<SyncObject> changes, bool corrections)
	{
		using var applyChanges = Profiler.Start(nameof(ApplyChanges));

		// The collection is incoming types
		// todo: performance, could we increase performance by going straight to entity,
		//  currently we convert to entity then back to sync object
		// The only issue is processing entities individually. If an entity is added to a context then
		// something goes wrong we'll need to disconnect before processing them individually
		var groups = changes.Collection
			.Where(x => !x.Equals(SyncObjectExtensions.Empty))
			.GroupBy(x => x.TypeName)
			.OrderBy(x => x.Key);

		if (DatabaseProvider.Settings.SyncOrder.Any())
		{
			_syncOrder ??= DatabaseProvider.Settings.SyncOrder
				.SelectMany(pair => new[] { pair.sync, pair.entity })
				.ToList();

			groups = groups
				.OrderBy(g => _syncOrder.Contains(g.Key)
					? _syncOrder.IndexOf(g.Key)
					: int.MaxValue
				)
				.ThenBy(g => g.Key);
		}

		var response = new ServiceResult<SyncIssue> { Collection = new List<SyncIssue>() };
		groups.ForEach(x => ProcessSyncObjects(DatabaseProvider, x.Where(y => y.Status != SyncObjectStatus.Deleted), response.Collection, corrections));
		groups.Reverse().ForEach(x => ProcessSyncObjects(DatabaseProvider, x.Where(y => y.Status == SyncObjectStatus.Deleted), response.Collection, corrections));
		response.TotalCount = response.Collection.Count;
		return response;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Sync entity types are source-reflected and kept at runtime.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Related Id is a public property on source-reflected sync entities.")]
	private static Relationship[] BuildRelationships(Type type, ISyncableDatabase database)
	{
		var repositoryTypes = database.GetSyncableRepositories().Select(x => x.RealType).ToArray();
		var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		var relationships = new List<Relationship>();

		foreach (var syncIdProperty in properties)
		{
			if ((syncIdProperty.Name == nameof(ISyncEntity.SyncId))
				|| !syncIdProperty.Name.EndsWith("SyncId", StringComparison.Ordinal))
			{
				continue;
			}

			var propertyType = Nullable.GetUnderlyingType(syncIdProperty.PropertyType) ?? syncIdProperty.PropertyType;
			if (propertyType != typeof(Guid))
			{
				continue;
			}

			var prefix = syncIdProperty.Name[..^"SyncId".Length];
			if (string.IsNullOrEmpty(prefix))
			{
				continue;
			}

			var idProperty = properties.FirstOrDefault(x => x.Name == (prefix + "Id"));
			if ((idProperty == null) || !idProperty.CanWrite)
			{
				continue;
			}

			var relatedType = ResolveRelatedType(prefix, type, properties, repositoryTypes);
			if (relatedType == null)
			{
				continue;
			}

			var relatedIdProperty = relatedType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
			if (relatedIdProperty == null)
			{
				continue;
			}

			relationships.Add(new Relationship
			{
				EntityIdPropertyInfo = idProperty,
				EntitySyncIdPropertyInfo = syncIdProperty,
				RelatedIdPropertyInfo = relatedIdProperty,
				Type = relatedType
			});
		}

		return relationships.ToArray();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Related types are sync entities registered with SourceReflector.")]
	private ISyncEntity GetRelatedBySyncId(ISyncableDatabase database, Type relatedType, Guid syncId)
	{
		var typeName = relatedType.ToAssemblyName();
		var key = (typeName, syncId);
		if (_relatedBySyncId is { } cache && cache.TryGetValue(key, out var cached))
		{
			return cached;
		}

		var repository = database.GetSyncableRepository(relatedType);
		var found = repository?.Read(syncId);
		_relatedBySyncId?.TryAdd(key, found);
		return found;
	}

	private static Guid? GetRelatedSyncId(ISyncEntity entity, Relationship relationship)
	{
		var syncIdValue = relationship.EntitySyncIdPropertyInfo.GetValue(entity);
		var syncId = syncIdValue switch
		{
			Guid guid => guid,
			null => null,
			_ => (Guid?) syncIdValue
		};

		if (syncId is not { } relatedSyncId || (relatedSyncId == Guid.Empty))
		{
			return null;
		}

		return relatedSyncId;
	}

	private Relationship[] GetRelationshipConfigurations(Type entityType, ISyncableDatabase database)
	{
		return _relationshipCache.GetOrAdd(entityType, type => BuildRelationships(type, database));
	}

	/// <summary>
	/// Process the sync object.
	/// </summary>
	/// <returns> True if the sync object was processed otherwise false. </returns>
	private bool ProcessSyncObject(SyncObject syncObject, ISyncableDatabase database, ICollection<SyncIssue> issues, bool correction, bool isIndividualProcess)
	{
		using var process = Profiler.Start(nameof(ProcessSyncObject));

		Logger?.Write(LogLevel.Debug, SyncSessionStart?.Id ?? Guid.Empty, correction
				? $"Processing sync object correction {syncObject.SyncId} {syncObject.TypeName}."
				: $"Processing sync object {syncObject.SyncId} {syncObject.TypeName}.",
			DateTimeProvider.UtcNow
		);

		if (Converter == null)
		{
			issues.Add(new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.UpdateException,
				Message = "A sync converter is required.",
				TypeName = syncObject.TypeName
			});
			return false;
		}

		ISyncEntity syncEntity;
		using (Profiler.Start("ConvertIncoming"))
		{
			syncEntity = Converter.ConvertIncoming(this, syncObject);
		}

		if (syncEntity == null)
		{
			issues.Add(new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.UpdateException,
				Message = "Failed to convert the incoming sync object.",
				TypeName = syncObject.TypeName
			});
			return false;
		}

		syncEntity.ModifiedOn = syncObject.ModifiedOn;

		if (SyncSettings.ShouldExcludeRepository(syncEntity.GetRealType()))
		{
			var issue = new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.RepositoryFiltered,
				Message = "The item is not being processed because this repository not syncable.",
				TypeName = syncObject.TypeName
			};
			issues.Add(issue);
			Logger?.Write(LogLevel.Debug, SyncSessionStart?.Id ?? Guid.Empty, issue.Message, DateTimeProvider.UtcNow);
			return false;
		}

		// Scope filters compare local *Id. Resolve *SyncId before the keep-test.
		// A missing parent stays at the default so the keep-test can reject the row.
		UpdateLocalRelationships(syncEntity, database, enforceMissing: false);

		if (RejectIfIncomingFiltered(syncEntity, syncObject, issues))
		{
			return false;
		}

		if (((syncObject.Status == SyncObjectStatus.Added) || (syncObject.Status == SyncObjectStatus.Updated))
			&& RejectIfRelatedIncomingFiltered(syncEntity, syncObject, issues, database))
		{
			return false;
		}

		var type = syncEntity.GetRealType();
		var repository = database.GetSyncableRepository(type);

		if (repository == null)
		{
			throw new InvalidDataException("Failed to find a syncable repository for the entity.");
		}

		var syncRepositoryFilter = SyncSettings.GetFilter(repository);
		ISyncEntity foundEntity;
		using (Profiler.Start($"{nameof(ProcessSyncObject)}ReadEntity"))
		{
			//
			// Check to see if primary key caching is enabled and is never expiring for a client
			// This combination of state means we are caching all keys for a local client to reduce
			// the amount of database access.
			//
			// NOTE: This means the database MUST cache all primary keys as they are stored. If the
			// database fails to update the cache manager then this would result in processing of
			// sync items individually which could destroy performance.
			//
			// Disable caching when running "individual" processing just in case there is caching issues.
			// Disable caching if the repository is using a different lookup filter because matching could be using a different "sync lookup key"
			//  - todo: change key cache to add a "GetEntitySyncId" (see GetEntityId) method, this way we could cache on any lookup key
			// Disable caching if the cache does not support the sync entity type
			//
			var doesNotHaveLookupFilter = syncRepositoryFilter?.HasLookupFilter != true;
			if (doesNotHaveLookupFilter
				&& !isIndividualProcess
				&& !IsServerClient
				&& (database.KeyCache?.SupportsType(type) == true))
			{
				var id = database.KeyCache.GetEntityId(syncEntity);
				if (id == null)
				{
					// The ID was not found so the entity is to believed to not exist.
					var readEntity = repository.Read(syncObject.SyncId);
					if (readEntity != null)
					{
						database.KeyCache.AddEntity(readEntity);
						foundEntity = readEntity;
					}
					else
					{
						foundEntity = null;
					}
				}
				else
				{
					// Id was found so let's read the entity by the primary key
					var readEntity = repository.ReadByPrimaryId(id);
					if ((readEntity != null) && (readEntity.SyncId == syncEntity.SyncId))
					{
						// The entity was found so return it by ID.
						foundEntity = readEntity;
					}
					else
					{
						foundEntity = doesNotHaveLookupFilter
							? repository.Read(syncObject.SyncId)
							: repository.Read(syncEntity, syncRepositoryFilter);
					}
				}
			}
			else
			{
				foundEntity = doesNotHaveLookupFilter
					? repository.Read(syncObject.SyncId)
					: repository.Read(syncEntity, syncRepositoryFilter);
			}
		}

		if (foundEntity != null)
		{
			if (RejectIfIncomingFiltered(foundEntity, syncObject, issues))
			{
				return false;
			}
		}
		else if (syncRepositoryFilter?.HasLookupFilter == true)
		{
			// Lookup missed. Use the SyncId row when it exists and is in
			// scope (update, not a second insert). Out of scope is filtered.
			var bySyncId = repository.Read(syncObject.SyncId);
			if (RejectIfIncomingFiltered(bySyncId, syncObject, issues))
			{
				return false;
			}

			foundEntity = bySyncId;
		}

		var syncStatus = syncObject.Status;

		if ((foundEntity != null) && (syncObject.Status == SyncObjectStatus.Added))
		{
			syncStatus = SyncObjectStatus.Updated;
		}
		else if ((foundEntity == null) && (syncObject.Status == SyncObjectStatus.Updated))
		{
			syncStatus = SyncObjectStatus.Added;
		}

		if (syncEntity.IsDeleted && (syncStatus != SyncObjectStatus.Deleted))
		{
			syncStatus = SyncObjectStatus.Deleted;
		}

		switch (syncStatus)
		{
			case SyncObjectStatus.Added:
			{
				using var added = Profiler.Start($"{nameof(ProcessSyncObject)}Added");

				// Instantiate a new instance of the sync entity to update, also use the provided sync ID
				// this is because it's possibly the sync entity is blocking updating of the sync ID so it 
				// will need to be set manually being that it will be filtered on update.
				foundEntity = (ISyncEntity) SourceReflector.CreateInstance(SourceReflector.GetSourceType(syncEntity));
				if (foundEntity == null)
				{
					throw new SyncIssueException(SyncIssueType.Unknown, "Failed to create a new instance.");
				}

				foundEntity.SyncId = syncObject.SyncId;

				if (UpdateEntity(syncObject, syncEntity, foundEntity, syncStatus, issues, database))
				{
					repository.Add(foundEntity);
					return true;
				}

				repository.Discard(foundEntity);
				return false;
			}
			case SyncObjectStatus.Updated:
			{
				using var modified = Profiler.Start($"{nameof(ProcessSyncObject)}Modified");

				if ((foundEntity == null)
					|| ((foundEntity.ModifiedOn >= syncEntity.ModifiedOn)
						&& !correction))
				{
					// Did not find the entity, or it has not changed.
					return false;
				}

				if (!UpdateEntity(syncObject, syncEntity, foundEntity, syncStatus, issues, database))
				{
					repository.Discard(foundEntity);
					return false;
				}

				return true;
			}
			case SyncObjectStatus.Deleted:
			{
				using var deleted = Profiler.Start($"{nameof(ProcessSyncObject)}Deleted");

				if (foundEntity == null)
				{
					if (SyncSettings.PermanentDeletions)
					{
						return false;
					}

					foundEntity = (ISyncEntity) SourceReflector.CreateInstance(SourceReflector.GetSourceType(syncEntity));
					foundEntity.SyncId = syncObject.SyncId;
					repository.Add(foundEntity);
				}
				else if (IsServerClient
					&& (foundEntity.ModifiedOn >= syncEntity.ModifiedOn)
					&& !correction)
				{
					return false;
				}

				if (!UpdateEntity(syncObject, syncEntity, foundEntity, syncStatus, issues, database))
				{
					repository.Discard(foundEntity);
					return false;
				}

				if (SyncSettings.PermanentDeletions)
				{
					repository.Remove(foundEntity);
					return true;
				}

				foundEntity.IsDeleted = true;
				return true;
			}
			default:
			{
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void ProcessSyncObjects(ISyncableDatabaseProvider provider, IEnumerable<SyncObject> syncObjects, ICollection<SyncIssue> issues, bool corrections)
	{
		List<SyncObject> objects;
		using (Profiler.Start(nameof(ProcessSyncObjects) + "SyncObjectsToList"))
		{
			objects = syncObjects.ToList();
		}

		if (objects.Count <= 0)
		{
			return;
		}

		using var process = Profiler.Start(nameof(ProcessSyncObjects));
		try
		{
			ISyncableDatabase database;
			using (Profiler.Start(nameof(ProcessSyncObjects) + "GetDatabase"))
			{
				database = provider.GetSyncableDatabase();
				database.Profiler = Profiler;
				database.DatabaseSettings.MaintainCreatedOn = false;
				database.DatabaseSettings.MaintainModifiedOn = IsServerClient;
			}

			try
			{
				var changes = 0;
				_applyDatabase = database;
				_relatedBySyncId = new Dictionary<(string TypeName, Guid SyncId), ISyncEntity>();

				for (var i = 0; i < objects.Count; i++)
				{
					if (ProcessSyncObject(objects[i], database, issues, corrections, false))
					{
						changes++;
					}
				}

				// Save the whole group in one call. Do not SaveChanges per row.
				// Per-row saves turn a bulk request into one transaction per item
				// and destroy sync performance. Parent local keys are set in the
				// entity update (GetEntityPrimaryKey: KeyCache, then one filtered
				// repository read) before this save.
				using (Profiler.Start(nameof(ProcessSyncObjects) + "SaveDatabase"))
				{
					database.SaveChanges();
				}

				if (corrections)
				{
					Statistics.AppliedCorrections += changes;
				}
				else
				{
					Statistics.AppliedChanges += changes;
				}
			}
			finally
			{
				_applyDatabase = null;
				_relatedBySyncId = null;
				database.Dispose();
			}
		}
		catch
		{
			Statistics.IndividualProcessCount++;
			Logger?.Write(LogLevel.Warning, SyncSessionStart?.Id ?? Guid.Empty, "Failed to process sync objects in the batch.", DateTimeProvider.UtcNow);
			ProcessSyncObjectsIndividually(provider, objects, issues, corrections);
		}
	}

	private void ProcessSyncObjectsIndividually(ISyncableDatabaseProvider provider, IEnumerable<SyncObject> syncObjects, ICollection<SyncIssue> issues, bool corrections)
	{
		using var individually = Profiler.Start(nameof(ProcessSyncObjectsIndividually));
		var objects = syncObjects.ToList();

		foreach (var syncObject in objects)
		{
			try
			{
				ISyncableDatabase database;
				using (Profiler.Start($"{nameof(ProcessSyncObjectsIndividually)}GetDatabase"))
				{
					database = provider.GetSyncableDatabase();
					database.Profiler = Profiler;
					database.DatabaseSettings.MaintainCreatedOn = false;
					database.DatabaseSettings.MaintainModifiedOn = IsServerClient;
				}

				using (database)
				{
					_applyDatabase = database;
					_relatedBySyncId = new Dictionary<(string TypeName, Guid SyncId), ISyncEntity>();
					try
					{
						if (!ProcessSyncObject(syncObject, database, issues, corrections, true))
						{
							continue;
						}
					}
					finally
					{
						_applyDatabase = null;
						_relatedBySyncId = null;
					}

					using (Profiler.Start($"{nameof(ProcessSyncObjectsIndividually)}SaveDatabase"))
					{
						database.SaveChanges();
					}

					if (corrections)
					{
						Statistics.AppliedCorrections++;
					}
					else
					{
						Statistics.AppliedChanges++;
					}
				}
			}
			catch (SyncIssueException ex)
				{
					ex.Issues.ForEach(issues.Add);

					var issue = new SyncIssue
					{
						Id = syncObject.SyncId,
						IssueType = ex.IssueType,
						Message = ex.Message,
						TypeName = syncObject.TypeName
					};

					if (SyncSettings.IncludeIssueDetails)
					{
						issue.Message += Environment.NewLine + ex.ToDetailedString();
					}

					issues.Add(issue);
				}
				catch (InvalidConstraintException ex)
				{
					var issue = new SyncIssue
					{
						Id = syncObject.SyncId,
						IssueType = SyncIssueType.ConstraintException,
						Message = "Invalid constraint exception...",
						TypeName = syncObject.TypeName
					};

					if (SyncSettings.IncludeIssueDetails)
					{
						issue.Message += Environment.NewLine + ex.ToDetailedString();
					}

					issues.Add(issue);
				}
				catch (InvalidOperationException ex)
				{
					var issue = new SyncIssue
					{
						Id = syncObject.SyncId,
						IssueType = SyncIssueType.RelationshipConstraint,
						Message = "Invalid operation exception...",
						TypeName = syncObject.TypeName
					};

					if (SyncSettings.IncludeIssueDetails)
					{
						issue.Message += Environment.NewLine + ex.ToDetailedString();
					}

					issues.Add(issue);
				}
				catch (ValidationException ex)
				{
					var issue = new SyncIssue
					{
						Id = syncObject.SyncId,
						IssueType = SyncIssueType.ValidationException,
						Message = ex.Message,
						TypeName = syncObject.TypeName
					};

					if (SyncSettings.IncludeIssueDetails)
					{
						issue.Message += Environment.NewLine + ex.ToDetailedString();
					}

					issues.Add(issue);
				}
				catch (Exception ex)
				{
					var details = ex.ToDetailedString();

					// Cannot catch the DbUpdateException without reference EntityFramework.
					var issue = details.Contains("conflicted with the FOREIGN KEY constraint")
						|| details.Contains("The DELETE statement conflicted with the REFERENCE constraint")
							? new SyncIssue
							{
								Id = syncObject.SyncId,
								IssueType = SyncIssueType.RelationshipConstraint,
								Message = "This entity has relationship issue with another entity.",
								TypeName = syncObject.TypeName
							}
							: new SyncIssue
							{
								Id = syncObject.SyncId,
								IssueType = SyncIssueType.Unknown,
								Message = "Unknown issue...",
								TypeName = syncObject.TypeName
							};

					if (SyncSettings.IncludeIssueDetails)
					{
						issue.Message += Environment.NewLine + ex.ToDetailedString();
					}

					issues.Add(issue);
				}
		}
	}

	private ISyncEntity ReadRelatedEntity(ISyncableDatabase database, Type relatedType, Guid syncId)
	{
		var found = GetRelatedBySyncId(database, relatedType, syncId);
		if ((found != null) && SyncSettings.ShouldFilterIncomingEntity(relatedType.ToAssemblyName(), found))
		{
			return null;
		}

		return found;
	}

	/// <summary>
	/// Payload fields can be rewritten to pass the session incoming filter. The stored row cannot.
	/// </summary>
	private bool RejectIfIncomingFiltered(ISyncEntity entity, SyncObject syncObject, ICollection<SyncIssue> issues)
	{
		if ((entity == null) || !SyncSettings.ShouldFilterIncomingEntity(entity.GetRealType().ToAssemblyName(), entity))
		{
			return false;
		}

		var issue = new SyncIssue
		{
			Id = syncObject.SyncId,
			IssueType = SyncIssueType.SyncEntityFiltered,
			Message = "The item is not being processed because the sync entity is being filtered.",
			TypeName = syncObject.TypeName
		};
		issues.Add(issue);
		Logger?.Write(LogLevel.Debug, SyncSessionStart?.Id ?? Guid.Empty, issue.Message, DateTimeProvider.UtcNow);
		return true;
	}

	/// <summary>
	/// Incoming *SyncId can name a stored related row that fails that type's
	/// apply keep-test (scope and/or incoming). Reject after convert, before
	/// the destination row is loaded or updated.
	/// </summary>
	private bool RejectIfRelatedIncomingFiltered(
		ISyncEntity incoming,
		SyncObject syncObject,
		ICollection<SyncIssue> issues,
		ISyncableDatabase database
	)
	{
		foreach (var relationship in GetRelationshipConfigurations(incoming.GetRealType(), database))
		{
			var relatedSyncId = GetRelatedSyncId(incoming, relationship);
			if (relatedSyncId == null)
			{
				continue;
			}

			var repository = database.GetSyncableRepository(relationship.Type);
			if (repository == null)
			{
				continue;
			}

			var filter = SyncSettings.GetFilter(repository);
			if (filter?.HasApplyKeepTest != true)
			{
				continue;
			}

			var typeName = relationship.Type.ToAssemblyName();
			var related = GetRelatedBySyncId(database, relationship.Type, relatedSyncId.Value);
			if ((related == null) || !SyncSettings.ShouldFilterIncomingEntity(typeName, related))
			{
				continue;
			}

			issues.Add(new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.RelationshipConstraint,
				Message = "The related entity is not being processed because the sync entity is being filtered.",
				TypeName = syncObject.TypeName
			});
			return true;
		}

		return false;
	}

	private static Type ResolveRelatedType(string prefix, Type ownerType, PropertyInfo[] properties, Type[] repositoryTypes)
	{
		var navigation = properties.FirstOrDefault(x => x.Name == prefix);
		if ((navigation != null) && typeof(ISyncEntity).IsAssignableFrom(navigation.PropertyType))
		{
			return navigation.PropertyType;
		}

		if (prefix == "Parent")
		{
			return ownerType;
		}

		Type suffixMatch = null;
		foreach (var type in repositoryTypes)
		{
			var name = type.Name;
			if ((name == prefix)
				|| (name == (prefix + "Entity"))
				|| (name == ("Client" + prefix))
				|| (name == ("Client" + prefix + "Entity")))
			{
				return type;
			}

			if (name.EndsWith(prefix + "Entity", StringComparison.Ordinal)
				|| ((name != prefix) && name.EndsWith(prefix, StringComparison.Ordinal)))
			{
				if (suffixMatch != null)
				{
					return null;
				}

				suffixMatch = type;
			}
		}

		return suffixMatch;
	}

	private static void SetRelationshipId(PropertyInfo idProperty, ISyncEntity entity, object value)
	{
		if (value != null)
		{
			idProperty.SetValue(entity, value);
			return;
		}

		var type = idProperty.PropertyType;
		if ((Nullable.GetUnderlyingType(type) != null) || !type.IsValueType)
		{
			idProperty.SetValue(entity, null);
			return;
		}

		if (type == typeof(int))
		{
			idProperty.SetValue(entity, 0);
			return;
		}

		if (type == typeof(long))
		{
			idProperty.SetValue(entity, 0L);
			return;
		}

		idProperty.SetValue(entity, null);
	}

	private bool UpdateEntity(SyncObject syncObject, ISyncEntity syncEntity, ISyncEntity foundEntity, SyncObjectStatus status, ICollection<SyncIssue> issues, ISyncableDatabase database)
	{
		using var updateEntity = Profiler.Start("UpdateEntity");
		try
		{
			var converter = Converter;
			if (converter == null)
			{
				issues.Add(new SyncIssue
				{
					Id = syncObject.SyncId,
					IssueType = SyncIssueType.UpdateException,
					Message = "A sync converter is required.",
					TypeName = syncObject.TypeName
				});
				return false;
			}

			if (!converter.Update(this, syncEntity, foundEntity, status))
			{
				issues.Add(new SyncIssue
				{
					Id = syncObject.SyncId,
					IssueType = SyncIssueType.UpdateException,
					Message = "The converter failed to update the entity.",
					TypeName = syncObject.TypeName
				});
				return false;
			}

			// Incoming update does not copy CreatedOn or ModifiedOn (EverythingExceptSyncUpdate).
			// Always take ModifiedOn. Copy CreatedOn only when this row has none, so a
			// delete for a SyncId this database has never stored keeps the sender's time
			// instead of DateTime.MinValue. A later update does not replace a real value.
			foundEntity.ModifiedOn = syncEntity.ModifiedOn;
			if ((foundEntity.CreatedOn == DateTime.MinValue) && (syncEntity.CreatedOn != DateTime.MinValue))
			{
				foundEntity.CreatedOn = syncEntity.CreatedOn;
			}

			using (Profiler.Start("UpdateLocalRelationships"))
			{
				UpdateLocalRelationships(foundEntity, database);
			}

			return true;
		}
		catch (SyncIssueException ex)
		{
			ex.Issues.ForEach(issues.Add);
			issues.Add(new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = ex.IssueType,
				Message = ex.Message,
				TypeName = syncObject.TypeName
			});
			return false;
		}
		catch (SyncUpdateException ex)
		{
			issues.Add(new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.UpdateException,
				Message = ex.Message,
				TypeName = syncObject.TypeName
			});
			return false;
		}
	}

	private bool TrySetRelationshipId(ISyncEntity entity, Relationship relationship, ISyncableDatabase database, Guid relatedSyncId)
	{
		var relatedRepository = database.GetSyncableRepository(relationship.Type);
		if ((relatedRepository != null) && !IsServerClient)
		{
			var relatedFilter = SyncSettings.GetFilter(relatedRepository);
			if (relatedFilter?.HasApplyKeepTest != true)
			{
				var cachedId = database.KeyCache?.GetEntityId(relationship.Type, relatedSyncId);
				if (cachedId != null)
				{
					SetRelationshipId(relationship.EntityIdPropertyInfo, entity, cachedId);
					return true;
				}
			}
		}

		var found = ReadRelatedEntity(database, relationship.Type, relatedSyncId);
		if (found == null)
		{
			return false;
		}

		var id = relationship.RelatedIdPropertyInfo.GetValue(found);
		SetRelationshipId(relationship.EntityIdPropertyInfo, entity, id);
		database.KeyCache?.AddEntityId(relationship.Type, relatedSyncId, id);
		return true;
	}

	/// <summary>
	/// Set local *Id values from *SyncId. Related rows are Read(syncId), not a lookup filter.
	/// A keep-test miss (scope and/or incoming) is treated as missing.
	/// When <paramref name="enforceMissing" /> is false, a missing required id stays at its default.
	/// </summary>
	private void UpdateLocalRelationships(ISyncEntity entity, ISyncableDatabase database, bool enforceMissing = true)
	{
		var issues = new List<SyncIssue>();
		var entityType = entity.GetRealType();

		foreach (var relationship in GetRelationshipConfigurations(entityType, database))
		{
			var relatedSyncId = GetRelatedSyncId(entity, relationship);
			if (relatedSyncId == null)
			{
				SetRelationshipId(relationship.EntityIdPropertyInfo, entity, null);
				continue;
			}

			if (TrySetRelationshipId(entity, relationship, database, relatedSyncId.Value))
			{
				continue;
			}

			if ((Nullable.GetUnderlyingType(relationship.EntityIdPropertyInfo.PropertyType) != null) || !enforceMissing)
			{
				SetRelationshipId(relationship.EntityIdPropertyInfo, entity, null);
				continue;
			}

			issues.Add(new SyncIssue
			{
				Id = relatedSyncId.Value,
				IssueType = SyncIssueType.RelationshipConstraint,
				Message = "Failed to find the relational entity.",
				TypeName = relationship.Type.ToAssemblyName()
			});
		}

		if (issues.Count > 0)
		{
			throw new SyncIssueException(SyncIssueType.RelationshipConstraint,
				"This entity has relationship issues.",
				issues.ToArray());
		}
	}

	#endregion

	#region Classes

	private sealed class Relationship
	{
		#region Properties

		public PropertyInfo EntityIdPropertyInfo { get; set; }

		public PropertyInfo EntitySyncIdPropertyInfo { get; set; }

		public PropertyInfo RelatedIdPropertyInfo { get; set; }

		public Type Type { get; set; }

		#endregion
	}

	#endregion
}