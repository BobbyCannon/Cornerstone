#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a view model.
/// </summary>
[Updateable(false)]
public abstract class ViewModel<T> : ViewModel, IViewModel<T>
{
	#region Properties

	public T Id { get; set; }

	#endregion
}

/// <summary>
/// Represents a view model.
/// </summary>
[SourceReflection]
[Updateable(false)]
public abstract class ViewModel : Notifiable, IViewModel
{
	#region Properties

	public bool IsInitialized { get; private set; }

	#endregion

	#region Methods

	public virtual void Initialize()
	{
		IsInitialized = true;
	}

	public virtual void Uninitialize()
	{
		IsInitialized = false;
	}

	#endregion
}

public interface IViewModel<T> : IViewModel
{
	#region Properties

	/// <summary>
	/// Gets or sets the ID of the view.
	/// </summary>
	public T Id { get; set; }

	#endregion
}

/// <summary>
/// Represents an object that subscribes to events.
/// </summary>
public interface IViewModel : INotifiable
{
	#region Properties

	/// <summary>
	/// The manager is initialized.
	/// </summary>
	bool IsInitialized { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Initialize the viewmodel.
	/// </summary>
	public void Initialize();

	/// <summary>
	/// Reset the view model.
	/// </summary>
	/// <remarks>
	/// Will require the view model to be re-initialized.
	/// </remarks>
	void Uninitialize();

	#endregion
}