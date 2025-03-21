#region References

using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Sync;

public class LogEvent : SyncModel, ILogEvent
{
	#region Properties

	public LogLevel Level { get; set; }

	public string Message { get; set; }

	#endregion
}

public interface ILogEvent
{
	#region Properties

	/// <summary>
	/// The log level for the log event.
	/// </summary>
	LogLevel Level { get; set; }

	/// <summary>
	/// The message for the log event.
	/// </summary>
	string Message { get; set; }

	#endregion
}