#region References

using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the settings for a sync client
/// </summary>
public class SyncClientSettings : Bindable<SyncClientSettings>
{
	#region Properties

	/// <summary>
	/// Determines if the sync client should cache primary keys for relationships.
	/// </summary>
	public bool EnablePrimaryKeyCache { get; set; }

	/// <summary>
	/// Indicates this client is the server and should maintain dates, meaning as you save data the CreatedOn, ModifiedOn will
	/// be updated to the current server time. This should only be set for the "Server" sync client that represents the primary database.
	/// </summary>
	public bool IsServerClient { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the SyncClientOptions with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(SyncClientSettings update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(EnablePrimaryKeyCache, update.EnablePrimaryKeyCache, settings.ShouldProcessProperty(nameof(EnablePrimaryKeyCache)), x => EnablePrimaryKeyCache = x);
		UpdateProperty(IsServerClient, update.IsServerClient, settings.ShouldProcessProperty(nameof(IsServerClient)), x => IsServerClient = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			SyncClientSettings value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}