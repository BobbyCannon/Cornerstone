#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[Packable(1, ["*"])]
public partial class Bookmark
	: SyncModel, IBookmark,
		IUpdateable<Bookmark>,
		IUpdateable<BookmarkEntity>,
		IUpdateable<IBookmark>
{
	#region Properties

	public partial Guid? CustomerSyncId { get; set; }
	public partial bool IsParent { get; set; }
	public partial string Name { get; set; }
	public partial int Order { get; set; }
	public partial Guid? ParentSyncId { get; set; }

	#endregion
}

public interface IBookmark : IHierarchySyncItem
{
	#region Properties

	string Name { get; set; }

	#endregion
}
