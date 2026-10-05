#region References

using Cornerstone.Agent.Keystone.Channels;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppBus : KeystoneBus
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppBus(
		LoggingChannel loggingChannel,
		ModelsChannel modelsChannel,
		NotificationChannel notificationChannel,
		SettingsChannel settingsChannel)
	{
		Logging = Track(loggingChannel);
		Models = Track(modelsChannel);
		Notification = Track(notificationChannel);
		Settings = Track(settingsChannel);
	}

	#endregion

	#region Properties

	public LoggingChannel Logging { get; }
	public ModelsChannel Models { get; }
	public NotificationChannel Notification { get; }
	public SettingsChannel Settings { get; }

	#endregion
}