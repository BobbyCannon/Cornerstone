#region References

using System;
using System.IO;
using Cornerstone.FileSystem;
using Environment = Android.OS.Environment;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidFileService : FileService
{
	#region Methods

	/// <summary>
	/// Android's storage system uses Storage Access Framework (SAF).
	/// </summary>
	/// <param name="folderUri"> The SAF URI for the folder. </param>
	/// <returns> The folder path. </returns>
	public override string ConvertFolderUriToFolderPath(Uri folderUri)
	{
		// Android (Avalonia Folder Picker)
		//	- OriginalString content://com.android.externalstorage.documents/tree/primary%3AMusic
		//	- LocalPath /tree/primary:Music
		// Desired
		//	- 	/storage/emulated/0/Music

		// Check if the URI matches our expected format
		var prefix = "/tree/primary:";
		if (!folderUri.LocalPath.StartsWith(prefix))
		{
			return folderUri.AbsolutePath;
		}

		// Extract the directory name after "primary:"
		var directory = folderUri.LocalPath.Substring(prefix.Length);
		directory = Uri.UnescapeDataString(directory);

		var externalDirectory = Environment.ExternalStorageDirectory;
		var genericPath = Path.Combine(externalDirectory.AbsolutePath, directory);

		return genericPath;
	}

	#endregion
}