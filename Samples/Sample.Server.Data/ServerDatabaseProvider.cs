#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public abstract class ServerDatabaseProvider 
	: SyncableDatabaseProvider<IServerDatabase>
{
	#region Constructors

	/// <inheritdoc />
	protected ServerDatabaseProvider(IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache, IServerDatabase.GetDefaultDatabaseSettings())
	{
	}

	#endregion
}