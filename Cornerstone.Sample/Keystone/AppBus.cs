#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.Channels;

#endregion

namespace Cornerstone.Sample.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus(
		NotificationChannel notificationChannel,
		SettingsChannel settingsChannel)
	{
		Notification = Track(notificationChannel);
		Settings = Track(settingsChannel);
	}

	#endregion

	#region Properties

	public NotificationChannel Notification { get; private set; }

	public SettingsChannel Settings { get; private set; }

	#endregion
}