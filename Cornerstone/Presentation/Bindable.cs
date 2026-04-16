#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a bindable object for a UI bindings.
/// </summary>
public abstract class Bindable<T> : Bindable, IUpdateable<T>
{
	#region Constructors

	protected Bindable() : this(null)
	{
	}

	protected Bindable(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	public abstract bool UpdateWith(T update, IncludeExcludeSettings settings);

	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			T value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}

/// <summary>
/// Represents a bindable object for a UI bindings.
/// </summary>
[SourceReflection]
public abstract class Bindable : Notifiable, IBindable
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

	public virtual void UpdateDispatcher(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	#endregion
}

/// <summary>
/// Represents a bindable object.
/// </summary>
public interface IBindable : INotifiable, IDispatchable
{
	#region Methods

	/// <summary>
	/// Updates the entity for this entity.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public void UpdateDispatcher(IDispatcher dispatcher);

	#endregion
}