#region References

using Cornerstone.Collections;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents an object that can track property changes.
/// </summary>
public interface ITrackPropertyChanges
{
	#region Methods

	/// <summary>
	/// Apply the changes to this object to the provided destination object.
	/// </summary>
	/// <param name="destination"> The object to update. </param>
	public void ApplyChangesTo(object destination);

	/// <summary>
	/// Get the list of changed properties.
	/// </summary>
	/// <returns> The list of changed property in a read only set. </returns>
	public ReadOnlySet<string> GetChangedProperties();

	/// <summary>
	/// Determines if the object has changes.
	/// </summary>
	/// <returns> True if the object has changes otherwise false. </returns>
	public bool HasChanges();

	/// <summary>
	/// Determines if the object has changes.
	/// </summary>
	/// <param name="settings"> An optional set of options. </param>
	/// <returns> True if the object has changes otherwise false. </returns>
	public bool HasChanges(IncludeExcludeSettings settings);

	/// <summary>
	/// Reset the "has changes" state.
	/// </summary>
	public void ResetHasChanges();

	#endregion
}