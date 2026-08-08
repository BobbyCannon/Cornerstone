#region References

using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Generic long-running operation status for domain state.
/// UI projects this (busy / progress / error); processors own writes.
/// Optional <see cref="WaitUntilIdleAsync"/> supports hosts that stay open until work finishes.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class OperationState : CornerstoneObject
{
	#region Fields

	private TaskCompletionSource _whenIdle;

	#endregion

	#region Properties

	public partial bool CanCancel { get; set; }

	public partial string ErrorText { get; set; }

	public partial bool IsBusy { get; set; }

	/// <summary>
	/// Optional coarse phase label for UI (e.g. "Fetch", "Refresh"). Domain types may also track a typed kind.
	/// </summary>
	public partial string Phase { get; set; }

	public partial string ProgressText { get; set; }

	#endregion

	#region Methods

	public virtual void Clear()
	{
		IsBusy = false;
		ProgressText = string.Empty;
		ErrorText = string.Empty;
		CanCancel = false;
		Phase = string.Empty;
		CompleteIdleWaiters();
	}

	public virtual void Fail(string error)
	{
		IsBusy = false;
		ErrorText = error ?? string.Empty;
		ProgressText = string.Empty;
		CanCancel = false;
		CompleteIdleWaiters();
	}

	/// <summary>
	/// Mark the operation busy. Completes any previous idle waiters first.
	/// </summary>
	/// <param name="progressText"> User-facing progress. </param>
	/// <param name="phase"> Optional phase label (e.g. enum name). </param>
	public virtual void Start(string progressText = null, string phase = null)
	{
		// If a previous op left waiters (should not happen), release them.
		if (IsBusy)
		{
			CompleteIdleWaiters();
		}

		_whenIdle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		IsBusy = true;
		ProgressText = progressText ?? string.Empty;
		ErrorText = string.Empty;
		CanCancel = false;
		Phase = phase ?? string.Empty;
	}

	public virtual void Succeed()
	{
		IsBusy = false;
		ProgressText = string.Empty;
		ErrorText = string.Empty;
		CanCancel = false;
		Phase = string.Empty;
		CompleteIdleWaiters();
	}

	/// <summary>
	/// Completes when the operation is not busy (including already idle).
	/// After <see cref="Start"/>, completes when <see cref="Succeed"/>, <see cref="Fail"/>, or <see cref="Clear"/> runs.
	/// </summary>
	public Task WaitUntilIdleAsync(CancellationToken cancellationToken = default)
	{
		if (!IsBusy)
		{
			return Task.CompletedTask;
		}

		var whenIdle = _whenIdle;
		if (whenIdle == null)
		{
			return Task.CompletedTask;
		}

		return cancellationToken.CanBeCanceled
			? whenIdle.Task.WaitAsync(cancellationToken)
			: whenIdle.Task;
	}

	private void CompleteIdleWaiters()
	{
		var whenIdle = Interlocked.Exchange(ref _whenIdle, null);
		whenIdle?.TrySetResult();
	}

	#endregion
}