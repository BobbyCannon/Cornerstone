#region References

using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Data.Bytes;
using Cornerstone.Input;
using Cornerstone.Location;
using Cornerstone.Media;
using RuntimeInformation = Cornerstone.Runtime.RuntimeInformation;

#endregion

namespace Cornerstone.Windows;

public static class WindowsPlatform
{
	#region Methods

	public static void Initialize(DependencyProvider dependencyProvider)
	{
		RuntimeInformation.SetPlatformOverride(x => x.DeviceDisplaySize, Screen.PrimaryScreen.Size);

		if (GetPhysicallyInstalledSystemMemory(out var memory))
		{
			RuntimeInformation.SetPlatformOverride(x => x.DeviceMemory, ByteSize.FromKilobytes(memory));
		}

		AddPlatformImplementations(dependencyProvider);
	}

	private static void AddPlatformImplementations(DependencyProvider dependencyProvider)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			// Will only work if the thread is STA
			dependencyProvider.AddSingleton<AudioPlayer, WindowsAudioPlayer>();
		}

		dependencyProvider.AddSingleton<ILocationProvider, WindowsLocationProvider>();
		dependencyProvider.AddSingleton<Keyboard, WindowsKeyboard>();
		dependencyProvider.AddSingleton<Mouse, WindowsMouse>();
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	#endregion
}