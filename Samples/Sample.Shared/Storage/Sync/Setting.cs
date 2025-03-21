#region References

using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Sync;

/// <summary>
/// Represents the public setting model.
/// </summary>
public class Setting : SyncModel, ISetting
{
	#region Properties

	public string Name { get; set; }

	public string Value { get; set; }

	#endregion
}

public interface ISetting
{
	string Name { get; set; }

	string Value { get; set; }
}