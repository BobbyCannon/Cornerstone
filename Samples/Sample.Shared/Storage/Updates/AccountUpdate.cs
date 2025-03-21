#region References

using System;
using Cornerstone.Data;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Updates;

public class AccountUpdate : PartialUpdate<Account>
{
	#region Properties

	public int Id
	{
		get => Get(0);
		set => Set(value);
	}

	public string Name
	{
		get => Get(string.Empty);
		set => Set(value);
	}

	public Guid SyncId
	{
		get => Get(Guid.Empty);
		set => Set(value);
	}

	#endregion
}