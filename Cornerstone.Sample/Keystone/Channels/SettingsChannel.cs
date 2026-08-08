#region References

using Cornerstone.Keystone;
using Cornerstone.Keystone.Messages;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.Channels;

[SourceReflection]
[DependencyInjected]
public partial class SettingsChannel : KeystoneChannel<SettingsChannel.SettingsMessageType>
{
	#region Methods

	[ChannelSubscription<SettingsMessageType>(SettingsMessageType.SettingsLoaded)]
	public void SettingsLoaded()
	{
		Publish(SettingsMessageType.SettingsLoaded);
	}

	#endregion

	#region Enumerations

	public enum SettingsMessageType
	{
		Unknown,
		SettingsLoaded
	}

	#endregion
}