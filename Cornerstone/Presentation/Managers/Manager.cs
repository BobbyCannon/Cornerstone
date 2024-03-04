#region References

using Cornerstone.Attributes;

#endregion

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Represents a manager of a data set.
/// </summary>
public class Manager : Bindable, IManager
{
	#region Constructors

	/// <summary>
	/// Create an instance a manager of a data set.
	/// </summary>
	public Manager() : this(null)
	{
	}

	/// <summary>
	/// Create an instance a manager of a data set.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Manager(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	[SerializationIgnore]
	public bool IsInitialized { get; private set; }

	/// <inheritdoc />
	[SerializationIgnore]
	public bool IsLoaded { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Initialize()
	{
		IsInitialized = true;
	}

	/// <inheritdoc />
	public virtual void Load(params object[] values)
	{
		IsLoaded = true;
	}

	/// <inheritdoc />
	public virtual void Process()
	{
	}

	/// <inheritdoc />
	public virtual void Uninitialize()
	{
		IsInitialized = false;
	}

	/// <inheritdoc />
	public virtual void Unload()
	{
		IsLoaded = false;
	}

	#endregion
}

/// <summary>
/// Represents a manager of a data set.
/// </summary>
public interface IManager : IViewModel
{
	#region Properties

	/// <summary>
	/// The manager is loaded.
	/// </summary>
	bool IsLoaded { get; }

	#endregion

	#region Methods

	/// <summary>
	/// The method to load the manager.
	/// </summary>
	void Load(params object[] values);

	/// <summary>
	/// The method to call on a worker thread to process the manager.
	/// </summary>
	void Process();

	/// <summary>
	/// The method to unload the manager.
	/// </summary>
	void Unload();

	#endregion
}