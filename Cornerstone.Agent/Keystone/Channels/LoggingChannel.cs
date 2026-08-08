#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Logging;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class LoggingChannel : KeystoneChannel<LoggingChannel.LoggingMessageType>
{
	#region Methods

	[ChannelSubscription<LoggingMessageType, LoggingMessage>(LoggingMessageType.Log)]
	public void Log(string message, LogLevel level)
	{
		Publish(LoggingMessageType.Log, new LoggingMessage(message, level));
	}

	#endregion

	#region Records

	public record struct LoggingMessage(string Message, LogLevel Level) : IChannelMessage;

	#endregion

	#region Enumerations

	public enum LoggingMessageType
	{
		Log
	}

	#endregion
}