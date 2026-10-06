#region References

using Cornerstone.Input;
using Cornerstone.Location;
using Cornerstone.Media;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Security.SecurityKeys;

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
		DependencyProvider.AddTransient<AudioPlayer, AudioPlayerStub>();
		DependencyProvider.AddSingleton<Gamepad, GamepadStub>();
		DependencyProvider.AddSingleton<IKeepAlive, UnsupportedKeepAlive>();
		DependencyProvider.AddSingleton<Keyboard, KeyboardStub>();
		DependencyProvider.AddSingleton<Mouse, MouseStub>();
		DependencyProvider.AddSingleton<ILocationProvider, BrowserLocationProvider>();
		DependencyProvider.AddSingleton<IPermissions, BrowserPermissions>();
		DependencyProvider.AddSingleton<SecurityCardReader, SecurityCardReaderStub>();
		DependencyProvider.AddSingleton<IWindowsHelloService, WindowsHelloServiceStub>();
	}

	#endregion
}