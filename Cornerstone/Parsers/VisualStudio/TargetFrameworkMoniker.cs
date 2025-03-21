#region References

using System.Text.RegularExpressions;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Parsers.VisualStudio;

/// <summary>
/// Represents the .NET project target framework.
/// </summary>
/// <remarks>
/// For further details on target frameworks see:
/// https://learn.microsoft.com/en-us/dotnet/standard/frameworks
/// https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-1-0
/// </remarks>
public class TargetFrameworkMoniker : Notifiable, ITargetFrameworkMoniker
{
	#region Fields

	private static readonly Regex _regex;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the target framework moniker.
	/// </summary>
	public TargetFrameworkMoniker()
	{
	}

	static TargetFrameworkMoniker()
	{
		_regex = new Regex("(?<t>(?<p>(uap|MonoAndroid|Xamarin.iOS)|(net[0-9]+.[0-9])|(net([0-9]+))|(netstandard[0-9].[0-9]))-?(?<o>(android|browser|ios|maccatalyst|macos|tvos|windows))?)(?<v>[0-9.]+)?", RegexOptions.Singleline);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Target Framework Moniker (TFM)
	/// ex. netcoreapp3.1, netstandard2.0, net462, net9.0-windows10.19041.0
	/// </summary>
	public string Moniker { get; set; }

	/// <summary>
	/// Any OS-specific binding. Supported in .NET 5+.
	/// Ex. net9.0-windows -> windows, net9.0-android12.0 -> android
	/// </summary>
	public string OperatingSystem { get; set; }

	/// <summary>
	/// The operating system version.
	/// Ex. net9.0-windows10.0.19041.0 -> "10.0.19041.0", net9.0-android12.0 -> 12.0
	/// </summary>
	public string OperatingSystemVersion { get; set; }

	/// <summary>
	/// The platform moniker.
	/// Ex. net9.0-windows -> net9.0
	/// </summary>
	public string PlatformMoniker { get; set; }

	/// <summary>
	/// The type moniker.
	/// Ex. net9.0-windows10.0.19041.0 -> net9.0-windows
	/// Ex. net9.0-windows -> net9.0-windows
	/// </summary>
	public string TypeMoniker { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Create a target framework moniker from the provided value.
	/// </summary>
	public static TargetFrameworkMoniker ShallowClone(ITargetFrameworkMoniker targetFramework)
	{
		var response = new TargetFrameworkMoniker();
		response.UpdateWith(targetFramework);
		return response;
	}

	/// <summary>
	/// Returns a string representation of <see cref="T:Cornerstone.Parsers.VisualStudio.ProjectOld.TargetFramework" />.
	/// </summary>
	/// <returns> String representation of <see cref="T:Cornerstone.Parsers.VisualStudio.ProjectOld.TargetFramework" />. </returns>
	public override string ToString()
	{
		return Moniker;
	}

	/// <summary>
	/// Update the TargetFrameworkMoniker with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(TargetFrameworkMoniker update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the TargetFrameworkMoniker with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the entity. </param>
	public virtual bool UpdateWith(ITargetFrameworkMoniker update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Moniker = update.Moniker;
			OperatingSystem = update.OperatingSystem;
			OperatingSystemVersion = update.OperatingSystemVersion;
			PlatformMoniker = update.PlatformMoniker;
			TypeMoniker = update.TypeMoniker;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Moniker)), x => x.Moniker = update.Moniker);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(OperatingSystem)), x => x.OperatingSystem = update.OperatingSystem);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(OperatingSystemVersion)), x => x.OperatingSystemVersion = update.OperatingSystemVersion);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(PlatformMoniker)), x => x.PlatformMoniker = update.PlatformMoniker);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(TypeMoniker)), x => x.TypeMoniker = update.TypeMoniker);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			TargetFrameworkMoniker value => UpdateWith(value, settings),
			ITargetFrameworkMoniker value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	internal virtual void ParseMoniker(string moniker)
	{
		var matches = _regex.Match(moniker);

		if (matches.Success)
		{
			Moniker = moniker;
			PlatformMoniker = matches.Groups["p"].Value;
			OperatingSystem = matches.Groups["o"].Value;
			OperatingSystemVersion = matches.Groups["v"].Value;
			TypeMoniker = matches.Groups["t"].Value;
		}
		else
		{
			Moniker = moniker;
			PlatformMoniker = string.Empty;
			OperatingSystem = string.Empty;
			OperatingSystemVersion = string.Empty;
			TypeMoniker = string.Empty;
		}
	}

	#endregion
}

/// <summary>
/// Represents a target framework moniker (TFM).
/// </summary>
public interface ITargetFrameworkMoniker
{
	#region Properties

	/// <summary>
	/// Target Framework Moniker (TFM)
	/// ex. netcoreapp3.1, netstandard2.0, net462, net9.0-windows10.19041.0
	/// </summary>
	string Moniker { get; set; }

	/// <summary>
	/// Any OS-specific binding. Supported in .NET 5+.
	/// Ex. net9.0-windows -> windows, net9.0-android12.0 -> android
	/// </summary>
	string OperatingSystem { get; set; }

	/// <summary>
	/// The operating system version.
	/// Ex. net9.0-windows10.0.19041.0 -> "10.0.19041.0", net9.0-android12.0 -> 12.0
	/// </summary>
	string OperatingSystemVersion { get; set; }

	/// <summary>
	/// The platform moniker.
	/// Ex. net9.0-windows -> net9.0
	/// </summary>
	string PlatformMoniker { get; set; }

	/// <summary>
	/// The type moniker.
	/// Ex. net9.0-windows10.0.19041.0 -> net9.0-windows
	/// Ex. net9.0-windows -> net9.0-windows
	/// </summary>
	string TypeMoniker { get; set; }

	#endregion
}