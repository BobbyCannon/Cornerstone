#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the settings for a sync client
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncClientSettings : CornerstoneObject<SyncClientSettings>
{
	#region Properties

	/// <summary>
	/// Determines if the sync client should cache primary keys for relationships.
	/// </summary>
	public partial bool EnablePrimaryKeyCache { get; set; }

	/// <summary>
	/// Indicates this client is the server and should maintain dates, meaning as you save data the CreatedOn, ModifiedOn will
	/// be updated to the current server time. This should only be set for the "Server" sync client that represents the primary database.
	/// </summary>
	public partial bool IsServerClient { get; set; }

	#endregion
}