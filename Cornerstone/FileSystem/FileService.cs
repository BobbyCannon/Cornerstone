#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.FileSystem;

[SourceReflection]
public class FileService
{
	#region Methods

	public virtual string ConvertFolderUriToFolderPath(Uri folderUri)
	{
		return folderUri.AbsolutePath;
	}

	public virtual IEnumerable<FileDetails> GetFiles(string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			throw new ArgumentException("Directory path cannot be null or empty.", nameof(directory));
		}

		var directoryInfo = new DirectoryInfo(directory);

		if (!directoryInfo.Exists)
		{
			return Enumerable.Empty<FileDetails>();
		}

		return directoryInfo.GetFiles()
			.Select(fileInfo => new FileDetails
			{
				DisplayPath = fileInfo.FullName,
				Extension = fileInfo.Extension,
				Identifier = fileInfo.FullName,
				LastModified = fileInfo.LastWriteTimeUtc,
				Length = fileInfo.Length,
				Name = fileInfo.Name
			});
	}

	public virtual byte[] ReadBytes(string filePath)
	{
		return File.ReadAllBytes(filePath);
	}

	public virtual void WriteAllText(string filePath, string text)
	{
		var directory = new DirectoryInfo(Path.GetDirectoryName(filePath));
		directory.Create();

		File.WriteAllText(filePath, text);
	}

	#endregion
}