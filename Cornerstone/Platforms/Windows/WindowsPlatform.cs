#region References

using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Data.Bytes;
using Cornerstone.Input;
using Cornerstone.Location;
using Cornerstone.Media;
using Cornerstone.Security;
using Cornerstone.Security.SecurityKeys;
using RuntimeInformation = Cornerstone.Runtime.RuntimeInformation;

#endregion

namespace Cornerstone.Platforms.Windows;

public static class WindowsPlatform
{
	#region Properties

	public static DependencyProvider DependencyProvider { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(DependencyProvider dependencyProvider)
	{
		DependencyProvider = dependencyProvider;
		RuntimeInformation.SetPlatformOverride(x => x.DeviceDisplaySize, Screen.PrimaryScreen.Size);

		if (GetPhysicallyInstalledSystemMemory(out var memory))
		{
			RuntimeInformation.SetPlatformOverride(x => x.DeviceMemory, ByteSize.FromKilobytes(memory));
		}

		AddPlatformImplementations();
	}

	private static void AddPlatformImplementations()
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			// Will only work if the thread is STA
			DependencyProvider.AddSingleton<AudioPlayer, WindowsAudioPlayer>();
		}

		DependencyProvider.AddSingleton<ILocationProvider, WindowsLocationProvider>();
		DependencyProvider.AddSingleton<Gamepad, WindowsGamepad>();
		DependencyProvider.AddSingleton<Keyboard, WindowsKeyboard>();
		DependencyProvider.AddSingleton<Mouse, WindowsMouse>();
		DependencyProvider.AddSingleton<SmartCardReader, WindowsSmartCardReader>();
		DependencyProvider.AddSingleton<PlatformCredentialVault, WindowsPlatformCredentialVault>();
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	#endregion
}