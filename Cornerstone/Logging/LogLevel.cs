namespace Cornerstone.Logging;

/// <summary>
/// Severity for operational log messages.
/// </summary>
public enum LogLevel
{
	/// <summary>
	/// Most detailed messages (high volume).
	/// </summary>
	Trace = 0,

	/// <summary>
	/// Detailed diagnostic messages.
	/// </summary>
	Debug = 1,

	/// <summary>
	/// General informational messages.
	/// </summary>
	Information = 2,

	/// <summary>
	/// Unexpected but recoverable conditions.
	/// </summary>
	Warning = 3,

	/// <summary>
	/// Failures that need attention.
	/// </summary>
	Error = 4,

	/// <summary>
	/// Severe failures.
	/// </summary>
	Critical = 5,

	/// <summary>
	/// Disable all logging when used as <see cref="Logger.MinimumLevel" />.
	/// </summary>
	None = 6
}
