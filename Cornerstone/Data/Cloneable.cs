#region References

using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Data;

/// <inheritdoc cref="ICloneable{T}" />
public class Cloneable<T> : Notifiable, ICloneable<T>
{
	#region Methods

	/// <inheritdoc />
	public T DeepClone(int? maxDepth = null)
	{
		var response = typeof(T).CreateInstance();

		switch (response)
		{
			case IUpdateable updateable:
			{
				updateable.UpdateWith(this, UpdateableAction.Updateable);
				break;
			}
			default:
			{
				response.UpdateWithUsingReflection(this);
				break;
			}
		}

		if (response is ITrackPropertyChanges changeable)
		{
			changeable.ResetHasChanges();
		}

		return (T) response;
	}

	/// <inheritdoc />
	public T ShallowClone()
	{
		return DeepClone(0);
	}

	/// <inheritdoc />
	object ICloneable.DeepClone(int? maxDepth)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	object ICloneable.ShallowClone()
	{
		return ShallowClone();
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
	new T DeepClone(int? maxDepth = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	new T ShallowClone();

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
	public object DeepClone(int? maxDepth = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	public object ShallowClone();

	#endregion
}