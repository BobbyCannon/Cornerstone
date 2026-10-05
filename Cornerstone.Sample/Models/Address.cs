#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[Packable(1, ["*"])]
public partial class Address
	: SyncModel, IAddress,
		IUpdateable<Address>,
		IUpdateable<AddressEntity>,
		IUpdateable<IAddress>
{
	#region Properties

	public partial float AccuracyMeters { get; set; }
	public partial string City { get; set; }
	public partial Guid? CustomerSyncId { get; set; }
	public partial Guid ExternalId { get; set; }
	public partial byte Floor { get; set; }
	public partial bool IsPrimary { get; set; }
	public partial double Latitude { get; set; }
	public partial string Line1 { get; set; }
	public partial double Longitude { get; set; }
	public partial byte[] Photo { get; set; }
	public partial string Postal { get; set; }
	public partial string State { get; set; }
	public partial decimal TaxRate { get; set; }
	public partial short UnitNumber { get; set; }

	#endregion
}

public interface IAddress
{
	#region Properties

	public float AccuracyMeters { get; set; }
	public string City { get; set; }
	public Guid? CustomerSyncId { get; set; }
	public Guid ExternalId { get; set; }
	public byte Floor { get; set; }
	public bool IsPrimary { get; set; }
	public double Latitude { get; set; }
	public string Line1 { get; set; }
	public double Longitude { get; set; }
	public byte[] Photo { get; set; }
	public string Postal { get; set; }
	public string State { get; set; }
	public decimal TaxRate { get; set; }
	public short UnitNumber { get; set; }

	#endregion
}