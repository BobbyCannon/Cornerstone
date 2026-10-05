#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[SqlTable(TableName = "Settings")]
[Updateable(UpdateableAction.All, [nameof(Name), nameof(Value)])]
public partial class SettingEntity
	: SyncEntity<long>, ISetting,
		IUpdateable<SettingEntity>,
		IUpdateable<Setting>,
		IUpdateable<ISetting>
{
	#region Properties

	[SqlTableColumn(IsNullable = false)]
	public partial string Name { get; set; }

	[SqlTableColumn]
	public partial string Value { get; set; }

	#endregion
}