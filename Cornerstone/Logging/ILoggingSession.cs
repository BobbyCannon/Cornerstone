#region References

using System;

#endregion

namespace Cornerstone.Logging;

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