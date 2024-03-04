#region References

using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a bindable object.
/// </summary>
public abstract class CloneableBindable<T, T2> : CloneableBindable<T>, ICloneable<T2>
	where T : T2, new()
{
	#region Constructors

	/// <summary>
	/// Initializes a bindable object.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected CloneableBindable(IDispatcher dispatcher = null) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	T2 ICloneable<T2>.DeepClone(int? maxDepth)
	{
		return DeepClone(maxDepth);
	}

	/// <inheritdoc />
	T2 ICloneable<T2>.ShallowClone()
	{
		return DeepClone(0);
	}

	#endregion
}

/// <summary>
/// Represents a bindable object.
/// </summary>
public abstract class CloneableBindable<T> : Bindable, ICloneable<T>
{
	#region Constructors

	/// <summary>
	/// Initializes a bindable object.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected CloneableBindable(IDispatcher dispatcher = null) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual T DeepClone(int? maxDepth = null)
	{
		var response = typeof(T).CreateInstance();

		switch (response)
		{
			case Entity entity:
			{
				entity.UpdateWith(this);
				break;
			}
			case IUpdateable updatable:
			{
				updatable.UpdateWith(this);
				break;
			}
			default:
			{
				response.UpdateWithUsingReflection(this);
				break;
			}
		}

		if (response is ITrackPropertyChanges changes)
		{
			changes.ResetHasChanges();
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