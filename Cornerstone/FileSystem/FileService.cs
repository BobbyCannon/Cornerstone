#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.FileSystem;

public class FileService
{
	#region Methods

	public virtual string ConvertFolderUriToFolderPath(Uri folderUri)
	{
		return folderUri.AbsolutePath;
	}

	public virtual void WriteAllText(string filePath, string text)
	{
		var directory = new DirectoryInfo(Path.GetDirectoryName(filePath));
		directory.Create();

		File.WriteAllText(filePath, text);
	}

	#endregion
}