#region References

using Cornerstone.Location;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Browser;

public class BrowserPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public BrowserPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
	{
		DependencyProvider = dependencyProvider;
		RuntimeInformation = runtimeInformation;
	}

	#endregion

	#region Properties

	public DependencyProvider DependencyProvider { get; }

	public RuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		if (!IsLifecycleInitialized())
		{
			AddPlatformImplementations();
		}

		base.InitializeLifecycle();
	}

	private void AddPlatformImplementations()
	{
		//DependencyProvider.AddTransient<Gamepad, BrowserGamepad>();
		DependencyProvider.AddSingleton<ILocationProvider, BrowserLocationProvider>();

		//DependencyProvider.AddSingleton<IPermissions, BrowserPermissions>();
	}

	#endregion
}