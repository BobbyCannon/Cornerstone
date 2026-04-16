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
public partial class Account : SyncModel, IAccount
{
	#region Properties

	public partial string EmailAddress { get; set; }
	public partial DateTime LastLoginDate { get; set; }
	public partial string Name { get; set; }
	public partial string Picture { get; set; }
	public partial string Roles { get; set; }
	public partial AccountStatus Status { get; set; }
	public partial string TimeZoneId { get; set; }

	#endregion
}

public interface IAccount : IComparable
{
	#region Properties

	public string EmailAddress { get; set; }
	public DateTime LastLoginDate { get; set; }
	public string Name { get; set; }
	public string Picture { get; set; }
	public string Roles { get; set; }
	public AccountStatus Status { get; set; }
	public string TimeZoneId { get; set; }

	#endregion
}