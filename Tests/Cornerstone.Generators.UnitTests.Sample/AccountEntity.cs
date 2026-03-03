#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Generators.UnitTests.Sample;

public class AccountEntity : SyncEntity<int>
{
	#region Properties

	public DateTime? LastLoggedIn { get; set; }

	public string Name { get; set; }

	#endregion
}