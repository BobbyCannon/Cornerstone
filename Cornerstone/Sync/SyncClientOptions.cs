#region References

using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the options for a sync client
/// </summary>
public class SyncClientOptions : Notifiable<SyncClientOptions>
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
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(SyncClientOptions update, UpdateableOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			EnablePrimaryKeyCache = update.EnablePrimaryKeyCache;
			IsServerClient = update.IsServerClient;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(EnablePrimaryKeyCache)), x => x.EnablePrimaryKeyCache = update.EnablePrimaryKeyCache);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(IsServerClient)), x => x.IsServerClient = update.IsServerClient);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, UpdateableOptions options)
	{
		return update switch
		{
			SyncClientOptions value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}