using System;
using System.Collections.Generic;
using System.IO;

namespace Cornerstone.VisualStudio.Core.Completion;

/// <summary>
/// Picks a single source path for Go To Definition when a symbol has many locations.
/// Prefers a non-generated file whose name matches the symbol.
/// </summary>
public static class GoToDefinitionLocationPicker
{
	#region Methods

	public static bool IsGeneratedPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return true;
		}

		var normalized = path.Replace('/', '\\');
		if (normalized.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}

		return normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
			|| normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
			|| normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
	}

	public static string PickPreferredPath(IEnumerable<string> paths, string symbolName)
	{
		if (paths == null)
		{
			return null;
		}

		string first = null;
		string firstNonGenerated = null;
		string matchingName = null;

		foreach (var path in paths)
		{
			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			if (first == null)
			{
				first = path;
			}

			if (IsGeneratedPath(path))
			{
				continue;
			}

			if (firstNonGenerated == null)
			{
				firstNonGenerated = path;
			}

			if (string.IsNullOrEmpty(symbolName))
			{
				continue;
			}

			var fileName = Path.GetFileNameWithoutExtension(path);
			if (string.Equals(fileName, symbolName, StringComparison.OrdinalIgnoreCase))
			{
				matchingName = path;
				break;
			}
		}

		return matchingName ?? firstNonGenerated ?? first;
	}

	#endregion
}
