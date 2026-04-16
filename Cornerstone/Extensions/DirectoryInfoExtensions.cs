#region References

using System;
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
	/// Safely create a directory.
	/// </summary>
	/// <param name="info"> The information on the directory to create. </param>
	public static bool SafeCreate(this DirectoryInfo info)
	{
		Utility.Retry(() =>
		{
			info.Refresh();

			if (!info.Exists)
			{
				info.Create();
			}
		});

		return Utility.WaitUntil(() =>
		{
			info.Refresh();
			return info.Exists;
		});
	}

	/// <summary>
	/// Safely delete a directory.
	/// </summary>
	/// <param name="info"> The information of the directory to delete. </param>
	public static bool SafeDelete(this DirectoryInfo info)
	{
		Utility.Retry(() =>
		{
			info.Refresh();

			if (info.Exists)
			{
				info.Delete();
			}
		});

		return Utility.WaitUntil(() =>
		{
			info.Refresh();
			return info.Exists;
		});
	}

	#endregion
}