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
[SqlTable(TableName = "Addresses")]
[Updateable(UpdateableAction.All, [
	nameof(AccuracyMeters), nameof(City), nameof(CustomerSyncId), nameof(ExternalId), nameof(Floor),
	nameof(IsPrimary), nameof(Latitude), nameof(Line1), nameof(Longitude), nameof(Photo), nameof(Postal),
	nameof(State), nameof(TaxRate), nameof(UnitNumber)
])]
[Updateable(UpdateableAction.EverythingExceptSync, [nameof(CustomerId)])]
public partial class AddressEntity
	: SyncEntity<long>, IAddress,
		IUpdateable<AddressEntity>,
		IUpdateable<Address>,
		IUpdateable<IAddress>
{
	#region Properties

	[SqlTableColumn]
	public partial float AccuracyMeters { get; set; }

	[SqlTableColumn]
	public partial string City { get; set; }

	[SqlTableColumn]
	public partial int? CustomerId { get; set; }

	[SqlTableColumn]
	public partial Guid? CustomerSyncId { get; set; }

	[SqlTableColumn]
	public partial Guid ExternalId { get; set; }

	[SqlTableColumn]
	public partial byte Floor { get; set; }

	[SqlTableColumn]
	public partial bool IsPrimary { get; set; }

	[SqlTableColumn]
	public partial double Latitude { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Line1 { get; set; }

	[SqlTableColumn]
	public partial double Longitude { get; set; }

	[SqlTableColumn]
	public partial byte[] Photo { get; set; }

	[SqlTableColumn]
	public partial string Postal { get; set; }

	[SqlTableColumn]
	public partial string State { get; set; }

	[SqlTableColumn]
	public partial decimal TaxRate { get; set; }

	[SqlTableColumn]
	public partial short UnitNumber { get; set; }

	#endregion
}