#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Client;

public class ClientLogEvent : SyncEntity<long, LogEvent>, IClientEntity, ILogEvent
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public ClientLogEvent()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override long Id { get; set; }

	/// <summary>
	/// Represents the last time this account was update on the client
	/// </summary>
	public DateTime LastClientUpdate { get; set; }

	/// <inheritdoc />
	public LogLevel Level { get; set; }

	/// <inheritdoc />
	public string Message { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool CanBeModified()
	{
		return false;
	}

	#endregion
}