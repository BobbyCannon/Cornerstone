#region References

using System;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Settings;

public class SyncSetting : SyncModel, ISetting
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