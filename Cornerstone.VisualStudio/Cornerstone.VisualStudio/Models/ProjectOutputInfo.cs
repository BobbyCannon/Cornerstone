#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.Models;

/// <summary>
/// Holds information about a <see cref="ProjectInfo" />'s outputs.
/// </summary>
public class ProjectOutputInfo
{
	#region Constructors

	public ProjectOutputInfo(
		string targetAssembly,
		string targetFramework,
		string targetFrameworkIdentifier,
		string hostApp,
		string runtimeIdentifier,
		string targetPlatformIdentifier,
		string cornerstoneHostApp = null,
		string configuration = null)
	{
		TargetAssembly = targetAssembly;
		TargetFramework = targetFramework;
		TargetFrameworkIdentifier = targetFrameworkIdentifier;
		AvaloniaHostApp = hostApp;
		CornerstoneHostApp = cornerstoneHostApp;
		RuntimeIdentifier = runtimeIdentifier;
		TargetPlatformIdentifier = targetPlatformIdentifier;
		Configuration = configuration;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the full path to Avalonia.Designer.HostApp.dll.
	/// </summary>
	public string AvaloniaHostApp { get; }

	/// <summary>
	/// MSBuild Configuration dimension (Debug, Release, …).
	/// </summary>
	public string Configuration { get; }

	/// <summary>
	/// Gets the full path to Cornerstone.Designer.HostApp.dll.
	/// </summary>
	public string CornerstoneHostApp { get; internal set; }

	/// <summary>
	/// True when this output can run the desktop designer host (.NET Core / Framework, or a net* TFM
	/// when the identifier was not available in design-time evaluation).
	/// </summary>
	public bool IsDesignerRunnableFramework
	{
		get
		{
			if (IsNetCore || IsNetFramework)
			{
				return true;
			}

			var tf = TargetFramework;
			if (string.IsNullOrEmpty(tf) ||
				tf.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return tf.StartsWith("net", StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	/// Host app for the given preview platform.
	/// </summary>
	public string HostAppFor(Services.XamlPreviewPlatform platform)
	{
		return platform == Services.XamlPreviewPlatform.Cornerstone ? CornerstoneHostApp : AvaloniaHostApp;
	}

	/// <summary>
	/// Gets a value indicating whether the target framework is .NET Core.
	/// </summary>
	public bool IsNetCore => FrameworkInformation.IsNetCoreApp(TargetFrameworkIdentifier);

	/// <summary>
	/// Gets a value indicating whether the target framework is .NET Framework.
	/// </summary>
	public bool IsNetFramework => FrameworkInformation.IsNetFramework(TargetFrameworkIdentifier);

	/// <summary>
	/// Gets a value indicating whether the target framework is .NET Standard.
	/// </summary>
	public bool IsNetStandard => FrameworkInformation.IsNetStandard(TargetFrameworkIdentifier);

	/// <summary>
	/// Gets the RuntimeIdentifier of the project.
	/// </summary>
	public string RuntimeIdentifier { get; }

	/// <summary>
	/// Gets the full path to the target assembly for the output.
	/// </summary>
	public string TargetAssembly { get; }

	/// <summary>
	/// Gets the friendly name of framework for the output.
	/// </summary>
	public string TargetFramework { get; }

	/// <summary>
	/// Gets the long name of framework for the output.
	/// </summary>
	public string TargetFrameworkIdentifier { get; }

	public string TargetPlatformIdentifier { get; }

	#endregion
}