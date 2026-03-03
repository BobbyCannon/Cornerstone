#region References

using Cornerstone.Reflection;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a Cornerstone database.
/// </summary>
[SourceReflection]
public abstract class SyncableDatabase : Database, ISyncableDatabase
{
	#region Methods

	public abstract ISyncableRepository<T, T1> GetSyncableRepository<T, T1>() where T : SyncEntity<T1>;

	#endregion
}

/// <summary>
/// The interfaces for a Cornerstone syncable database.
/// </summary>
public interface ISyncableDatabase : IDatabase
{
	#region Methods

	ISyncableRepository<T, T1> GetSyncableRepository<T, T1>() where T : SyncEntity<T1>;

	#endregion
}