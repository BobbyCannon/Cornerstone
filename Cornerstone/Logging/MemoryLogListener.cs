#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Log listener that will keep a history of events.
/// </summary>
public class MemoryLogListener : LogListener
{
	#region Constructors

	private MemoryLogListener(Guid sessionId, EventLevel level = EventLevel.Informational) : base(sessionId, level)
	{
		Events = new SpeedyList<LogEventArgs>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The events that have been captured from the event source (logger).
	/// </summary>
	public SpeedyList<LogEventArgs> Events { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Clear the listener.
	/// </summary>
	public void Clear()
	{
		Events.Clear();
	}

	/// <summary>
	/// Create an instance of the log listener and start listening.
	/// </summary>
	/// <param name="sessionId"> The session of the log to monitor. </param>
	/// <param name="level"> The level in which to log. </param>
	/// <param name="initialize"> An optional initialize action. </param>
	public static MemoryLogListener CreateSession(Guid sessionId, EventLevel level, Action<LogListener> initialize = null)
	{
		var logListener = new MemoryLogListener(sessionId, level);
		initialize?.Invoke(logListener);
		logListener.Start();
		return logListener;
	}

	/// <summary>
	/// Return events for a specific session.
	/// </summary>
	/// <param name="sessionId"> The ID of the session to access. </param>
	/// <returns> The events if found otherwise empty collection. </returns>
	public IEnumerable<LogEventArgs> GetEvents(Guid sessionId)
	{
		return Events.Where(x => x.SessionId == sessionId).ToList();
	}

	/// <summary>
	/// Gets the events in a details string format.
	/// </summary>
	/// <param name="includeDateTime"> Option to include messaged on in message. </param>
	/// <param name="includeSessionId"> Option to include session ID in message. </param>
	/// <returns> The formatted events. </returns>
	public List<string> GetEventsDetails(bool includeDateTime = true, bool includeSessionId = false)
	{
		return Events
			.Select(v => v.GetDetailedMessage(
					includeDateTime,
					includeSessionId
				)
			)
			.ToList();
	}

	/// <inheritdoc />
	protected override void OnEventWritten(EventWrittenEventArgs args)
	{
		var logEvent = args.ToLogEvent();

		if ((SessionId != Guid.Empty) && !Equals(SessionId, logEvent.SessionId))
		{
			return;
		}

		if (OnlyEventsWithMessages && string.IsNullOrEmpty(args.Message))
		{
			return;
		}

		Events.Add(logEvent);
		base.OnEventWritten(args);
	}

	#endregion
}