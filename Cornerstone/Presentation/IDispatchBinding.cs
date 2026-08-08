namespace Cornerstone.Presentation;

/// <summary>
/// ViewModel-owned binding that projects a model-side source onto a UI destination
/// during <see cref="DispatchableViewModel.ApplyModelChanges" />.
/// </summary>
public interface IDispatchBinding
{
	#region Methods

	/// <summary>
	/// Applies the projection when pending. Implementations should clear the source
	/// pending signal after a successful apply where appropriate.
	/// </summary>
	void ApplyPendingChanges();

	/// <summary>
	/// True when this binding has work for the current tick.
	/// </summary>
	bool HasPendingChanges();

	#endregion
}