#region References

using System.IO;
using System.Linq;
using static Cornerstone.FileSystem.Sync.Utilities;

#endregion

namespace Cornerstone.FileSystem.Sync;

public static class FileSync
{
	#region Methods

	/// <summary>
	/// Assumes source directory exists. destination may or may not exist.
	/// </summary>
	public static DifferenceResults Compare(SyncLog log, string source, string destination, FileSyncSettings fileSyncSettings)
	{
		if (!Directory.Exists(destination))
		{
			FileSystem.CreateDirectory(log, destination, fileSyncSettings.WhatIf);
		}

		source = Paths.TrimSeparator(source);
		destination = Paths.TrimSeparator(destination);

		var results = Folders.DiffFolders(log,
			source,
			destination,
			fileSyncSettings.Pattern,
			fileSyncSettings.Recursive,
			fileSyncSettings.UpdateChangedFiles
		);

		return results;
	}

	/// <summary>
	/// Assumes source directory exists. destination may or may not exist.
	/// </summary>
	public static void CompareThenProcess(SyncLog log, string source, string destination, FileSyncSettings settings)
	{
		if (!Directory.Exists(destination))
		{
			FileSystem.CreateDirectory(log, destination, settings.WhatIf);
		}

		var results = Compare(log, source, destination, settings);
		Process(log, results, source, destination, settings);
	}

	/// <summary>
	/// Assumes source exists, destination may or may not exist.
	/// If it exists and is identical bytes to source, nothing is done.
	/// If it exists and is different, it is overwritten.
	/// If it doesn't exist, source is copied.
	/// </summary>
	public static void Files(SyncLog log, string source, string destination, FileSyncSettings fileSyncSettings)
	{
		if (File.Exists(destination) && FileComparer.AreContentsIdentical(source, destination))
		{
			log.AppendLine("File contents are identical.");
			return;
		}

		FileSystem.CopyFile(log, source, destination, fileSyncSettings.WhatIf);
	}

