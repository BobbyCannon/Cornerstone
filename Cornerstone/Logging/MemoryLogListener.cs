#region References

using System;
using System.Collections.Concurrent;
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
		Events = new ConcurrentDictionary<Guid, SpeedyList<LogEventArgs>>();
		Sessions = new ConcurrentDictionary<Guid, ILoggingSession>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The events that have been captured from the event source (logger).
	/// </summary>
	public ConcurrentDictionary<Guid, SpeedyList<LogEventArgs>> Events { get; }

	/// <summary>
	/// The session info for a set of events.
	/// </summary>
	public ConcurrentDictionary<Guid, ILoggingSession> Sessions { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Clear the listener.
	/// </summary>
	public void Clear()
	{
		Events.Clear();
		Sessions.Clear();
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
	public SpeedyList<LogEventArgs> GetEvents(Guid sessionId)
	{
		return Events.TryGetValue(sessionId, out var events)
			? events
			: [];
	}

	/// <summary>
	/// Gets the events in a details string format.
	/// </summary>
	/// <param name="includeDateTime"> Option to include messaged on in message. </param>
	/// <param name="includeSessionId"> Option to include session ID in message. </param>
	/// <param name="elapsedTime"> Optional to convert DateTime to Elapsed. Relative to session start time value. </param>
	/// <returns> The formatted events. </returns>
	public List<string> GetEventsDetails(bool includeDateTime = true, bool includeSessionId = false, bool elapsedTime = false)
	{
		var sessions = Sessions
			.OrderBy(x => x.Value.StartedOn)
			.Select(x => x.Value)
			.ToList();

		return sessions
			.SelectMany(x =>
				Enumerable
				.OrderBy<LogEventArgs, DateTime>((Events.TryGetValue(x.SessionId, out var values) ? values : new List<LogEventArgs>()), o => o.MessagedOn)
				.Select(v => v.GetDetailedMessage(
					includeDateTime,
					includeSessionId,
					elapsedTime ? x.StartedOn : null
				))
			)
			.ToList();
	}

	/// <inheritdoc />
	protected override void OnEventWritten(EventWrittenEventArgs args)
	{
		if ((SessionId != Guid.Empty) && !Equals(SessionId, args.Payload?.FirstOrDefault()))
		{
			return;
		}

		if (OnlyEventsWithMessages && string.IsNullOrEmpty(args.Message))
		{
			return;
		}

		var logEvent = args.ToLogEvent();
		Sessions.GetOrAdd(SessionId, guid => new LoggingSession(guid, logEvent.MessagedOn));
		var list = Events.GetOrAdd(SessionId, x => []);
		list.Add(logEvent);
		base.OnEventWritten(args);
	}

	#endregion
}