#region References

using Cornerstone.Keystone;
using Cornerstone.Navigator.Keystone.State;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Navigator.Keystone;

/// <summary>
/// Single source of truth for the application domain.
/// Keep this free of UI concerns.
/// </summary>
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