namespace Cornerstone.Presentation;

/// <summary>
/// Represents a dispatchable object.
/// </summary>
public interface IDispatchable
{
	#region Methods

	/// <summary>
	/// Gets the dispatcher for the dispatchable.
	/// </summary>
	/// <returns> The dispatcher. </returns>
	IDispatcher GetDispatcher();

	#endregion
}