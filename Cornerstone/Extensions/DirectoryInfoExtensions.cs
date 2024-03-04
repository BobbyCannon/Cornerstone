#region References

using System.IO;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for directory info
/// </summary>
public static class DirectoryInfoExtensions
{
	#region Methods

	/// <summary>
	/// Empties a directory of all the files and the directories.
	/// </summary>
	/// <param name="directory"> The directory to empty. </param>
	public static void Empty(this DirectoryInfo directory)
	{
		// See if the directory exists.
		if (!directory.Exists)
		{
			return;
		}

		directory.SafeDelete();
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
		UtilityExtensions.Retry(() =>
		{
			info.Refresh();

			if (!info.Exists)
			{
				info.Create();
			}
		}, 1000, 10);

		return UtilityExtensions.WaitUntil(() =>
		{
			info.Refresh();
			return info.Exists;
		}, 1000, 10);
	}

	/// <summary>
	/// Safely delete a directory.
	/// </summary>
	/// <param name="info"> The information of the directory to delete. </param>
	/// <param name="timeout"> The timeout to stop retrying. </param>
	/// <param name="delay"> The delay between retries. </param>
	public static void SafeDelete(this DirectoryInfo info, int timeout = 1000, int delay = 10)
	{
		UtilityExtensions.Retry(() =>
		{
			info.Refresh();

			if (info.Exists)
			{
				info.Delete(true);
			}
		}, timeout, delay);

		UtilityExtensions.WaitUntil(() =>
		{
			info.Refresh();
			return !info.Exists;
		}, timeout, delay);
	}

	#endregion
}