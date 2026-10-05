#region References

using System;
using System.Collections.Generic;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type of the entity of the collection. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
public interface ISyncableRepository<T, in T2> : ISyncableRepository, IRepository<T, T2>
	where T : SyncEntity<T2>
{
	#region Methods

	/// <summary>
	/// Gets the sync entity by the primary ID.
	/// </summary>
	/// <param name="primaryId"> The primary ID of the sync entity. </param>
	/// <returns> The sync entity or null. </returns>
	ISyncEntity ReadByPrimaryId(T2 primaryId);

	#endregion
}

/// <summary>
/// Represents a syncable repository.
/// </summary>
public interface ISyncableRepository
{
	#region Properties

	/// <summary>
	/// The type this repository is for.
	/// </summary>
	Type RealType { get; }

	/// <summary>
	/// The type name this repository is for. Will be in assembly name format.
	/// </summary>
	string TypeName { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Adds a sync entity to the repository.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	void Add(ISyncEntity entity);

	/// <summary>
	/// Drop a pending apply so a rejected update is not written on SaveChanges.
	/// </summary>
	/// <param name="entity"> The entity to forget. </param>
	void Discard(ISyncEntity entity);

	/// <summary>
	/// Gets the count of changes from the repository.
	/// </summary>
	/// <param name="since"> The start date and time get changes for. </param>
	/// <param name="until"> The end date and time get changes for. </param>
	/// <param name="filter"> The optional filter expression to filter changes. </param>
	/// <returns> The count of changes from the repository. </returns>
	int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter);

	/// <summary>
	/// Gets the changes from the repository. The results are read only and will not have tracking enabled.
	/// A change is CreatedOn in [since, until) or ModifiedOn in [since, until), ANDed with the filter
	/// scope and outgoing keep-tests. Rows are ordered by ModifiedOn then Id. Skip is the count already
	/// returned for this repository in this window.
	/// </summary>
	/// <param name="since"> The start date and time get changes for. </param>
	/// <param name="until"> The end date and time get changes for. </param>
	/// <param name="skip"> The number of matching rows already returned for this repository. </param>
	/// <param name="take"> The number of items to take. </param>
	/// <param name="filter"> The optional filter expression to filter changes. </param>
	/// <returns> The list of changes from the repository. </returns>
	IEnumerable<ISyncEntity> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter);

	/// <summary>
	/// Gets the sync entity by the ID.
	/// </summary>
	/// <param name="syncId"> The ID of the sync entity. </param>
	/// <returns> The sync entity or null. </returns>
	ISyncEntity Read(Guid syncId);

	/// <summary>
	/// Gets the sync entity by the ID.
	/// </summary>
	/// <param name="entity"> The entity to use with the filter. </param>
	/// <param name="filter"> An optional sync filter to locate the entity. </param>
	/// <returns> The sync entity or null. </returns>
	ISyncEntity Read(ISyncEntity entity, SyncRepositoryFilter filter);

	/// <summary>
	/// Read all keys for the repository.
	/// For local/client caches only. Do not call on a server repository (100 million+ rows).
	/// </summary>
	/// <returns> </returns>
	IDictionary<Guid, object> ReadAllKeys();

	/// <summary>
	/// Gets the sync entity by the primary ID.
	/// </summary>
	/// <param name="primaryId"> The primary ID of the sync entity. </param>
	/// <returns> The sync entity or null. </returns>
	ISyncEntity ReadByPrimaryId(object primaryId);

	/// <summary>
	/// Removes a sync entity from the repository.
	/// </summary>
	/// <param name="entity"> The entity to be removed. </param>
	void Remove(ISyncEntity entity);

	#endregion
}