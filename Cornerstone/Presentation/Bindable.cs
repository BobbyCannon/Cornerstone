﻿#region References

using System.Runtime.CompilerServices;
using Cornerstone.Data;
using Cornerstone.Extensions;

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
	public virtual T DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null)
	{
		return (T) this.DeepCloneUsingUpdateWith(typeof(T), maxDepth, settings);
	}

	/// <inheritdoc />
	public T ShallowClone(IncludeExcludeSettings settings = null)
	{
		return DeepClone(0, settings);
	}

	/// <inheritdoc />
	public abstract bool UpdateWith(T update, IncludeExcludeSettings settings);

	/// <inheritdoc />
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