#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.IO;

/// <inheritdoc />
public class FolderListItem : TreeViewListItem
{
	#region Constructors

	/// <inheritdoc />
	public FolderListItem(string directoryPath, IDispatcher dispatcher)
		: this(Guid.NewGuid(), directoryPath, dispatcher)
	{
	}

	/// <inheritdoc />
	public FolderListItem(Guid id, string directoryPath, IDispatcher dispatcher)
		: base(id, dispatcher)
	{
		FolderPath = directoryPath;
		Name = string.IsNullOrWhiteSpace(directoryPath) ? string.Empty : new DirectoryInfo(directoryPath).Name;
		Children = new SpeedyList<TreeViewListItem>(dispatcher,
			new OrderBy<TreeViewListItem>(x => x is FolderListItem, true),
			new OrderBy<TreeViewListItem>(x => x is FileListItem, true),
			new OrderBy<TreeViewListItem>(x => x.Name)
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The child items for this folder.
	/// </summary>
	public SpeedyList<TreeViewListItem> Children { get; }

	/// <summary>
	/// The absolute path of the older.
	/// </summary>
	public string FolderPath { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Refresh the folder.
	/// </summary>
	public virtual void Refresh(IHierarchySettings settings = null)
	{
		var directoryInfo = new DirectoryInfo(FolderPath);
		settings ??= new HierarchySettings();

		Reconcile(directoryInfo, this, settings);
	}

	private void Reconcile(DirectoryInfo sourceDirectory, FolderListItem destination, IHierarchySettings settings)
	{
		var dispatcher = GetDispatcher();
		var sourceFolders = sourceDirectory.GetDirectories();
		var destinationFolders = destination.Children
			.Where(x => x is FolderListItem)
			.Cast<FolderListItem>()
			.ToList();

		foreach (var destinationFolder in destinationFolders)
		{
			var foundSource = sourceFolders.FirstOrDefault(x => x.FullName == destinationFolder.FolderPath);
			if (foundSource == null)
			{
				// Source not found so remove the folder
				destination.Children.Remove(destinationFolder);
			}
		}

		foreach (var sourceFolder in sourceFolders)
		{
			if (settings.ShouldExclude(sourceFolder))
			{
				continue;
			}

			var destinationFolder = destinationFolders.FirstOrDefault(x => x.FolderPath == sourceFolder.FullName);
			if (destinationFolder == null)
			{
				destinationFolder = new FolderListItem(sourceFolder.FullName, dispatcher);
				destination.Children.Add(destinationFolder);
			}
		}

		var sourceFiles = sourceDirectory.GetFiles();
		var destinationFiles = destination.Children
			.Where(x => x is FileListItem)
			.Cast<FileListItem>()
			.ToList();

		foreach (var destinationFile in destinationFiles)
		{
			var foundSource = sourceFiles.FirstOrDefault(x => x.FullName == destinationFile.FilePath);
			if (foundSource == null)
			{
				// Source not found so remove the file
				destination.Children.Remove(destinationFile);
			}
		}

		foreach (var sourceFile in sourceFiles)
		{
			var destinationFile = destinationFiles.FirstOrDefault(x => x.FilePath == sourceFile.FullName);
			if (destinationFile == null)
			{
				destinationFile = new FileListItem(sourceFile.FullName, dispatcher);
				destination.Children.Add(destinationFile);
			}
		}

		foreach (var folder in destination.Children)
		{
			(folder as FolderListItem)?.Refresh(settings);
		}
	}

	#endregion
}