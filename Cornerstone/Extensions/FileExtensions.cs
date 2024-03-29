﻿#region References

using System.IO;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// File system extensions (file / directories)
/// </summary>
public static class FileExtensions
{
	#region Methods

	/// <summary>
	/// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
	/// </summary>
	/// <param name="file"> The file information. </param>
	/// <returns> A byte array containing the contents of the file. </returns>
	public static byte[] ReadAllBytes(this FileInfo file)
	{
		return File.ReadAllBytes(file.FullName);
	}

	/// <summary>
	/// Safely create a file.
	/// </summary>
	/// <param name="file"> The information of the file to create. </param>
	public static void SafeCreate(this FileInfo file)
	{
		file.Refresh();
		if (file.Exists)
		{
			return;
		}

		UtilityExtensions.Retry(() =>
		{
			if (file.Exists)
			{
				return;
			}

			File.Create(file.FullName).Dispose();
		}, 1000, 10);

		UtilityExtensions.WaitUntil(() =>
		{
			file.Refresh();
			return file.Exists;
		}, 1000, 10);
	}

	/// <summary>
	/// Safely delete a file.
	/// </summary>
	/// <param name="info"> The information of the file to delete. </param>
	public static void SafeDelete(this FileInfo info)
	{
		UtilityExtensions.Retry(() =>
		{
			info.Refresh();

			if (info.Exists)
			{
				info.Delete();
			}
		}, 1000, 10);

		UtilityExtensions.WaitUntil(() =>
		{
			info.Refresh();
			return !info.Exists;
		}, 1000, 10);
	}

	/// <summary>
	/// Safely move a file.
	/// </summary>
	/// <param name="fileLocation"> The information of the file to move. </param>
	/// <param name="newLocation"> The location to move the file to. </param>
	/// <param name="overwrite"> Option to overwrite the destination file if it already exists. Defaults to false. </param>
	/// <param name="timeout"> The timeout to stop retrying. </param>
	/// <param name="delay"> The delay between retries. </param>
	public static void SafeMove(this FileInfo fileLocation, FileInfo newLocation, bool overwrite = false, int timeout = 1000, int delay = 10)
	{
		fileLocation.Refresh();
		if (!fileLocation.Exists)
		{
			throw new FileNotFoundException("The file could not be found.", fileLocation.FullName);
		}

		// Try to ensure the directory exist
		newLocation.Directory?.SafeCreate();

		// Must clone file info because MoveTo will change full name
		var movingFileLocation = new FileInfo(fileLocation.FullName);

		#if (NET6_0_OR_GREATER)
		UtilityExtensions.Retry(() => movingFileLocation.MoveTo(newLocation.FullName, overwrite), timeout, delay);
		#else
		if (overwrite && newLocation.Exists)
		{
			newLocation.SafeDelete();
		}

		UtilityExtensions.Retry(() => movingFileLocation.MoveTo(newLocation.FullName), timeout, delay);
		#endif

		UtilityExtensions.WaitUntil(() =>
		{
			fileLocation.Refresh();
			newLocation.Refresh();
			return !fileLocation.Exists && newLocation.Exists;
		}, timeout, delay);
	}

	internal static FileStream OpenAndCopyTo(this FileStream from, FileInfo to, int timeout)
	{
		lock (from)
		{
			var response = UtilityExtensions.Retry(() => File.Open(to.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.None), timeout, 50);
			from.Position = 0;
			from.CopyTo(response);
			response.Flush(true);
			response.Position = 0;
			return response;
		}
	}

	/// <summary>
	/// Open the file with read/write permission with file read share.
	/// </summary>
	/// <param name="info"> The information for the file. </param>
	/// <returns> The stream for the file. </returns>
	internal static FileStream OpenFile(this FileInfo info)
	{
		return UtilityExtensions.Retry(() => File.Open(info.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read), 1000, 50);
	}

	#endregion
}