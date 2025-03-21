namespace Cornerstone.PowerShell;

public static class PowerShellPlatform
{
	#region Properties

	public static IDependencyProvider DependencyProvider { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(IDependencyProvider dependencyProvider)
	{
		DependencyProvider = dependencyProvider ?? new DependencyProvider("Cornerstone PowerShell ");
	}

	#endregion
}