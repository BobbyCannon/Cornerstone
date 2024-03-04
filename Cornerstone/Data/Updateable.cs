#region References

using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a comparer for a type.
/// </summary>
/// <typeparam name="T"> The type to be compared. </typeparam>
public abstract class Updateable<T> : Bindable
	where T : IUpdateable
{
	#region Constructors

	/// <summary>
	/// Creates an instance of a comparer.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected Updateable(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="value"> The value to be tested. </param>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public virtual bool ShouldUpdate(T value, T update, UpdateableOptions options)
	{
		return value.UpdateWith(update, options);
	}

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="value"> The value to be tested. </param>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	public bool ShouldUpdate(object value, object update, UpdateableOptions options)
	{
		return value is T tValue
			&& update is T tUpdate
			&& ShouldUpdate(tValue, tUpdate, options);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be tested. </param>
	/// <param name="update"> The source of the update. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public virtual bool UpdateWith(ref T value, T update, UpdateableOptions options)
	{
		return value.UpdateWith(update, options);
	}

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="value"> The value to be tested. </param>
	/// <param name="update"> The source of the update. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	public bool UpdateWith(ref object value, object update, UpdateableOptions options)
	{
		return value.UpdateWith(update, options);
	}

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
	HashSet<string> GetDefaultIncludedProperties(UpdateableAction action);

	/// <summary>
	/// Determine if the update should be applied.
	/// </summary>
	/// <param name="update"> The update to be tested. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update should be applied otherwise false. </returns>
	bool ShouldUpdate(object update, UpdateableOptions options);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool TryUpdateWith(object update);

	/// <summary>
	/// Try to apply an update to the provided value.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool TryUpdateWith(object update, UpdateableOptions options);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(object update);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="action"> The type of the action this update is for. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(object update, UpdateableAction action);

	/// <summary>
	/// Allows updating of one type to another based on member Name and Type.
	/// </summary>
	/// <param name="update"> The source of the update. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	/// <returns> True if the update was applied otherwise false. </returns>
	bool UpdateWith(object update, UpdateableOptions options);

	#endregion
}