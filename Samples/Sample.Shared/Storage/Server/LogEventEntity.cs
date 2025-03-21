#region References

using System;
using Cornerstone.Sync;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Server;

public class LogEventEntity : SyncEntity<long, LogEvent>, ILogEvent
{
	#region Constructors

	public LogEventEntity()
	{
		ResetHasChanges();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The Date / Time of the log entry acknowledgement.
	/// </summary>
	public DateTime? AcknowledgedOn { get; set; }

	/// <inheritdoc />
	public override long Id { get; set; }

	/// <inheritdoc />
	public LogLevel Level { get; set; }

	/// <summary>
	/// The Date / Time of the log entry.
	/// </summary>
	public DateTime LoggedOn { get; set; }

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