#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class SettingsChannel : KeystoneChannel
{
	#region Records

	[ChannelMessage<SettingsChannel>]
	public record struct SettingsLoadedMessage : IChannelMessage;

	#endregion
}