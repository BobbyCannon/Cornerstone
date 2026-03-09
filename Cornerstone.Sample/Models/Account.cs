#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
public partial class Account : SyncModel, IPackable
{
	#region Properties

	[Pack(1, 5)]
	[UpdateableAction(UpdateableAction.All)]
	public string DisplayName { get; set; }

	[Pack(1, 6)]
	[UpdateableAction(UpdateableAction.All)]
	public string EmailAddress { get; set; }

	[Pack(1, 7)]
	[UpdateableAction(UpdateableAction.All)]
	public bool IsEnabled { get; set; }

	[Pack(1, 8)]
	[UpdateableAction(UpdateableAction.All)]
	public DateTime LastLoginDate { get; set; }

	[Pack(1, 9)]
	[UpdateableAction(UpdateableAction.All)]
	public string Picture { get; set; }

	[Pack(1, 10)]
	[UpdateableAction(UpdateableAction.All)]
	public string PictureUrl { get; set; }

	[Pack(1, 11)]
	[UpdateableAction(UpdateableAction.All)]
	public string Roles { get; set; }

	[Pack(1, 12)]
	[UpdateableAction(UpdateableAction.All)]
	public string TimeZoneId { get; set; }

	#endregion
}