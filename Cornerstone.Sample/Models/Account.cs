#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
public partial class Account
	: SyncEntity<int>,
		IComparable<Account>,
		IComparable
{
	#region Properties

	[UpdateableAction(UpdateableAction.All)]
	public DateTime? Birthday { get; set; }

	[Column(IsNullable = false)]
	[UpdateableAction(UpdateableAction.All)]
	public string Name { get; set; }

	#endregion
}