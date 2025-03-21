#region References

using System.IO;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.FileSystem;

public class FolderManager : DirectoryOrFileInfo
{
	#region Constructors

	public FolderManager(string directoryPath, IDispatcher dispatcher)
		: this(new DirectoryInfo(directoryPath), dispatcher)
	{
	}

	public FolderManager(DirectoryInfo info, IDispatcher dispatcher)
		: base(null, dispatcher)
	{
		DirectoryInfo = info;
	}

	#endregion

	#region Methods

	public void ChangeDirectory(string directoryPath, bool refresh)
	{
		DirectoryInfo = new DirectoryInfo(directoryPath);
		Clear();

		if (refresh)
		{
			Refresh();
		}
	}

	#endregion
}