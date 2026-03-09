#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a manager.
/// </summary>
public partial class Manager : Notifiable, IManager
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsInitialized { get; protected set; }

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

	public virtual void Update()
	{
	}

	#endregion
}

/// <summary>
/// Represents a manager.
/// </summary>
public interface IManager : IViewModel
{
	#region Methods

	/// <summary>
	/// The method to call on a worker thread to process the manager.
	/// </summary>
	void Update();

	#endregion
}