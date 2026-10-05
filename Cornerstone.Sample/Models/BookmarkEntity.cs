#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[SqlTable(TableName = "Bookmarks")]
[Updateable(UpdateableAction.All, [
	nameof(CustomerSyncId), nameof(IsParent), nameof(Name), nameof(Order), nameof(ParentSyncId)
])]
[Updateable(UpdateableAction.EverythingExceptSync, [nameof(CustomerId), nameof(ParentId)])]
public partial class BookmarkEntity
	: SyncEntity<long>, IBookmark,
		IUpdateable<BookmarkEntity>,
		IUpdateable<Bookmark>,
		IUpdateable<IBookmark>
{
	#region Properties

	[SqlTableColumn]
	public partial int? CustomerId { get; set; }

	[SqlTableColumn]
	public partial Guid? CustomerSyncId { get; set; }

	[SqlTableColumn]
	public partial bool IsParent { get; set; }

	[SqlTableColumn(IsNullable = false)]
	public partial string Name { get; set; }

	[SqlTableColumn]
	public partial int Order { get; set; }

	[SqlTableColumn]
	[SqlForeignKey("Bookmarks")]
	public partial long? ParentId { get; set; }

	[SqlTableColumn]
	public partial Guid? ParentSyncId { get; set; }

	#endregion
}
