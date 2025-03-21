#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Client;

public class ClientSetting : SyncEntity<long, Setting>, ISetting, IClientEntity
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public ClientSetting()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override long Id { get; set; }

	/// <inheritdoc />
	public DateTime LastClientUpdate { get; set; }

	public string Name { get; set; }

	public string Value { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanBeModified()
	{
		return true;
	}

	#endregion
}