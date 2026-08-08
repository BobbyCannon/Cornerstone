#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.FileSystem.Sync;

/// <summary>
/// For large remote folders it can take hours just to enumerate the contents of the remote folder.
/// For unreliable connections restarting ContentSync will require re-reading the contents from
/// scratch again. To avoid this repeated cost, once the contents of the remote folder have been read
/// we flush it to disk. If at any time after that ContentSync encounters errors, the cache will persist
/// until ContentSync is invoked next time. However, upon successful completion the cache is cleared.
/// </summary>
public static class DirectoryContentsCache
{
	#region Fields

	private static readonly string _cacheRootFolder;
	private static readonly HashSet<string> _filesWritten;

	#endregion

	#region Constructors

	static DirectoryContentsCache()
	{
		_cacheRootFolder = Path.Combine(Path.GetTempPath(), "ContentSync");
		_filesWritten = new(StringComparer.OrdinalIgnoreCase);
	}

	#endregion

	#region Methods

	public static void ClearWrittenFilesFromCache()
	{
		foreach (var file in _filesWritten)
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception)
			{
				// Ignore errors?
			}
		}
	}

	public static void SaveToCache(string rootFolder, string pattern, HashSet<string> files, HashSet<string> folders)
	{
		if ((files.Count < 10000) && (folders.Count < 1000))
		{
			// don't bother caching small amounts of data to disk
			return;
		}

		Directory.CreateDirectory(_cacheRootFolder);
		GetCacheFilePaths(rootFolder, pattern, out var fileListFilePath, out var folderListFilePath);
		File.WriteAllLines(fileListFilePath, files.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
		File.WriteAllLines(folderListFilePath, folders.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
		_filesWritten.Add(fileListFilePath);
		_filesWritten.Add(folderListFilePath);
	}

	public static bool TryReadFromCache(string rootFolder, string pattern, HashSet<string> files, HashSet<string> folders)
	{
		if (!Directory.Exists(_cacheRootFolder))
		{
			return false;
		}

		GetCacheFilePaths(rootFolder, pattern, out var fileListFilePath, out var folderListFilePath);

		if (!File.Exists(fileListFilePath) || !File.Exists(folderListFilePath))
		{
			return false;
		}
		foreach (var line in File.ReadAllLines(fileListFilePath))
		{
			files.Add(line);
		}
		foreach (var line in File.ReadAllLines(folderListFilePath))
		{
			folders.Add(line);
		}

		// pretend we just wrote these files so that they can be deleted on successful completion
		_filesWritten.Add(fileListFilePath);
		_filesWritten.Add(folderListFilePath);

		return true;
	}

	private static void GetCacheFilePaths(string rootFolder, string pattern, out string fileListFilePath, out string folderListFilePath)
	{
		var hash = (rootFolder + pattern).ToMd5HashHexString();
		fileListFilePath = Path.Combine(_cacheRootFolder, hash + "_files.txt");
		folderListFilePath = Path.Combine(_cacheRootFolder, hash + "_folders.txt");
	}

	#endregion
}