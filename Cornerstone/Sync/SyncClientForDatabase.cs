#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Profiling;
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

	private static List<string> _syncOrder;

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
	/// Sends changes to a server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="changes"> The changes to write to the server. </param>
	/// <returns> A list of sync issues if there were any. </returns>
	public override ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes)
	{
		return ApplyChanges(changes, false);
	}

	/// <summary>
	/// Sends issue corrections to a server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="corrections"> The corrections to write to the server. </param>
	/// <returns> A list of sync issues if there were any. </returns>
	public override ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections)
	{
		return ApplyChanges(corrections, true);
	}

	/// <summary>
	/// Gets the changes from the server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="request"> The details for the request. </param>
	/// <returns> The list of changes from the server. </returns>
	public override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
	{
		ValidateSession(sessionId);

		var response = new ServiceResult<SyncObject>
		{
			Skipped = request.Skip,
			TotalCount = Profiler.Time(nameof(GetChangeCount), () => GetChangeCount(request))
		};

		if (response.TotalCount <= 0)
		{
			return response;
		}

		return Profiler.Time(nameof(GetChanges), () =>
		{
			// if the [since] and [until] are equal that means we should get all changes from since to now
			if (request.Since == request.Until)
			{
				request.Until = DateTimeProvider.UtcNow;
			}

			var take = (request.Take <= 0) || (request.Take > SyncSettings.ItemsPerSyncRequest) ? SyncSettings.ItemsPerSyncRequest : request.Take;
			var remainingSkip = request.Skip;
			using var database = DatabaseProvider.GetSyncableDatabase();

			foreach (var repository in database.GetSyncableRepositories())
			{
				// Skip this type if it's being filters or if the outgoing converter cannot convert
				if (!SyncSettings.ShouldSyncRepository(repository.TypeName)
					|| ((Converter != null) && !Converter.CanConvertOutgoing(repository.TypeName)))
				{
					// Do not process this repository because we have filters and the repository is not in the filters.
					continue;
				}

				var syncRepositoryFilter = SyncSettings.GetFilter(repository);

				// Check to see if this repository should be skipped
				var changeCount = repository.GetChangeCount(request.Since, request.Until, syncRepositoryFilter);
				if (changeCount <= remainingSkip)
				{
					// this repo changes was processed in a previous GetChanges request
					remainingSkip -= changeCount;
					continue;
				}

				var changes = repository.GetChanges(request.Since, request.Until, remainingSkip, take - response.Collection.Count, syncRepositoryFilter).ToList();
				var items = changes.Select(x => Converter?.ConvertOutgoing(this, x)).ToList();

				response.Collection.AddRange(items);
				remainingSkip = 0;

				if (response.Collection.Count >= take)
				{
					// We have filled up the response so time to return
					break;
				}
			}

			Statistics.Changes += response.Collection.Count;

			return response;
		});
	}

	/// <summary>
	/// Gets the list of sync objects to try and resolve the issue list.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="issues"> The issues to process. </param>
	/// <returns> The sync objects to resolve the issues. </returns>
	public override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
	{
		ValidateSession(sessionId);

		var response = new ServiceResult<SyncObject>();
		return response;
	}

	/// <summary>
	/// Gets an instance of the database this sync client is for.
	/// </summary>
	/// <returns> The database that is syncable. </returns>
	public ISyncableDatabase GetDatabase()
	{
		return DatabaseProvider.GetSyncableDatabase();
	}

	/// <summary>
	/// Gets an instance of the database this sync client is for.
	/// </summary>
	/// <returns> The database that is syncable. </returns>
	public T GetDatabase<T>() where T : class, ISyncableDatabase
	{
		return (T) DatabaseProvider.GetSyncableDatabase();
	}

	private ServiceResult<SyncIssue> ApplyChanges(ServiceRequest<SyncObject> changes, bool corrections)
	{
		return Profiler.Time(nameof(ApplyChanges), () =>
		{
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
			if (SyncSettings.PermanentDeletions)
			{
				groups.ForEach(x => ProcessSyncObjects(DatabaseProvider, x.Where(y => y.Status != SyncObjectStatus.Deleted), response.Collection, corrections));
				groups.Reverse().ForEach(x => ProcessSyncObjects(DatabaseProvider, x.Where(y => y.Status == SyncObjectStatus.Deleted), response.Collection, corrections));
			}
			else
			{
				groups.ForEach(x => ProcessSyncObjects(DatabaseProvider, x, response.Collection, corrections));
			}
			response.TotalCount = response.Collection.Count;
			return response;
		});
	}

	private int GetChangeCount(SyncRequest request)
	{
		using var database = DatabaseProvider.GetSyncableDatabase();
		var repositories = database.GetSyncableRepositories().ToList();
		var changeCount = repositories
			.Sum(repository =>
			{
				// Skip this type if it's being filters or if the outgoing converter cannot convert
				if (!SyncSettings.ShouldSyncRepository(repository.TypeName)
					|| ((Converter != null) && !Converter.CanConvertOutgoing(repository.TypeName)))
				{
					// Do not count this repository because we have filters and the repository is not in the filters.
					return 0;
				}

				var syncRepositoryFilter = SyncSettings.GetFilter(repository);
				return repository.GetChangeCount(request.Since, request.Until, syncRepositoryFilter);
			});

		return changeCount;
	}

	private static IEnumerable<Relationship> GetRelationshipConfigurations(ISyncEntity entity)
	{
		var syncEntityType = typeof(ISyncEntity);
		var properties = entity.GetRealType().GetProperties();
		var syncProperties = properties
			.Where(x => syncEntityType.IsAssignableFrom(x.PropertyType))
			.Select(x => new
			{
				EntityPropertyInfo = x,
				EntityIdPropertyInfo = properties.FirstOrDefault(y => y.Name == (x.Name + "Id")),
				EntitySyncIdPropertyInfo = properties.FirstOrDefault(y => y.Name == (x.Name + "SyncId")),
				Type = x.PropertyType,
				TypeIdPropertyInfo = x.PropertyType.GetProperties().First(p => p.Name == "Id")
			})
			.ToList();

		var response = syncProperties
			.Where(x => x.EntityIdPropertyInfo != null)
			.Where(x => x.EntitySyncIdPropertyInfo != null)
			.Select(x => new Relationship
			{
				EntityPropertyInfo = x.EntityPropertyInfo,
				EntityIdPropertyInfo = x.EntityIdPropertyInfo,
				EntitySyncId = (Guid?) x.EntitySyncIdPropertyInfo.GetValue(entity),
				Type = x.Type,
				TypeIdPropertyInfo = x.TypeIdPropertyInfo
			})
			.ToList();

		return response;
	}

	/// <summary>
	/// Process the sync object.
	/// </summary>
	/// <returns> True if the sync object was processed otherwise false. </returns>
	private bool ProcessSyncObject(SyncObject syncObject, ISyncableDatabase database, ICollection<SyncIssue> issues, bool correction, bool isIndividualProcess)
	{
		return Profiler.Time(nameof(ProcessSyncObject), () =>
		{
			Logger?.Write(LogLevel.Debug, SyncSessionStart?.Id ?? Guid.Empty, correction
					? $"Processing sync object correction {syncObject.SyncId} {syncObject.TypeName}."
					: $"Processing sync object {syncObject.SyncId} {syncObject.TypeName}.",
				DateTimeProvider.UtcNow
			);

			// SyncObject => SyncModel => SyncEntity
			var syncEntity = Converter.ConvertIncoming(this, syncObject);
			if (syncEntity == null)
			{
				Debugger.Break();
				return false;
			}

			if (!SyncSettings.ShouldSyncRepository(syncEntity.GetType()))
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

			if (SyncSettings.ShouldFilterIncomingEntity(syncObject.TypeName, syncEntity))
			{
				var issue = new SyncIssue
				{
					Id = syncObject.SyncId,
					IssueType = SyncIssueType.SyncEntityFiltered,
					Message = "The item is not being processed because the sync entity is being filtered.",
					TypeName = syncObject.TypeName
				};
				issues.Add(issue);
				Logger?.Write(LogLevel.Debug, SyncSessionStart?.Id ?? Guid.Empty, issue.Message, DateTimeProvider.UtcNow);
				return false;
			}

			var type = syncEntity.GetType();
			var repository = database.GetSyncableRepository(type);

			if (repository == null)
			{
				throw new InvalidDataException("Failed to find a syncable repository for the entity.");
			}

			var syncRepositoryFilter = SyncSettings.GetFilter(repository);
			var foundEntity = Profiler.Time($"{nameof(ProcessSyncObject)}ReadEntity", () =>
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
							// update cache?
							return readEntity;
						}

						return null;
					}
					else
					{
						// Id was found so let's read the entity by the primary key
						var readEntity = repository.ReadByPrimaryId(id);
						if ((readEntity != null) && (readEntity.SyncId == syncEntity.SyncId))
						{
							// The entity was found so return it by ID.
							return readEntity;
						}
					}
				}

				return doesNotHaveLookupFilter
					? repository.Read(syncObject.SyncId)
					: repository.Read(syncEntity, syncRepositoryFilter);
			});

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
					return Profiler.Time($"{nameof(ProcessSyncObject)}Added", () =>
					{
						// Instantiate a new instance of the sync entity to update, also use the provided sync ID
						// this is because it's possibly the sync entity is blocking updating of the sync ID so it 
						// will need to be set manually being that it will be filtered on update.
						foundEntity = (ISyncEntity) Activator.CreateInstance(syncEntity.GetType());
						if (foundEntity == null)
						{
							throw new SyncIssueException(SyncIssueType.Unknown, "Failed to create a new instance.");
						}

						foundEntity.SyncId = syncObject.SyncId;

						if (UpdateEntity(database, syncObject, syncEntity, foundEntity, syncStatus, issues))
						{
							repository.Add(foundEntity);
							return true;
						}

						return false;
					});
				}
				case SyncObjectStatus.Updated:
				{
					return Profiler.Time($"{nameof(ProcessSyncObject)}Modified", () =>
					{
						if ((foundEntity == null)
							|| ((foundEntity.ModifiedOn >= syncEntity.ModifiedOn)
								&& !correction))
						{
							// Did not find the entity, or it has not changed.
							return false;
						}

						if (!UpdateEntity(database, syncObject, syncEntity, foundEntity, syncStatus, issues))
						{
							// todo: roll back any entity changes
							//database.RevertChanges(foundEntity);
							return false;
						}

						return true;
					});
				}
				case SyncObjectStatus.Deleted:
				{
					return Profiler.Time($"{nameof(ProcessSyncObject)}Deleted", () =>
					{
						var entityIsNew = foundEntity == null;
						if (entityIsNew)
						{
							// Check to see if we are permanently deleting sync entity
							if (SyncSettings.PermanentDeletions)
							{
								// Entity not found, and we don't soft delete so bounce
								return false;
							}

							// We did not find the entity, and we should be soft deleting
							// this means we must "add" the entity so we can delete it

							// Insert the "soft deleted" item into the database "IsDeleted" will be handled below.
							foundEntity = (ISyncEntity) Activator.CreateInstance(syncEntity.GetType())!;
							foundEntity.SyncId = syncObject.SyncId;

							// need to add the entity first then we can be soft-deleted it.
							UpdateEntity(database, syncObject, syncEntity, foundEntity, SyncObjectStatus.Added, issues);
						}

						// Now run the delete process.
						if (!UpdateEntity(database, syncObject, syncEntity, foundEntity, syncStatus, issues))
						{
							// todo: roll back any possible changes
							return false;
						}

						if (entityIsNew)
						{
							// The entity was restored to be marked as soft deleted entity
							repository.Add(foundEntity);
						}

						if (SyncSettings.PermanentDeletions)
						{
							repository.Remove(foundEntity);
						}
						else
						{
							foundEntity.IsDeleted = true;
						}

						return true;
					});
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		});
	}

	private void ProcessSyncObjects(ISyncableDatabaseProvider provider, IEnumerable<SyncObject> syncObjects, ICollection<SyncIssue> issues, bool corrections)
	{
		Profiler.Time(nameof(ProcessSyncObjects), () =>
		{
			var objects = Profiler.Time(nameof(ProcessSyncObjects) + "SyncObjectsToList", syncObjects.ToList);
			if (objects.Count <= 0)
			{
				return;
			}

			try
			{
				var database = Profiler.Time(nameof(ProcessSyncObjects) + "GetDatabase", () =>
				{
					var d = provider.GetSyncableDatabase();
					d.DatabaseSettings.MaintainCreatedOn = false;
					d.DatabaseSettings.MaintainModifiedOn = IsServerClient;
					return d;
				});

				try
				{
					var changes = 0;

					Profiler.Time(nameof(ProcessSyncObjects), () =>
					{
						for (var i = 0; i < objects.Count; i++)
						{
							if (ProcessSyncObject(objects[i], database, issues, corrections, false))
							{
								changes++;
							}
						}
					});

					Profiler.Time(nameof(ProcessSyncObjects) + "SaveDatabase", database.SaveChanges);

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
					database.Dispose();
				}
			}
			catch
			{
				Statistics.IndividualProcessCount++;
				Logger?.Write(LogLevel.Warning, SyncSessionStart?.Id ?? Guid.Empty, "Failed to process sync objects in the batch.", DateTimeProvider.UtcNow);
				ProcessSyncObjectsIndividually(provider, objects, issues, corrections);
			}
		});
	}

	private void ProcessSyncObjectsIndividually(ISyncableDatabaseProvider provider, IEnumerable<SyncObject> syncObjects, ICollection<SyncIssue> issues, bool corrections)
	{
		Profiler.Time(nameof(ProcessSyncObjectsIndividually), () =>
		{
			var objects = syncObjects.ToList();

			foreach (var syncObject in objects)
			{
				try
				{
					using var database = Profiler.Time($"{nameof(ProcessSyncObjectsIndividually)}GetDatabase", () =>
					{
						var d = provider.GetSyncableDatabase();
						d.DatabaseSettings.MaintainCreatedOn = false;
						d.DatabaseSettings.MaintainModifiedOn = IsServerClient;
						return d;
					});

					if (!ProcessSyncObject(syncObject, database, issues, corrections, true))
					{
						continue;
					}

					Profiler.Time($"{nameof(ProcessSyncObjectsIndividually)}SaveDatabase", () => database.SaveChanges());

					if (corrections)
					{
						Statistics.AppliedCorrections++;
					}
					else
					{
						Statistics.AppliedChanges++;
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
		});
	}

	private bool UpdateEntity(ISyncableDatabase database, SyncObject syncObject, ISyncEntity syncEntity, ISyncEntity foundEntity, SyncObjectStatus status, ICollection<SyncIssue> issues)
	{
		try
		{
			if (!UpdateEntity(syncEntity, foundEntity, status))
			{
				// returning false just means do not process and do not return a sync issue
				return false;
			}

			UpdateLocalRelationships(foundEntity, database);
			return true;
		}
		catch (SyncUpdateException ex)
		{
			// throwing an update exception just means return a sync issue
			var issue = new SyncIssue
			{
				Id = syncObject.SyncId,
				IssueType = SyncIssueType.UpdateException,
				Message = ex.Message,
				TypeName = syncObject.TypeName
			};
			issues.Add(issue);
			return false;
		}
	}

	private bool UpdateEntity(ISyncEntity source, ISyncEntity destination, SyncObjectStatus status)
	{
		return Converter?.Update(this, source, destination, status) ?? false;
	}

	/// <summary>
	/// Updates the entities local relationships.
	/// </summary>
	/// <param name="entity"> The entity to update. </param>
	/// <param name="database"> The database with the relationship repositories. </param>
	/// <exception cref="SyncIssueException"> An exception will all sync issues. </exception>
	private void UpdateLocalRelationships(ISyncEntity entity, ISyncableDatabase database)
	{
		var response = new List<SyncIssue>();

		foreach (var relationship in GetRelationshipConfigurations(entity))
		{
			if (!relationship.EntitySyncId.HasValue || (relationship.EntitySyncId == Guid.Empty))
			{
				continue;
			}

			var entityId = database.KeyCache?.GetEntityId(relationship.Type, relationship.EntitySyncId.Value);
			if (entityId != null)
			{
				relationship.EntityIdPropertyInfo.SetValue(entity, entityId);
				continue;
			}

			// todo: repositories with custom lookup predicates do not use the same sync id
			// - this is problematic when it comes to relationship other fk relationships because the sync IDs may not match
			var repository = database.GetSyncableRepository(relationship.Type);
			var foundEntity = repository?.Read(relationship.EntitySyncId.Value);

			if (foundEntity != null)
			{
				var id = relationship.TypeIdPropertyInfo.GetValue(foundEntity);
				relationship.EntityIdPropertyInfo.SetValue(entity, id);
				database.KeyCache?.AddEntityId(relationship.Type, relationship.EntitySyncId.Value, id);
				continue;
			}

			response.Add(new SyncIssue
			{
				Id = relationship.EntitySyncId.Value,
				IssueType = SyncIssueType.RelationshipConstraint,
				Message = "Failed to find the relational entity.",
				TypeName = relationship.Type.ToAssemblyName()
			});
		}

		if (response.Any(x => x != null))
		{
			throw new SyncIssueException(SyncIssueType.RelationshipConstraint,
				"This entity has relationship issues.",
				response.Where(x => x != null).ToArray());
		}
	}

	#endregion

	#region Classes

	internal class Relationship
	{
		#region Properties

		/// <summary>
		/// The property information for the entity ID.
		/// </summary>
		public PropertyInfo EntityIdPropertyInfo { get; set; }

		/// <summary>
		/// The property information for the entity.
		/// </summary>
		public PropertyInfo EntityPropertyInfo { get; set; }

		public Guid? EntitySyncId { get; set; }

		public Type Type { get; set; }

		public PropertyInfo TypeIdPropertyInfo { get; internal set; }

		#endregion
	}

	#endregion
}