	/// <summary>
	/// Assumes source directory exists. destination may or may not exist.
	/// </summary>
	public static void Process(SyncLog log, DifferenceResults results, string source, string destination, FileSyncSettings settings)
	{
		var changesMade = false;
		var filesFailedToCopy = 0;
		var filesFailedToDelete = 0;
		var foldersFailedToCreate = 0;
		var foldersFailedToDelete = 0;

		if (settings.CopyLeftOnlyFiles)
		{
			using (log.MeasureTime("Copying new files"))
			{
				log.IncreaseIndent();

				foreach (var leftOnly in results.LeftOnlyFiles)
				{
					var destinationFilePath = destination + leftOnly;
					if (!FileSystem.CopyFile(log, source + leftOnly, destinationFilePath, settings.WhatIf))
					{
						filesFailedToCopy++;
					}

					changesMade = true;
				}

				log.DecreaseIndent();
			}
		}

		if (settings.UpdateChangedFiles)
		{
			using (log.MeasureTime("Updating changed files"))
			{
				log.IncreaseIndent();

				foreach (var changed in results.ChangedFiles)
				{
					var destinationFilePath = destination + changed;
					if (!FileSystem.CopyFile(log, source + changed, destinationFilePath, settings.WhatIf))
					{
						filesFailedToCopy++;
					}

					changesMade = true;
				}

				log.DecreaseIndent();
			}
		}

		if (settings.DeleteRightOnlyFiles)
		{
			using (log.MeasureTime("Deleting extra files"))
			{
				log.IncreaseIndent();

				foreach (var rightOnly in results.RightOnlyFiles)
				{
					var deletedFilePath = destination + rightOnly;
					if (!FileSystem.DeleteFile(log, deletedFilePath, settings.WhatIf))
					{
						filesFailedToDelete++;
					}

					changesMade = true;
				}

				log.DecreaseIndent();
			}
		}

		var foldersCreated = 0;
		if (settings.CopyEmptyDirectories)
		{
			using (log.MeasureTime("Creating folders"))
			{
				log.IncreaseIndent();

				foreach (var leftOnlyFolder in results.LeftOnlyFolders)
				{
					var newFolder = destination + leftOnlyFolder;
					if (!Directory.Exists(newFolder))
					{
						if (!FileSystem.CreateDirectory(log, newFolder, settings.WhatIf))
						{
							foldersFailedToCreate++;
						}
						else
						{
							foldersCreated++;
						}

						changesMade = true;
					}
				}

				log.DecreaseIndent();
			}
		}

		var foldersDeleted = 0;
		if (settings.DeleteRightOnlyDirectories)
		{
			using (log.MeasureTime("Deleting folders"))
			{
				log.IncreaseIndent();

				foreach (var rightOnlyFolder in results.RightOnlyFolders)
				{
					var deletedFolderPath = destination + rightOnlyFolder;
					if (!Directory.Exists(deletedFolderPath))
					{
						continue;
					}

					if (!FileSystem.DeleteDirectory(log, deletedFolderPath, settings.WhatIf))
					{
						foldersFailedToDelete++;
					}
					else
					{
						foldersDeleted++;
					}

					changesMade = true;
				}

				log.DecreaseIndent();
			}
		}

		if (results.LeftOnlyFiles.Any() && settings.CopyLeftOnlyFiles)
		{
			var count = results.LeftOnlyFiles.Count();
			var fileOrFiles = Pluralize("file", count);

			log.AppendLine(settings.WhatIf
				? $"Would have copied {count} new {fileOrFiles}"
				: $"{count} new {fileOrFiles} copied"
			);
		}

		if ((foldersCreated > 0) && settings.CopyEmptyDirectories)
		{
			var folderOrFolders = Pluralize("folder", foldersCreated);

			log.AppendLine(settings.WhatIf
				? $"Would have created {foldersCreated} {folderOrFolders}"
				: $"{foldersCreated} {folderOrFolders} created"
			);
		}

		if (results.ChangedFiles.Any() && settings.UpdateChangedFiles)
		{
			var count = results.ChangedFiles.Count();
			var fileOrFiles = Pluralize("file", count);
			
			log.AppendLine(settings.WhatIf
				? $"Would have updated {count} changed {fileOrFiles}"
				: $"{count} changed {fileOrFiles} updated"
			);
		}

		if (results.RightOnlyFiles.Any() && settings.DeleteRightOnlyFiles)
		{
			var count = results.RightOnlyFiles.Count();
			var fileOrFiles = Pluralize("file", count);

			log.AppendLine(settings.WhatIf
				? $"Would have deleted {count} right-only {fileOrFiles}"
				: $"{count} right-only {fileOrFiles} deleted"
			);
		}

		if ((foldersDeleted > 0) && settings.DeleteRightOnlyDirectories)
		{
			var folderOrFolders = Pluralize("folder", foldersDeleted);

			log.AppendLine(settings.WhatIf
				? $"Would have deleted {foldersDeleted} right-only {folderOrFolders}"
				: $"{foldersDeleted} right-only {folderOrFolders} deleted"
			);
		}

		if (results.IdenticalFiles.Any())
		{
			var count = results.IdenticalFiles.Count();
			var fileOrFiles = Pluralize("file", count);
			log.AppendLine($"{count} identical {fileOrFiles}");
		}

		if (filesFailedToCopy > 0)
		{
			log.AppendLine($"Failed to copy {filesFailedToCopy} {Pluralize("file", filesFailedToCopy)}");
		}

		if (filesFailedToDelete > 0)
		{
			log.AppendLine($"Failed to delete {filesFailedToDelete} {Pluralize("file", filesFailedToDelete)}.");
		}

		if (foldersFailedToCreate > 0)
		{
			log.AppendLine($"Failed to create {foldersFailedToCreate} {Pluralize("folder", foldersFailedToCreate)}.");
		}

		if (foldersFailedToDelete > 0)
		{
			log.AppendLine($"Failed to delete {foldersFailedToDelete} {Pluralize("folder", foldersFailedToDelete)}.");
		}

		if (!changesMade)
		{
			log.AppendLine(settings.WhatIf ? "Would have made no changes." : "Made no changes.");
		}

		// if there were no errors, delete the cache of the folder contents. Otherwise, chances are they're
		// going to restart the process, so we might need the cache next time.
		if ((filesFailedToCopy == 0) &&
			(filesFailedToDelete == 0) &&
			(foldersFailedToCreate == 0) &&
			(foldersFailedToDelete == 0))
		{
			DirectoryContentsCache.ClearWrittenFilesFromCache();
		}
	}

	#endregion
}