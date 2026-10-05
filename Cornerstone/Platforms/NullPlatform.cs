#region References

using Cornerstone.Input;
using Cornerstone.Media;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Security.SecurityKeys;

#endregion

namespace Cornerstone.Platforms;

/// <summary>
/// No-op platform for TFMs without OS-specific implementations (e.g. plain net10.0).
/// </summary>
public class NullPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public NullPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
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
			DependencyProvider.AddTransient<AudioPlayer, AudioPlayerStub>();
			DependencyProvider.AddSingleton<Gamepad, GamepadStub>();
			DependencyProvider.AddSingleton<IKeepAlive, UnsupportedKeepAlive>();
			DependencyProvider.AddSingleton<Keyboard, KeyboardStub>();
			DependencyProvider.AddSingleton<Mouse, MouseStub>();
			DependencyProvider.AddSingleton<IPermissions, Permissions>();
			DependencyProvider.AddSingleton<SecurityCardReader, SecurityCardReaderStub>();
			DependencyProvider.AddSingleton<IWindowsHelloService, WindowsHelloServiceStub>();
		}

		base.InitializeLifecycle();
	}

	#endregion
}