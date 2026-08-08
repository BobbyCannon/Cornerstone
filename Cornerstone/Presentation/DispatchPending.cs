#region References

using System.Threading;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Explicit dispatch-only dirty flag for custom projections
/// (charts, derived fields, multistep apply work).
/// </summary>
public sealed class DispatchPending : IDispatchPending
{
	#region Fields

	private int _pending;

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool HasPending => Volatile.Read(ref _pending) != 0;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void ClearHasPending()
	{
		Volatile.Write(ref _pending, 0);
	}

	/// <summary>
	/// Marks this source as needing a dispatch apply.
	/// </summary>
	public void MarkPending()
	{
		Volatile.Write(ref _pending, 1);
	}

	#endregion
}

/// <summary>
/// Coarse "view needs an update" signal for the AppDispatcher / DispatchableViewModel loop.
/// Distinct from <see cref="Data.ITrackPropertyChanges" /> (property-bit graphs, ApplyChangesTo).
/// </summary>
public interface IDispatchPending
{
	#region Properties

	/// <summary>
	/// True when there is work a dispatcher binding should apply.
	/// </summary>
	bool HasPending { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Clears the pending signal after a successful projection/apply.
	/// For flag-only sources this is the whole state. For buffered sources
	/// (e.g. <see cref="Text.TextIngress" />), this must not discard unapplied payload —
	/// consume first (Drain), then clear is a no-op or flag-only.
	/// </summary>
	void ClearHasPending();

	#endregion
}