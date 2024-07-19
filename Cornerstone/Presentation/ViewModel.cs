#region References

using System.Diagnostics;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a view model.
/// </summary>
public abstract class ViewModel<T> : ViewModel, IViewModel<T>
{
	#region Constructors

	/// <inheritdoc />
	protected ViewModel() : this(null)
	{
	}

	/// <inheritdoc />
	protected ViewModel(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public T Id { get; set; }

	#endregion
}

/// <summary>
/// Represents a view model.
/// </summary>
public abstract class ViewModel : Bindable, IViewModel
{
	#region Constructors

	/// <summary>
	/// Initializes a view model.
	/// </summary>
	protected ViewModel() : this(null)
	{
	}

	/// <summary>
	/// Initializes a view model.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected ViewModel(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool IsInitialized { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Initialize()
	{
		if (IsInitialized)
		{
			Debugger.Break();
			return;
		}

		IsInitialized = true;
	}

	/// <inheritdoc />
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
public interface IViewModel
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
	void Initialize();

	/// <summary>
	/// Reset the view model.
	/// </summary>
	/// <remarks>
	/// Will require the view model to be re-initialized.
	/// </remarks>
	void Uninitialize();

	#endregion
}