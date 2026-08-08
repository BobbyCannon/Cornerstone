namespace Cornerstone.VisualStudio.Models;

/// <summary>
/// Holds information about a <see cref="ProjectInfo" />'s outputs.
/// </summary>
public class ProjectOutputInfo
{
	#region Constructors

	public ProjectOutputInfo(
		string targetAssembly, string targetFramework, string targetFrameworkIdentifier, string hostApp, string runtimeIdentifier, string targetPlatformIdentifier)
	{
		TargetAssembly = targetAssembly;
		TargetFramework = targetFramework;
		TargetFrameworkIdentifier = targetFrameworkIdentifier;
		HostApp = hostApp;
		RuntimeIdentifier = runtimeIdentifier;
		TargetPlatformIdentifier = targetPlatformIdentifier;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the full path to the Avalonia.Designer.HostApp.dll to use.
	/// </summary>
	public string HostApp { get; }

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