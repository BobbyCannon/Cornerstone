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
	/// <param name="exclusions"> An optional set of exclusions. </param>
	/// <returns> True if the object has changes otherwise false. </returns>
	public bool HasChanges(params string[] exclusions);

	/// <summary>
	/// Reset the "has changes" state.
	/// </summary>
	public void ResetHasChanges();

	#endregion
}