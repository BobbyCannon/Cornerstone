#region References

using Cornerstone.Keystone;
using Cornerstone.MediaPlayer.Keystone.State;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer.Keystone;

[SourceReflection]
public class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(AppSettings settings, IRuntimeInformation runtimeInformation)
	{
		Settings = Track(settings);
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	public AppSettings Settings { get; }

	#endregion
}
