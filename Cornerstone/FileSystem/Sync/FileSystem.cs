#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.FileSystem.Sync;

public static class FileSystem
{
	#region Methods

	public static bool CopyFile(SyncLog log, string source, string destination, bool speculative)
	{
		if (speculative)
		{
			log.AppendLine($"Would copy {source} to {destination}");
		}
		else
		{
			var destinationFolder = Path.GetDirectoryName(destination);

			try
			{
				if (!string.IsNullOrEmpty(destinationFolder))
				{
					Directory.CreateDirectory(destinationFolder);
				}

				File.Copy(source, destination, true);
				log.AppendLine($"Copy {source} to {destination}");
			}
			catch (Exception ex)
			{
				log.AppendLine($"Unable to copy {source} to {destination}: {ex.Message}");
				return false;
			}
		}

		return true;
	}

	public static bool CreateDirectory(SyncLog log, string newFolder, bool speculative)
	{
		if (speculative)
		{
			log.AppendLine("Would create " + newFolder);
		}
		else
		{
			try
			{
				Directory.CreateDirectory(newFolder);
				log.AppendLine("Create " + newFolder);
			}
			catch (Exception ex)
			{
				log.AppendLine($"Unable to create directory {newFolder}: {ex.Message}");
				return false;
			}
		}

		return true;
	}

	public static bool DeleteDirectory(SyncLog log, string deletedFolderPath, bool speculative)
	{
		if (speculative)
		{
			log.AppendLine("Would delete " + deletedFolderPath);
		}
		else
		{
			try
			{
				Directory.Delete(deletedFolderPath, true);
				log.AppendLine("Delete " + deletedFolderPath);
			}
			catch (Exception ex)
			{
				log.AppendLine($"Unable to delete directory {deletedFolderPath}: {ex.Message}");
				return false;
			}
		}

		return true;
	}

	public static bool DeleteFile(SyncLog log, string deletedFilePath, bool speculative)
	{
		if (speculative)
		{
			log.AppendLine("Would delete " + deletedFilePath);
		}
		else
		{
			try
			{
				// this can happen if the directory contents cache is out-of-date
				if (!File.Exists(deletedFilePath))
				{
					return true;
				}

				var attributes = File.GetAttributes(deletedFilePath);
				if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					File.SetAttributes(deletedFilePath, attributes & ~FileAttributes.ReadOnly);
				}

				File.Delete(deletedFilePath);
				log.AppendLine("Delete " + deletedFilePath);
			}
			catch (Exception ex)
			{
				log.AppendLine($"Unable to delete file {deletedFilePath}: {ex.Message}");
				return false;
			}
		}

		return true;
	}

	#endregion
}