#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[SqlTable(TableName = "Accounts")]
[Updateable(UpdateableAction.EverythingExceptSyncAddAndUpdate, [
	nameof(EmailAddress), nameof(LastLoginDate), nameof(Name), nameof(Picture),
	nameof(Roles), nameof(Status), nameof(TimeZoneId)
])]
public partial class AccountEntity
	: SyncEntity<int>, IAccount,
		IComparable<AccountEntity>
{
	#region Properties

	[SqlTableColumn(IsNullable = false)]
	public partial string EmailAddress { get; set; }

	public partial DateTime LastLoginDate { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Name { get; set; }

	public partial string Picture { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Roles { get; set; }

	public partial AccountStatus Status { get; set; }

	public partial string TimeZoneId { get; set; }

	#endregion
}