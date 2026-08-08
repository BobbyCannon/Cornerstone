#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.FileSystem.Sync;

public class Folders
{
	#region Methods

	/// <summary>
	/// Assumes leftRoot is an existing folder. rightRoot may not exist if operating in speculative mode.
	/// </summary>
	public static DifferenceResults DiffFolders(SyncLog log, string leftRoot, string rightRoot,
		string pattern, bool recursive = true, bool compareContents = true)
	{
		var leftRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var leftOnlyFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using (log.MeasureTime("Scanning source directory"))
		{
			GetRelativePathsOfAllFiles(leftRoot, pattern, recursive, leftRelativePaths, leftOnlyFolders);
		}

		var rightRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var rightOnlyFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (Directory.Exists(rightRoot))
		{
			using (log.MeasureTime("Scanning destination directory"))
			{
				GetRelativePathsOfAllFiles(rightRoot, pattern, recursive, rightRelativePaths, rightOnlyFolders);
			}
		}

		var leftOnlyFiles = new List<string>();
		var identicalFiles = new List<string>();
		var changedFiles = new List<string>();
		var rightOnlyFiles = new HashSet<string>(rightRelativePaths, StringComparer.OrdinalIgnoreCase);
		var commonFolders = leftOnlyFolders.Intersect(rightOnlyFolders, StringComparer.OrdinalIgnoreCase).ToArray();
		var current = 0;

		leftOnlyFolders.ExceptWith(commonFolders);
		rightOnlyFolders.ExceptWith(commonFolders);

		using (log.MeasureTime("Comparing"))
		{
			Parallel.ForEach(
				leftRelativePaths,
				new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				left =>
				{
					var leftFullPath = leftRoot + left;
					var rightFullPath = rightRoot + left;
					var rightContains = rightRelativePaths.Contains(left);

					if (rightContains)
					{
						bool areSame;

						try
						{
							areSame = !compareContents || FileComparer.AreContentsIdentical(leftFullPath, rightFullPath);
						}
						catch (Exception ex)
						{
							log.AppendLine(ex.ToString());
							return;
						}

						if (areSame)
						{
							lock (identicalFiles)
							{
								identicalFiles.Add(left);
							}
						}
						else
						{
							lock (changedFiles)
							{
								changedFiles.Add(left);
							}
						}
					}
					else
					{
						lock (leftOnlyFiles)
						{
							leftOnlyFiles.Add(left);
						}
					}

					lock (rightOnlyFiles)
					{
						rightOnlyFiles.Remove(left);
					}

					Interlocked.Increment(ref current);
				});
		}

		using (log.MeasureTime("Sorting"))
		{
			leftOnlyFiles.Sort();
			identicalFiles.Sort();
			changedFiles.Sort();

			return new DifferenceResults(changedFiles,
				identicalFiles,
				leftRelativePaths,
				leftOnlyFiles,
				leftOnlyFolders.OrderBy(s => s).ToArray(),
				rightRelativePaths,
				rightOnlyFiles.OrderBy(s => s).ToArray(),
				rightOnlyFolders.OrderBy(s => s).ToArray());
		}
	}

	public static void GetRelativePathsOfAllFiles(string rootFolder, string pattern, bool recursive, HashSet<string> files, HashSet<string> folders)
	{
		// don't go through the cache for non-recursive case
		if (recursive && DirectoryContentsCache.TryReadFromCache(rootFolder, pattern, files, folders))
		{
			return;
		}

		var rootDirectoryInfo = new DirectoryInfo(rootFolder);
		var prefixLength = rootFolder.Length;
		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		var fileSystemInfos = rootDirectoryInfo.EnumerateFileSystemInfos(pattern, searchOption);
		foreach (var fileSystemInfo in fileSystemInfos)
		{
			var relativePath = fileSystemInfo.FullName;
			relativePath = relativePath[prefixLength..];
			if (fileSystemInfo is FileInfo)
			{
				files.Add(relativePath);
			}
			else if (recursive)
			{
				folders.Add(relativePath);
			}
		}

		if (recursive)
		{
			DirectoryContentsCache.SaveToCache(rootFolder, pattern, files, folders);
		}
	}

	#endregion
}