#region References

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
	/// Safely delete a file.
	/// </summary>
	/// <param name="info"> The information of the file to delete. </param>
	public static void SafeDelete(this FileInfo info)
	{
		if ((info == null) || !File.Exists(info.FullName))
		{
			return;
		}

		Utility.Retry(() =>
		{
			info.Refresh();

			if (info.Exists)
			{
				info.Delete();
			}
		}, 1000, 10);

		Utility.WaitUntil(() =>
		{
			info.Refresh();
			return !info.Exists;
		}, 1000, 10);
	}

	#endregion
}