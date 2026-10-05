using System;

namespace Cornerstone.VisualStudio.Core.Preview;

/// <summary>
/// Ranks executable projects that can host the XAML previewer. Lower is better.
/// </summary>
public static class PreviewHostSelector
{
	#region Constants

	public const string PreferredFrameworkDesktopHost = "Cornerstone.Sample.Desktop";

	#endregion

	#region Methods

	/// <summary>
	/// True for Cornerstone presentation framework libraries (not product apps).
	/// </summary>
	public static bool IsFrameworkPresentationLibrary(string xamlProjectName)
	{
		if (string.IsNullOrEmpty(xamlProjectName))
		{
			return false;
		}

		if (string.Equals(xamlProjectName, "Cornerstone.Presentation", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return xamlProjectName.StartsWith("Cornerstone.Presentation.", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Rank for a host candidate. 0 is the XAML project itself when it is an executable.
	/// </summary>
	public static int RankHost(PreviewHostRankContext context)
	{
		if (context == null)
		{
			return 9;
		}

		if (context.CandidateIsXamlProject)
		{
			return 0;
		}

		var xamlProjectName = context.XamlProjectName ?? string.Empty;
		var name = context.CandidateName ?? string.Empty;

		if (IsFrameworkPresentationLibrary(xamlProjectName) &&
			string.Equals(name, PreferredFrameworkDesktopHost, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (IsRelatedDesktopHostName(name, xamlProjectName))
		{
			return 2;
		}

		if (IsDesktopHostName(name, xamlProjectName))
		{
			return 3;
		}

		if (IsPlatformHostName(name, xamlProjectName))
		{
			return 4;
		}

		if (!string.IsNullOrEmpty(xamlProjectName) &&
			name.StartsWith(xamlProjectName + ".", StringComparison.OrdinalIgnoreCase))
		{
			return 5;
		}

		if (context.HasDesktopStack)
		{
			return 6;
		}

		if (context.IsStartupProject)
		{
			return 7;
		}

		if (context.DirectlyReferencesXamlProject)
		{
			return 8;
		}

		return 9;
	}

	/// <summary>
	/// Host named for this library: Foo.Desktop while editing Foo or Foo.Controls.
	/// </summary>
	public static bool IsRelatedDesktopHostName(string hostName, string xamlProjectName)
	{
		if (string.IsNullOrEmpty(hostName) || string.IsNullOrEmpty(xamlProjectName))
		{
			return false;
		}

		if (hostName.Equals(xamlProjectName + ".Desktop", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var root = GetProjectRootName(xamlProjectName);
		return !string.IsNullOrEmpty(root) &&
			hostName.Equals(root + ".Desktop", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Avalonia multi-platform convention: shared project + Something.Desktop host.
	/// </summary>
	public static bool IsDesktopHostName(string hostName, string xamlProjectName)
	{
		if (string.IsNullOrEmpty(hostName))
		{
			return false;
		}

		if (hostName.EndsWith(".Desktop", StringComparison.OrdinalIgnoreCase) ||
			hostName.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return IsRelatedDesktopHostName(hostName, xamlProjectName);
	}

	public static bool IsPlatformHostName(string hostName, string xamlProjectName)
	{
		if (string.IsNullOrEmpty(hostName))
		{
			return false;
		}

		var suffixes = new[] { ".Windows", ".Win", ".Mac", ".MacOS", ".Linux" };
		for (var i = 0; i < suffixes.Length; i++)
		{
			if (hostName.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		var root = GetProjectRootName(xamlProjectName);
		if (string.IsNullOrEmpty(root))
		{
			return false;
		}

		for (var i = 0; i < suffixes.Length; i++)
		{
			if (hostName.Equals(root + suffixes[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Album.Controls → Album; Album → Album.
	/// </summary>
	public static string GetProjectRootName(string projectName)
	{
		if (string.IsNullOrEmpty(projectName))
		{
			return projectName;
		}

		var dot = projectName.IndexOf('.');
		return dot > 0 ? projectName.Substring(0, dot) : projectName;
	}

	#endregion
}
