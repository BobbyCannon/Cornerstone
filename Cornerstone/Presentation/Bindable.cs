#region References

using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a bindable object for a UI bindings.
/// </summary>
public abstract class Bindable<T> : Bindable, ICloneable<T>, IUpdateable<T>
{
	#region Constructors

	public Bindable() : this(null)
	{
	}

	public Bindable(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual T DeepClone(int? maxDepth = null, IncludeExcludeOptions options = null)
	{
		return (T) this.DeepCloneUsingUpdateWith(typeof(T), maxDepth, options);
	}

	/// <inheritdoc />
	public T ShallowClone(IncludeExcludeOptions options = null)
	{
		return DeepClone(0, options);
	}

	/// <inheritdoc />
	public virtual bool ShouldUpdate(T update, IncludeExcludeOptions options)
	{
		return UpdateableExtensions.ShouldUpdate(this, update, options);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(T update)
	{
		return TryUpdateWith(update, IncludeExcludeOptions.Empty);
	}

	/// <inheritdoc />
	public bool TryUpdateWith(T update, IncludeExcludeOptions options)
	{
		return ShouldUpdate(update, options)
			&& UpdateWith(update, options);
	}

	/// <inheritdoc />
	public bool UpdateWith(T update)
	{
		return UpdateWith(update, IncludeExcludeOptions.Empty);
	}

	/// <inheritdoc />
	public bool UpdateWith(T update, UpdateableAction action)
	{
		var options = Cache.GetOptions(GetRealType(), action);
		return UpdateWith(update, options);
	}

	/// <inheritdoc />
	public abstract bool UpdateWith(T update, IncludeExcludeOptions options);

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeOptions options)
	{
		return update switch
		{
			T value => UpdateWith(value, options),
			_ => base.UpdateWith(update, options)
		};
	}

	#endregion
}

/// <summary>
/// Represents a bindable object for a UI bindings.
/// </summary>
public abstract class Bindable : Notifiable, IBindable, IDispatchable
{
	#region Fields

	private IDispatcher _dispatcher;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the bindable object.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected Bindable(IDispatcher dispatcher = null)
	{
		_dispatcher = dispatcher;
	}

	#endregion

	#region Methods

	/// <inheritdoc cref="IDispatcher" />
	public IDispatcher GetDispatcher()
	{
		return _dispatcher;
	}

	/// <inheritdoc />
	public virtual void UpdateDispatcher(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		// Ensure we have not paused property notifications
		if ((propertyName == null) || !IsPropertyChangeNotificationsEnabled())
		{
			// Property change notifications have been paused or property null so bounce
			return;
		}

		this.Dispatch(() =>
		{
			OnPropertyChangedInDispatcher(propertyName);
			base.OnPropertyChanged(propertyName);
		});
	}

	/// <summary>
	/// Notification of the OnPropertyChanged event in the dispatcher thread.
	/// </summary>
	/// <param name="propertyName"> The name of the property has changed. </param>
	protected virtual void OnPropertyChangedInDispatcher(string propertyName)
	{
	}

	#endregion
}

/// <summary>
/// Represents a bindable object.
/// </summary>
public interface IBindable : INotifiable
{
	#region Methods

	/// <summary>
	/// Get the current dispatcher in use.
	/// </summary>
	/// <returns>
	/// The dispatcher that is currently being used. Null if no dispatcher is assigned.
	/// </returns>
	public IDispatcher GetDispatcher();

	/// <summary>
	/// Updates the entity for this entity.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public void UpdateDispatcher(IDispatcher dispatcher);

	#endregion
}