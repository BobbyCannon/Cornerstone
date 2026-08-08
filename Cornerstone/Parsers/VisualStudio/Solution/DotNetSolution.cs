#region References

using System;
using System.IO;
using System.Xml.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

public class DotNetSolution : IDotNetSolution
{
	#region Constructors

	public DotNetSolution()
	{
		Items = new();
	}

	#endregion

	#region Properties

	public string Directory { get; set; }

	public string FilePath { get; private set; }

	/// <summary>
	/// Solution folders + their children (can be recursive)
	/// </summary>
	public SpeedyList<SolutionItem> Items { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Returns how many directory levels deep the path is, relative to the root of the drive/volume.
	/// Uses platform-correct separator. Counts folders only (not the final file name).
	/// </summary>
	/// <param name="path"> File or directory path (relative or absolute) </param>
	/// <returns> Number of parent directories from the root </returns>
	public static int GetDirectoryDepth(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return 0;
		}

		// Determine root to strip, avoiding CWD dependency for relative paths
		var root = Path.GetPathRoot(path);
		var target = string.IsNullOrEmpty(root) ? path : path[root.Length..];

		// Trim trailing separators to avoid counting an empty segment
		target = target.TrimEnd(Path.DirectorySeparatorChar);

		if (string.IsNullOrEmpty(target))
		{
			return 0;
		}

		// Use span iteration to avoid array allocation from Split
		var depth = 1;
		var startIndex = 0;
		while (startIndex < target.Length)
		{
			var index = target.IndexOf(Path.DirectorySeparatorChar, startIndex);
			if (index == -1)
			{
				break;
			}

			depth++;
			startIndex = index + 1;
		}

		return depth;
	}

	public static DotNetSolution Load(string filePath)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException("SLNX file not found", filePath);
		}

		var doc = XDocument.Load(filePath);
		var solutionDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
		var solution = new DotNetSolution
		{
			Directory = solutionDirectory,
			FilePath = Path.GetFullPath(filePath)
		};

		var root = doc.Root;
		if (root?.Name.LocalName != "Solution")
		{
			throw new InvalidDataException("Root element must be <Solution>");
		}

		foreach (var element in root.Elements())
		{
			solution.Items.Add(ParseSolutionItem(solutionDirectory, element));
		}

		return solution;
	}

	private static int CountSegments(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return 0;
		}

		var count = 1;
		var startIndex = 0;
		while (startIndex < path.Length)
		{
			var index = path.IndexOf(Path.DirectorySeparatorChar, startIndex);
			if (index == -1)
			{
				break;
			}

			count++;
			startIndex = index + 1;
		}

		return count;
	}

	private static (string relativePath, int level) GetPathAndLevel(string solutionDirectory, XElement element)
	{
		var relativePath = element.Attribute("Path")?.Value
			?? throw new InvalidDataException("Project missing Path attribute");

		// Combine and normalize path
		var combinedPath = Path.Combine(solutionDirectory, relativePath);
		var fullPath = Path.GetFullPath(combinedPath);
		var directory = Path.GetDirectoryName(fullPath) ?? solutionDirectory;

		// Calculate level relative to solution directory using spans to avoid allocations
		var relativeDir = GetRelativePathSpan(solutionDirectory, directory);
		var level = CountSegments(relativeDir);

		return (relativePath, level);
	}

	private static string GetRelativePathSpan(string solutionDirectory, string directory)
	{
		if (string.IsNullOrEmpty(solutionDirectory) || string.IsNullOrEmpty(directory))
		{
			return string.Empty;
		}

		// Fast path: identical directories
		if (solutionDirectory.Equals(directory, StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}

		// Use standard library but fallback to manual logic if needed for AOT safety
		var relativeDir = Path.GetRelativePath(solutionDirectory, directory);

		// Handle cases where paths are on different roots or identical
		if (string.IsNullOrEmpty(relativeDir) || (relativeDir == directory))
		{
			return string.Empty;
		}

		return relativeDir;
	}

	private static SolutionItem ParseSolutionItem(string solutionDirectory, XElement element)
	{
		var name = element.Name.LocalName;

		switch (name)
		{
			case "Project":
			{
				var (path, level) = GetPathAndLevel(solutionDirectory, element);
				return new SolutionItem
				{
					Name = path,
					ItemType = SolutionItemType.Project,
					Level = level
				};
			}
			case "Folder":
			{
				// Folders in .slnx often have a Name attribute (display name) and optionally a Path
				var displayName = element.Attribute("Name")?.Value;
				var folderPath = element.Attribute("Path")?.Value;

				// Calculate level based on Path if available, otherwise fallback to Name structure
				var level = 0;
				if (!string.IsNullOrEmpty(folderPath))
				{
					var cleanPath = folderPath.TrimEnd(Path.DirectorySeparatorChar);
					if (!string.IsNullOrEmpty(cleanPath))
					{
						level = CountSegments(cleanPath);
					}
				}
				else if (!string.IsNullOrEmpty(displayName))
				{
					level = CountSegments(displayName);
				}

				var folderItem = new SolutionItem
				{
					// Use Path if available for consistency, otherwise Name
					Name = folderPath ?? displayName ?? element.Name.LocalName,
					ItemType = SolutionItemType.Folder,
					Level = level
				};

				// Recursively load children (Projects, Files, or nested Folders)
				foreach (var childElement in element.Elements())
				{
					folderItem.Children.Add(ParseSolutionItem(solutionDirectory, childElement));
				}

				return folderItem;
			}
			case "File":
			{
				var (path, level) = GetPathAndLevel(solutionDirectory, element);
				return new SolutionItem
				{
					Name = path,
					ItemType = SolutionItemType.File,
					Level = level
				};
			}
			default:
			{
				return new SolutionItem
				{
					Name = element.Name.ToString(),
					ItemType = SolutionItemType.Unknown
				};
			}
		}
	}

	#endregion
}

public interface IDotNetSolution
{
	#region Properties

	/// <summary>
	/// The directory of the solution.
	/// </summary>
	string Directory { get; set; }

	#endregion
}