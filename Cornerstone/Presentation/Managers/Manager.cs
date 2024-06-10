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

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Initialize()
	{
		IsInitialized = true;
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

	#endregion
}

/// <summary>
/// Represents a manager of a data set.
/// </summary>
public interface IManager : IViewModel
{
	#region Methods

	/// <summary>
	/// The method to call on a worker thread to process the manager.
	/// </summary>
	void Process();

	#endregion
}