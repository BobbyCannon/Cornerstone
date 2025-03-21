﻿#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Attributes;
using Cornerstone.Data;
using Cornerstone.Settings;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Server;

public class SettingEntity : SyncEntity<long>
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public SettingEntity()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override long Id { get; set; }

	/// <summary>
	/// These are named to show that an incoming converter can convert
	/// <see cref="Setting{T}.Name" /> to this property.
	/// </summary>
	[SyncProperty(UpdateableAction.SyncIncomingAdd | UpdateableAction.SyncOutgoing)]
	public string ServerName { get; set; }

	/// <summary>
	/// These are named to show that an incoming converter can convert
	/// <see cref="Setting{T}.Value" /> to this property.
	/// </summary>
	[SyncProperty(UpdateableAction.SyncAll)]
	public string ServerValue { get; set; }

	#endregion
}