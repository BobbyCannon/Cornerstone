#region References

using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName.Keystone;

/// <summary>
/// Single source of truth for the application domain.
/// Keep this free of UI concerns.
/// </summary>
[SourceReflection]
public class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}
