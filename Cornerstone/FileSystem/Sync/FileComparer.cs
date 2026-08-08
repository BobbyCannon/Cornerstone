#region References

using System.IO;

#endregion

namespace Cornerstone.FileSystem.Sync;

public class FileComparer
{
	#region Methods

	/// <summary>
	/// Assumes both files already exist.
	/// </summary>
	public static bool AreContentsIdentical(string filePath1, string filePath2)
	{
		var fileInfo1 = new System.IO.FileInfo(filePath1);
		var fileInfo2 = new System.IO.FileInfo(filePath2);

		if (fileInfo1.Length != fileInfo2.Length)
		{
			return false;
		}

		const int bufferSize = 4096;
		var buffer1 = new byte[bufferSize];
		var buffer2 = new byte[bufferSize];

		using (var stream1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
		using (var stream2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
		{
			while (stream1.Position < stream1.Length)
			{
				var bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
				var bytesRead2 = stream2.Read(buffer2, 0, bufferSize);
				if (bytesRead1 != bytesRead2)
				{
					// can this ever happen?
					return false;
				}

				for (var i = 0; i < bytesRead1; i++)
				{
					if (buffer1[i] != buffer2[i])
					{
						return false;
					}
				}
			}
		}

		return true;
	}

	#endregion
}