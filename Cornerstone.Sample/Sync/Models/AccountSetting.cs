#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Sample.Sync.Models;

[Packable(1, [nameof(CanSync), nameof(Category), nameof(CreatedOn), nameof(ExpiresOn), nameof(IsDeleted), nameof(ModifiedOn),
	nameof(Name), nameof(SyncId), nameof(SyncDeviceSyncId), nameof(Value), nameof(ValueType)])]
[SourceReflection]
public partial class AccountSetting
	: SettingSyncModel, IAccountSetting,
		IUpdateable<AccountSetting>,
		IUpdateable<IAccountSetting>
{
	#region Properties

	[UpdateableAction(UpdateableAction.All)]
	public Guid? SyncDeviceSyncId { get; set; }

	#endregion
}

public interface IAccountSetting : ISetting
{
	#region Properties

	public Guid? SyncDeviceSyncId { get; set; }

	#endregion
}