#region References

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.FileSystem;

public class DirectoryOrFileInfo : SpeedyTree<DirectoryOrFileInfo>
{
	#region Constructors

	protected DirectoryOrFileInfo(DirectoryOrFileInfo parent, IDispatcher dispatcher)
		: base(parent, dispatcher,
			new OrderBy<DirectoryOrFileInfo>(x => x.IsDirectory, true),
			new OrderBy<DirectoryOrFileInfo>(x => x.Name)
		)
	{
	}

	#endregion

	#region Properties

	public DirectoryInfo DirectoryInfo { get; protected set; }

	public FileInfo FileInfo { get; protected set; }

	public bool IsDirectory => FileInfo == null;

	public bool IsExpanded { get; set; }

	public string Name => FileInfo?.Name ?? DirectoryInfo.Name;

	#endregion

	#region Methods

	public static DirectoryOrFileInfo Create(DirectoryInfo info, DirectoryOrFileInfo parent)
	{
		var response = new DirectoryOrFileInfo(parent, parent.GetDispatcher()) { DirectoryInfo = info };
		return response;
	}

	public static DirectoryOrFileInfo Create(FileInfo info, DirectoryOrFileInfo parent)
	{
		var response = new DirectoryOrFileInfo(parent, parent.GetDispatcher())
		{
			DirectoryInfo = info.Directory,
			FileInfo = info
		};
		return response;
	}

	public void Refresh()
	{
		if (!IsDirectory)
		{
			#if !ANDROID
			FileInfo?.Refresh();
			#endif
			return;
		}

		DirectoryInfo.Refresh();

		var results = new List<DirectoryOrFileInfo>();
		var infos = DirectoryInfo
			.GetDirectories()
			.Select(x => Create(x, this))
			.ToArray();

		var files = DirectoryInfo
			.GetFiles()
			.Select(x => Create(x, this))
			.ToArray();

		results.AddRange(infos);
		results.AddRange(files);

		Load(results);
	}

	#endregion
}