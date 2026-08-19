#region References

using System;

#endregion

namespace Cornerstone.Keystone.Messages;

/// <summary>
/// Metrics for a single channel publish after handlers complete.
/// Used by diagnostics (bus history); not a message payload.
/// </summary>
public readonly struct ChannelMessagePublishResult
{
	#region Constructors

	public ChannelMessagePublishResult(
		string channelName,
		string type,
		IChannelMessage message,
		long elapsedTicks,
		int handlerCount,
		bool hadError,
		string errorMessage)
	{
		ChannelName = channelName ?? string.Empty;
		Type = type ?? string.Empty;
		Message = message;
		ElapsedTicks = elapsedTicks;
		HandlerCount = handlerCount;
		HadError = hadError;
		ErrorMessage = errorMessage ?? string.Empty;
	}

	#endregion

	#region Properties

	public string ChannelName { get; }

	public long ElapsedTicks { get; }

	public string ErrorMessage { get; }

	public bool HadError { get; }

	public int HandlerCount { get; }

	public IChannelMessage Message { get; }

	/// <summary>
	/// Message CLR type name (the operation id).
	/// </summary>
	public string Type { get; }

	#endregion
}
