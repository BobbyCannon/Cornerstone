#region References

using System;

#endregion

namespace Cornerstone.Keystone.Messages;

/// <summary>
/// One completed bus publish for diagnostics history (ring buffer on KeystoneBus).
/// </summary>
public class ChannelMessageHistory
{
	#region Properties

	/// <summary>
	/// Channel type name (e.g. NotificationChannel).
	/// </summary>
	public string ChannelName { get; set; } = string.Empty;

	/// <summary>
	/// Handler batch duration in ticks (<see cref="TimeSpan.Ticks"/> scale via Stopwatch conversion).
	/// </summary>
	public long ElapsedTicks { get; set; }

	/// <summary>
	/// First handler exception message when <see cref="HadError"/> is true; otherwise empty.
	/// </summary>
	public string ErrorMessage { get; set; } = string.Empty;

	/// <summary>
	/// True when any handler threw during this publish.
	/// </summary>
	public bool HadError { get; set; }

	/// <summary>
	/// Number of handlers invoked for this message type.
	/// </summary>
	public int HandlerCount { get; set; }

	/// <summary>
	/// Display name (message type or payload type when useful).
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// UTC time when the publish completed (after handlers).
	/// </summary>
	public DateTime PublishOn { get; set; }

	/// <summary>
	/// Monotonic sequence assigned by the bus (stable UI identity).
	/// </summary>
	public long Sequence { get; set; }

	/// <summary>
	/// Message CLR type name (the operation id).
	/// </summary>
	public string Type { get; set; } = string.Empty;

	#endregion

	#region Methods

	/// <summary>
	/// Handler batch duration as a TimeSpan.
	/// </summary>
	public TimeSpan GetElapsed()
	{
		return TimeSpan.FromTicks(ElapsedTicks);
	}

	#endregion
}
