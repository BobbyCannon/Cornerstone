#region References

using System;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type contained in the repository. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
[Serializable]
internal class SyncableRepository<T, T2>
	: Repository<T, T2>,
		ISyncableRepository<T, T2>
	where T : SyncEntity<T2>
{
}

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type of the entity of the collection. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
public interface ISyncableRepository<T, in T2>
	: ISyncableRepository,
		IRepository<T, T2>
	where T : SyncEntity<T2>
{
}

/// <summary>
/// Represents a syncable repository.
/// </summary>
public interface ISyncableRepository
{
}