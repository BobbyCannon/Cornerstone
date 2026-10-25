using System;
using System.IO;

namespace Cornerstone.VisualStudio.Core.Preview;

/// <summary>
/// Project-relative path used as the preview document base URI.
/// Matches the csres name produced from the MSBuild item path: a leading slash
/// and forward slashes, such as /SourceControl/Views/RepositoryInfoView.cxaml.
/// </summary>
public static class XamlProjectPath
{
	#region Methods

	public static string FromProjectDirectory(string projectDirectory, string xamlPath)
	{
		if (string.IsNullOrWhiteSpace(projectDirectory) || string.IsNullOrWhiteSpace(xamlPath))
		{
			return null;
		}

		var fullXaml = Path.GetFullPath(xamlPath);
		var root = Path.GetFullPath(projectDirectory)
			.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var prefix = root + Path.DirectorySeparatorChar;
		if (!fullXaml.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var relative = fullXaml.Substring(prefix.Length).Replace('\\', '/');
		if (relative.Length == 0)
		{
			return null;
		}

		return "/" + relative;
	}

	#endregion
}
