#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.FileSystem;

public partial class DirectoryOrFileInfo
	: SpeedyTree<DirectoryOrFileInfo>
{
	#region Fields

	private readonly IDispatcher _dispatcher;

	#endregion

	#region Constructors

	protected DirectoryOrFileInfo(DirectoryOrFileInfo parent, IDispatcher dispatcher)
		: base(parent,
			new OrderBy<DirectoryOrFileInfo>(x => x.IsParent, true),
			new OrderBy<DirectoryOrFileInfo>(x => x.Name)
		)
	{
		_dispatcher = dispatcher;
	}

	#endregion

	#region Properties

	[Notify]
	[AlsoNotify(nameof(Name))]
	public partial DirectoryInfo DirectoryInfo { get; protected set; }

	[Notify]
	[AlsoNotify(nameof(IsParent), nameof(Name))]
	public partial FileInfo FileInfo { get; protected set; }

	public bool IsParent
	{
		get => FileInfo == null;
		set { }
	}

	[Notify]
	public partial bool IsRefreshing { get; set; }

	public string Name => FileInfo?.Name ?? DirectoryInfo.Name;

	public int Order { get; set; }

	[Notify]
	public partial Guid? ParentSyncId { get; set; }

	#endregion

	#region Methods

	public static DirectoryOrFileInfo Create(DirectoryInfo info, DirectoryOrFileInfo parent, IDispatcher dispatcher)
	{
		var response = new DirectoryOrFileInfo(parent, dispatcher) { DirectoryInfo = info };
		return response;
	}

	public static DirectoryOrFileInfo Create(FileInfo info, DirectoryOrFileInfo parent, IDispatcher dispatcher)
	{
		var response = new DirectoryOrFileInfo(parent, dispatcher)
		{
			DirectoryInfo = info.Directory,
			FileInfo = info
		};
		return response;
	}

	public void Refresh()
	{
		Task.Run(() =>
		{
			try
			{
				_dispatcher.Dispatch(() => IsRefreshing = true);

				if (!IsParent)
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
					.Select(x => Create(x, this, _dispatcher))
					.ToArray();

				var files = DirectoryInfo
					.GetFiles()
					.Select(x => Create(x, this, _dispatcher))
					.ToArray();

				results.AddRange(infos);
				results.AddRange(files);

				Children.Load(results);
			}
			finally
			{
				IsRefreshing = false;
			}
		});
	}

	#endregion
}