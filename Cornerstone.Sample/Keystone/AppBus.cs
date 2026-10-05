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
		SettingsChannel settingsChannel,
		SyncChannel syncChannel,
		MeshChannel meshChannel)
	{
		Notification = Track(notificationChannel);
		Settings = Track(settingsChannel);
		Sync = Track(syncChannel);
		Mesh = Track(meshChannel);
	}

	#endregion

	#region Properties

	public MeshChannel Mesh { get; private set; }

	public NotificationChannel Notification { get; private set; }

	public SettingsChannel Settings { get; private set; }

	public SyncChannel Sync { get; private set; }

	#endregion
}