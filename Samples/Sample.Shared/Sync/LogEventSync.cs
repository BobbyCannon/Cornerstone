#region References

using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Sync;

public class LogEventSync : SyncModel, ILogEventSync
{
	#region Properties

	/// <inheritdoc />
	public LogLevel Level { get; set; }

	/// <inheritdoc />
	public string Message { get; set; }

	#endregion
}

public interface ILogEventSync
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