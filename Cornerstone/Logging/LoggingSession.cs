#region References

using System;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Represents a logging session.
/// </summary>
public class LoggingSession : ILoggingSession
{
	#region Constructors

	/// <summary>
	/// Initialize an instance of a logging session.
	/// </summary>
	/// <param name="sessionId"> The ID of the session. </param>
	/// <param name="startedOn"> The date and time the session started. </param>
	public LoggingSession(Guid sessionId, DateTime startedOn)
	{
		SessionId = sessionId;
		StartedOn = startedOn;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public Guid SessionId { get; }

	/// <inheritdoc />
	public DateTime StartedOn { get; }

	#endregion
}

/// <summary>
/// Represents a logging session.
/// </summary>
public interface ILoggingSession
{
	#region Properties

	/// <summary>
	/// Gets the ID of the logging session.
	/// </summary>
	Guid SessionId { get; }

	/// <summary>
	/// The date time the logging session started on.
	/// </summary>
	DateTime StartedOn { get; }

	#endregion
}