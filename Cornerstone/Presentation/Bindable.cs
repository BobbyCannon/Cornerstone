#region References

using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Presentation;

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

	/// <inheritdoc />
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
	protected sealed override void OnPropertyChanged(string propertyName)
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