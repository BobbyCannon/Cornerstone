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
[Updateable(UpdateableAction.All, [
	nameof(AddressSyncId), nameof(CustomerSyncId), nameof(EmailAddress), nameof(LastLoginDate), nameof(Name), nameof(Picture),
	nameof(Roles), nameof(Status), nameof(TimeZoneId)
])]
[Updateable(UpdateableAction.EverythingExceptSync, [nameof(AddressId), nameof(CustomerId)])]
public partial class AccountEntity
	: SyncEntity<int>, IAccount,
		IUpdateable<AccountEntity>,
		IUpdateable<Account>,
		IUpdateable<IAccount>,
		IComparable<AccountEntity>
{
	#region Properties

	[SqlTableColumn]
	[SqlForeignKey("Addresses")]
	public partial long? AddressId { get; set; }

	[SqlTableColumn]
	public partial Guid? AddressSyncId { get; set; }

	[SqlTableColumn]
	public partial int? CustomerId { get; set; }

	[SqlTableColumn]
	public partial Guid? CustomerSyncId { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string EmailAddress { get; set; }

	[SqlIndex]
	[SqlTableColumn]
	public partial DateTime LastLoginDate { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Name { get; set; }

	[SqlTableColumn]
	public partial string Picture { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Roles { get; set; }

	[SqlTableColumn]
	public partial AccountStatus Status { get; set; }

	[SqlTableColumn]
	public partial string TimeZoneId { get; set; }

	#endregion
}