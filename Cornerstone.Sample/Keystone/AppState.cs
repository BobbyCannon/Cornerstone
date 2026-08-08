#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.State;

#endregion

namespace Cornerstone.Sample.Keystone;

/// <summary>
/// The primary app state. This will include only the primary data
/// and view state. This will not include all view state. Some minor
/// view state will be contained in the smaller views.
/// ex: control view state will exist there.
/// </summary>
[SourceReflection]
[DependencyInjected]
public partial class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(
		AppSettings settings,
		IRuntimeInformation runtimeInformation)
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