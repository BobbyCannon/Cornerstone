#region References

using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for directory info
/// </summary>
public static class DirectoryInfoExtensions
{
	#region Methods

	/// <summary>
	/// Gets differences between source and destination directories.
	/// Returns files and directories in source that are missing in destination.
	/// </summary>
	/// <param name="source"> Source DirectoryInfo. </param>
	/// <param name="destination"> Destination DirectoryInfo. </param>
	/// <returns> List of relative paths that differ (missing in destination). </returns>
	public static List<string> GetDifferences(this DirectoryInfo source, DirectoryInfo destination)
	{
		var differences = new List<string>();

		if (!source.Exists)
		{
			return differences;
		}

		// Get all files in source recursively
		var sourceFiles = source.GetFiles("*", SearchOption.AllDirectories)
			.Select(f => Path.GetRelativePath(source.FullName, f.FullName));

		// Get all files in destination recursively
		var destFiles = destination.Exists
			? destination.GetFiles("*", SearchOption.AllDirectories)
				.Select(f => Path.GetRelativePath(destination.FullName, f.FullName))
			: [];

		// Files in source but not in destination
		differences.AddRange(sourceFiles.Except(destFiles));

		// Get directories in source not in destination
		var sourceDirs = source.GetDirectories("*", SearchOption.AllDirectories)
			.Select(d => Path.GetRelativePath(source.FullName, d.FullName));
		var destDirs = destination.Exists
			? destination.GetDirectories("*", SearchOption.AllDirectories)
				.Select(d => Path.GetRelativePath(destination.FullName, d.FullName))
			: [];

		differences.AddRange(sourceDirs.Except(destDirs));

		return differences;
	}

	/// <summary>
	/// Empties a directory of all the files and the directories.
	/// </summary>
	/// <param name="directory"> The directory to empty. </param>
	public static bool IsEmpty(this DirectoryInfo directory)
	{
		// See if the directory exists.
		return !directory.Exists || directory.EnumerateFileSystemInfos().Any();
	}

	/// <summary>
	/// Reverse lookup a file from a directory info.
	/// </summary>
	/// <param name="directoryInfo"> The directory to search. </param>
	/// <param name="fileName"> The file name to search for. </param>
	/// <returns> The file info if found otherwise null. </returns>
	public static FileInfo ReverseLookup(this DirectoryInfo directoryInfo, string fileName)
	{
		do
		{
			var files = directoryInfo.GetFiles(fileName);

			foreach (var file in files)
			{
				if (file.Name == fileName)
				{
					return file;
				}
			}

			directoryInfo = directoryInfo.Parent;
		} while (directoryInfo != null);

		return null;
	}

	/// <summary>
	/// Safely create a directory.
	/// </summary>
	/// <param name="info"> The information on the directory to create. </param>
	public static bool SafeCreate(this DirectoryInfo info)
	{
		Utility.Retry(() =>
		{
			info.Refresh();

			if (!info.Exists)
			{
				info.Create();
			}
		});

		return Utility.WaitUntil(() =>
		{
			info.Refresh();
			return info.Exists;
		});
	}

	/// <summary>
	/// Safely delete a directory.
	/// </summary>
	/// <param name="info"> The information of the directory to delete. </param>
	public static bool SafeDelete(this DirectoryInfo info)
	{
		Utility.Retry(() =>
		{
			info.Refresh();

			if (info.Exists)
			{
				info.Delete();
			}
		});

		return Utility.WaitUntil(() =>
		{
			info.Refresh();
			return info.Exists;
		});
	}

	#endregion
}