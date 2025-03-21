﻿#region References

using System;
using System.Diagnostics.Tracing;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Represents a log event argument.
/// </summary>
public class LogEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	public LogEventArgs() : this(Guid.Empty, DateTime.MinValue, EventLevel.Verbose, string.Empty)
	{
	}

	/// <summary>
	/// Initializes a log event argument.
	/// </summary>
	public LogEventArgs(Guid sessionId, DateTime messagedOn, EventLevel level, string message)
	{
		SessionId = sessionId;
		MessagedOn = messagedOn;
		Level = level;
		Message = message;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The level of the event.
	/// </summary>
	public EventLevel Level { get; set; }

	/// <summary>
	/// The message for the event.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// The date time of the event
	/// </summary>
	public DateTime MessagedOn { get; set; }

	/// <summary>
	/// The session this message was captured on.
	/// </summary>
	public Guid SessionId { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Convert the event written event argument to its payload string
	/// </summary>
	/// <param name="includeDateTime"> Option to include messaged on in message. </param>
	/// <param name="includeSessionId"> Option to include session ID in message. </param>
	/// <param name="startedOn"> Optional to convert [DateTime] to [Elapsed]. Relative to provided value. </param>
	/// <returns> The formatted message. </returns>
	public string GetDetailedMessage(bool includeDateTime = true, bool includeSessionId = true, DateTime? startedOn = null)
	{
		if (!includeDateTime || (MessagedOn == default))
		{
			return includeSessionId && (SessionId != Guid.Empty)
				? $"{SessionId} {Level} : {Message}"
				: $"{Level} : {Message}";
		}

		if ((startedOn != null) && (startedOn > DateTime.MinValue))
		{
			var elapsed = MessagedOn - startedOn;
			return includeSessionId && (SessionId != Guid.Empty)
				? $"{elapsed:c} - {SessionId} {Level} : {Message}"
				: $"{elapsed:c} - {Level} : {Message}";
		}

		return includeSessionId && (SessionId != Guid.Empty)
			? $"{MessagedOn:O} - {SessionId} {Level} : {Message}"
			: $"{MessagedOn:O} - {Level} : {Message}";
	}

	#endregion
}