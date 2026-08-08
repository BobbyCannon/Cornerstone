#region References

using System;
using Cornerstone.Data;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a sync setting.
/// </summary>
[Packable(1, [
	nameof(CanSync), nameof(Category), nameof(CreatedOn), nameof(ExpiresOn), nameof(IsDeleted), nameof(ModifiedOn),
	nameof(Name), nameof(SyncId), nameof(Value), nameof(ValueType)
])]
[Updateable(UpdateableAction.All, ["*"])]
public abstract partial class SettingSyncModel : SyncModel, ISetting
{
	#region Properties
	
	public bool CanSync { get; set; }
	public string Category { get; set; }
	public DateTime ExpiresOn { get; set; }
	public string Name { get; set; }
	public string Value { get; set; }
	public string ValueType { get; set; }

	#endregion
}