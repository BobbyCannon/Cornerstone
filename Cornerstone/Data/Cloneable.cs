#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <inheritdoc cref="ICloneable{T}" />
public class Cloneable<T> : Notifiable, ICloneable<T>
{
	#region Methods

	/// <inheritdoc />
	public virtual T DeepClone(int? maxDepth = null)
	{
		return Cloneable.DeepClone<T>(this, maxDepth);
	}

	/// <inheritdoc />
	public T ShallowClone()
	{
		return DeepClone(0);
	}

	/// <inheritdoc />
	object ICloneable.DeepCloneObject(int? maxDepth)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	object ICloneable.ShallowCloneObject()
	{
		return ShallowClone();
	}

	#endregion
}

internal class Cloneable
{
	#region Methods

	public static T2 DeepClone<T2>(object oldValue, int? maxDepth = null)
	{
		var response = DeepClone(typeof(T2), oldValue, maxDepth);
		return (T2) response;
	}

	public static object DeepClone(Type type, object oldValue, int? maxDepth = null)
	{
		var response = type.CreateInstance();

		switch (response)
		{
			case IUpdateable updateable:
			{
				updateable.UpdateWith(oldValue, UpdateableAction.Updateable);
				break;
			}
			default:
			{
				response.UpdateWithUsingReflection(oldValue);
				break;
			}
		}

		if (response is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		return response;
	}

	#endregion
}

/// <summary>
/// Represents a cloneable item.
/// </summary>
public interface ICloneable<out T> : ICloneable
{
	#region Methods

	/// <summary>
	/// Deep clone the item with child relationships. Default level is -1 which means clone full hierarchy of children.
	/// </summary>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <returns> The cloned objects. </returns>
	public T DeepClone(int? maxDepth = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	public T ShallowClone();

	#endregion
}

/// <summary>
/// Represents a cloneable item.
/// </summary>
public interface ICloneable
{
	#region Methods

	/// <summary>
	/// Deep clone the item with child relationships. Default level is -1 which means clone full hierarchy of children.
	/// </summary>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <returns> The cloned objects. </returns>
	public object DeepCloneObject(int? maxDepth = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	public object ShallowCloneObject();

	#endregion
}