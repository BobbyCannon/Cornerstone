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
public partial class LoggingChannel : KeystoneChannel
{
	#region Records

	[ChannelMessage<LoggingChannel>("Log")]
	public record struct LoggingMessage(string Message, LogLevel Level) : IChannelMessage;

	#endregion
}
