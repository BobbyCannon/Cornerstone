#region References

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

public static class WindowsPlatform
{
	#region Properties

	public static DependencyProvider DependencyProvider { get; private set; }

	public static IRuntimeInformation RuntimeInformation { get; private set; }

	#endregion

	#region Methods

	public static void Initialize(DependencyProvider dependencyProvider, IRuntimeInformation runtimeInformation)
	{
		DependencyProvider = dependencyProvider;
		RuntimeInformation = runtimeInformation;

		AddPlatformImplementations();
	}

	public static void OpenInFileManager(string path, bool select = true)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		var fullPath = File.Exists(path) ? new FileInfo(path).FullName : new DirectoryInfo(path).FullName;
		Process.Start("explorer", select ? $"/select,\"{fullPath}\"" : fullPath);
	}

	public static void OpenWithExplorer(string path)
	{
		Process.Start("explorer", path);
	}

	private static void AddPlatformImplementations()
	{
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);

	#endregion
}