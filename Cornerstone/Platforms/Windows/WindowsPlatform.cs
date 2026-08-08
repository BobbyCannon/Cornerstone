#region References

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Cornerstone.Data.Bytes;
using Cornerstone.Location;
using Cornerstone.Runtime;
using Cornerstone.Security;
using RuntimeInformation = Cornerstone.Runtime.RuntimeInformation;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsPlatform : CornerstoneObject, IPlatform
{
	#region Constructors

	public WindowsPlatform(DependencyProvider dependencyProvider, RuntimeInformation runtimeInformation)
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

	public override void LoadLifecycle()
	{
		UpdateDeviceOverrides();
		base.LoadLifecycle();
	}

	public static void OpenInFileManager(string path, bool select = true)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		var isDirectory = Directory.Exists(path);
		var fullPath = isDirectory ? new DirectoryInfo(path).FullName : new FileInfo(path).FullName;

		if (isDirectory || !select)
		{
			Process.Start("explorer", fullPath);
		}
		else
		{
			Process.Start("explorer", $"/select,\"{fullPath}\"");
		}
	}

	public static void OpenWithExplorer(string path)
	{
		Process.Start("explorer", path);
	}

	private void AddPlatformImplementations()
	{
		DependencyProvider.AddSingleton<ILocationProvider, WindowsLocationProvider>();
		DependencyProvider.AddSingleton<PlatformCredentialVault, WindowsPlatformCredentialVault>();
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	private void UpdateDeviceOverrides()
	{
		var primaryScreen = Screen.PrimaryScreen;
		if (primaryScreen != null)
		{
			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceDisplaySize), primaryScreen.Size);
			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceDisplayRefreshRate), primaryScreen.RefreshRate);
		}

		if (GetPhysicallyInstalledSystemMemory(out var memory))
		{
			RuntimeInformation.SetPlatformOverride(nameof(RuntimeInformation.DeviceMemory), ByteSize.FromKilobytes(memory));
		}
	}

	#endregion
}