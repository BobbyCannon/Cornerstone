#region References

using System;
using Cornerstone.Input;
using Cornerstone.Media;
#if !ANDROID && !BROWSER && !IOS && !WINDOWS
using Cornerstone.Platforms.Linux;
using Cornerstone.Platforms.MacOS;
using Cornerstone.Platforms.Windows;
#endif
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
			DependencyProvider.AddSingleton<IBiometricKeyService, BiometricKeyServiceStub>();
			#if ANDROID || BROWSER || IOS || WINDOWS
			DependencyProvider.AddSingleton<Gamepad, GamepadStub>();
			#else
			if (OperatingSystem.IsLinux())
			{
				DependencyProvider.AddTransient<Gamepad, LinuxGamepad>();
			}
			else if (OperatingSystem.IsMacOS())
			{
				DependencyProvider.AddTransient<Gamepad, MacOSGamepad>();
			}
			else
			{
				DependencyProvider.AddSingleton<Gamepad, GamepadStub>();
			}

			if (OperatingSystem.IsWindows())
			{
				DependencyProvider.AddSingleton<IScreenSampler, WindowsScreenSampler>();
			}
			else if (OperatingSystem.IsLinux())
			{
				DependencyProvider.AddSingleton<IScreenSampler, LinuxScreenSampler>();
			}
			else if (OperatingSystem.IsMacOS())
			{
				DependencyProvider.AddSingleton<IScreenSampler, MacOSScreenSampler>();
			}
			else
			{
				DependencyProvider.AddSingleton<IScreenSampler, ScreenSamplerStub>();
			}
			#endif
			DependencyProvider.AddSingleton<IKeepAlive, UnsupportedKeepAlive>();
			DependencyProvider.AddSingleton<Keyboard, KeyboardStub>();
			DependencyProvider.AddSingleton<Mouse, MouseStub>();
			DependencyProvider.AddSingleton<IPermissions, Permissions>();
			DependencyProvider.AddSingleton<SecurityCardReader, SecurityCardReaderStub>();
		}

		base.InitializeLifecycle();
	}

	#endregion
}