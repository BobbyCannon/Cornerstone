#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[Packable(1, [nameof(CreatedOn), nameof(IsDeleted), nameof(ModifiedOn), nameof(Name), nameof(SyncId), nameof(Value)])]
public partial class Setting
	: SyncModel, ISetting,
		IUpdateable<Setting>,
		IUpdateable<SettingEntity>,
		IUpdateable<ISetting>
{
	#region Properties

	public partial string Name { get; set; }

	public partial string Value { get; set; }

	#endregion
}

public interface ISetting
{
	#region Properties

	string Name { get; set; }

	string Value { get; set; }

	#endregion
}