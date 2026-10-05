#region References

using Cornerstone.Data;
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
public partial class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		Greeting = "Welcome to Cornerstone + Keystone";
	}

	#endregion

	#region Properties

	/// <summary>
	/// Text the home view shows. Processors write it. The view binds a copy on the view model.
	/// </summary>
	[Notify]
	public partial string Greeting { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion
}
