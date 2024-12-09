#region References

using System.IO;

#endregion

namespace Cornerstone.Storage;

public static class StorageService
{
	#region Methods

	public static void WriteAllText(string filePath, string text)
	{
		var directory = new DirectoryInfo(Path.GetDirectoryName(filePath));
		directory.Create();

		File.WriteAllText(filePath, text);
	}

	#endregion
}