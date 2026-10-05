#region References

using Cornerstone.Agent.Keystone.State;
using Cornerstone.Keystone;
using Cornerstone.Logging;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone;

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
		IDateTimeProvider dateTimeProvider,
		ModelState modelState,
		IRuntimeInformation runtimeInformation)
	{
		Logs = new Logger(dateTimeProvider);
		ModelState = modelState;
		RuntimeInformation = runtimeInformation;
		Settings = Track(settings);
	}

	#endregion

	#region Properties

	public Logger Logs { get; }
	public ModelState ModelState { get; }
	public IRuntimeInformation RuntimeInformation { get; }
	public AppSettings Settings { get; }

	#endregion
}