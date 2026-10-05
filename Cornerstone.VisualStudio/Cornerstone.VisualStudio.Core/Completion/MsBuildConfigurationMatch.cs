using System;

namespace Cornerstone.VisualStudio.Core.Completion;

/// <summary>
/// Matches a project output to the active VS configuration (Debug vs Release).
/// Used so the XAML previewer does not pick bin\Release when Debug is selected.
/// </summary>
public static class MsBuildConfigurationMatch
{
	#region Methods

	/// <summary>
	/// True when this output belongs to <paramref name="activeConfiguration"/>.
	/// Unknown active configuration matches everything. A known active configuration
	/// does not fall back to another configuration.
	/// </summary>
	public static bool Matches(string activeConfiguration, string outputConfiguration, string targetPath)
	{
		if (string.IsNullOrWhiteSpace(activeConfiguration))
		{
			return true;
		}

		if (!string.IsNullOrWhiteSpace(outputConfiguration))
		{
			return string.Equals(outputConfiguration, activeConfiguration, StringComparison.OrdinalIgnoreCase);
		}

		return PathContainsConfigurationSegment(targetPath, activeConfiguration);
	}

	internal static bool PathContainsConfigurationSegment(string targetPath, string configuration)
	{
		if (string.IsNullOrWhiteSpace(targetPath) || string.IsNullOrWhiteSpace(configuration))
		{
			return false;
		}

		var needle = $"{System.IO.Path.DirectorySeparatorChar}{configuration}{System.IO.Path.DirectorySeparatorChar}";
		if (targetPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}

		needle = $"/{configuration}/";
		if (targetPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}

		needle = $"\\{configuration}\\";
		return targetPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	#endregion
}
