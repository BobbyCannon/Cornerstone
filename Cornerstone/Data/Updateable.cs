#region References

using System.Collections.Generic;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents an updateable item
/// </summary>
public interface IUpdateable<in T> : IUpdateable
{
	#region Methods

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	bool ShouldUpdate(T update, IncludeExcludeSettings settings);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool TryUpdateWith(T update);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool TryUpdateWith(T update, IncludeExcludeSettings settings);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(T update);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="action"> The type of the action this update is for. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(T update, UpdateableAction action);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(T update, IncludeExcludeSettings settings);

	#endregion
}

/// <summary>
/// Represents an updateable item
/// </summary>
public interface IUpdateable
{
	#region Methods

	/// <summary>
	/// Gets the default included properties. Defaults to an empty collection.
	/// </summary>
	/// <param name="action"> The properties to include for the action. </param>
	/// <returns> The properties to be included for the action. </returns>
	public HashSet<string> GetDefaultIncludedProperties(UpdateableAction action);

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public bool ShouldUpdate(object update, IncludeExcludeSettings settings);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool TryUpdateWith(object update);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool TryUpdateWith(object update, IncludeExcludeSettings settings);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(object update);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="action"> The type of the action this update is for. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(object update, UpdateableAction action);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(object update, IncludeExcludeSettings settings);

	#endregion
}