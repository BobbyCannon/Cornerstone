#region References

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Cornerstone.Convert.Converters;
using Cornerstone.Logging;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for logging.
/// </summary>
public static class LoggingExtensions
{
	#region Methods

	/// <summary>
	/// Convert the event written event argument to its payload string
	/// </summary>
	/// <param name="args"> The item to process. </param>
	/// <param name="includeSessionId"> Options to include session ID. </param>
	/// <param name="startedOn"> Optional to convert DateTime to Elapsed. Relative to provided value. </param>
	/// <returns> The formatted message. </returns>
	public static string GetDetailedMessage(this EventWrittenEventArgs args, bool includeSessionId = false, DateTime? startedOn = null)
	{
		if ((args == null) || (args.Payload == null))
		{
			throw new ArgumentNullException(nameof(args));
		}

		var messagedOn = args.Payload.Count >= 3
			? args.Payload[2] as DateTime? ?? default
			: default;

		if (messagedOn == default)
		{
			return includeSessionId
				? $"{args.Payload[0]} {args.Level} : {args.GetMessage()}"
				: $"{args.Level} : {args.GetMessage()}";
		}

		if ((startedOn != null) && (startedOn > DateTime.MinValue))
		{
			var elapsed = messagedOn - startedOn;
			return includeSessionId
				? $"{elapsed:c} - {args.Payload[0]} {args.Level} : {args.GetMessage()}"
				: $"{elapsed:c} - {args.Level} : {args.GetMessage()}";
		}

		return includeSessionId
			? $"{args.Payload[2]:O} - {args.Payload[0]} {args.Level} : {args.GetMessage()}"
			: $"{args.Payload[2]:O} - {args.Level} : {args.GetMessage()}";
	}

	/// <summary>
	/// Convert the event written event argument to its payload string
	/// </summary>
	/// <param name="args"> The item to process. </param>
	/// <returns> The formatted message. </returns>
	public static string GetMessage(this EventWrittenEventArgs args)
	{
		return string.IsNullOrWhiteSpace(args.Message)
			? args.Message
			: string.Format(args.Message, args.Payload.ToArray());
	}

	/// <summary>
	/// Convert the event written event argument to a speedy log event.
	/// </summary>
	/// <param name="args"> The item to process. </param>
	/// <returns> The log event. </returns>
	public static LogEventArgs ToLogEvent(this EventWrittenEventArgs args)
	{
		if (args?.Payload == null)
		{
			throw new ArgumentNullException(nameof(args));
		}

		var messagedOn = args.Payload.Count >= 3
			? args.Payload[2] as DateTime? ?? default
			: default;

		var sessionId = GuidConverter.TryToGuid(args.Payload[0], out var guid) ? guid : Guid.Empty;
		var message = args.GetMessage();

		return new LogEventArgs(sessionId, messagedOn, args.Level, message);
	}

	#endregion
}