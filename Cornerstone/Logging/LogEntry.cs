#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Reusable shell for a single slot in the <see cref="Logger" /> ring buffer.
/// Preallocated at logger construction and overwritten when the ring wraps.
/// </summary>
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[SourceReflection]
public partial class LogEntry
	: CornerstoneObject, ILogEntry,
		IUpdateable<LogEntry>,
		IUpdateable<ILogEntry>
{
	#region Properties

	public partial Exception Exception { get; set; }
	public partial LogLevel Level { get; set; }
	public partial string Message { get; set; }
	public partial ulong Sequence { get; set; }
	public partial Guid SessionId { get; set; }
	public partial DateTime Timestamp { get; set; }

	#endregion

	#region Methods

	public void Fill(ulong sequence, DateTime timestamp, LogLevel level, Guid sessionId, string message, Exception exception)
	{
		Sequence = sequence;
		Timestamp = timestamp;
		Level = level;
		SessionId = sessionId;
		Message = message ?? string.Empty;
		Exception = exception;
	}

	#endregion
}

public interface ILogEntry
{
	#region Properties

	/// <summary>
	/// Optional exception captured with this entry.
	/// </summary>
	Exception Exception { get; }

	/// <summary>
	/// Severity of the message.
	/// </summary>
	LogLevel Level { get; }

	/// <summary>
	/// Message text.
	/// </summary>
	string Message { get; }

	/// <summary>
	/// Monotonic sequence number assigned at write time.
	/// </summary>
	ulong Sequence { get; }

	/// <summary>
	/// Optional session correlation id (e.g. sync session).
	/// </summary>
	Guid SessionId { get; }

	/// <summary>
	/// UTC timestamp when the entry was written.
	/// </summary>
	DateTime Timestamp { get; }

	#endregion
}