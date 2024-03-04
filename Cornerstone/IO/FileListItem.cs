#region References

using System;
using System.IO;
using System.Windows.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.IO;

/// <inheritdoc />
public class FileListItem : TreeViewListItem
{
	#region Constructors

	/// <inheritdoc />
	public FileListItem(string filePath, IDispatcher dispatcher)
		: base(Guid.NewGuid(), dispatcher)
	{
		FilePath = filePath;
		Name = new FileInfo(filePath).Name;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The absolute path of the file.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// The command for requesting opening of the file.
	/// </summary>
	public ICommand OpenCommand { get; set; }

	#endregion
}