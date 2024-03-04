#region References

using Cornerstone.Sync;
using Sample.Shared.Storage.Client;

#endregion

namespace Sample.Shared.Storage;

public interface IClientDatabase : ISyncableDatabase
{
	#region Properties

	ISyncableRepository<ClientAccount, int> Accounts { get; }

	ISyncableRepository<ClientAddress, long> Addresses { get; }

	#endregion
}