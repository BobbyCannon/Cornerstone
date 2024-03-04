#region References

using System;
using System.Diagnostics;
using System.Threading;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Keeps track of a multi-threaded task to order actions. Allows test actions to run on different
/// threads (task) yet execute in a desired order by waiting for each action step.
/// </summary>
public class TestCoordinator
{
	#region Fields

	private long _completedStep;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the test coordinator.
	/// </summary>
	public TestCoordinator()
	{
		_completedStep = 0;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Process a step of the test.
	/// </summary>
	/// <param name="expectedStep"> The expected step of the test. </param>
	/// <param name="stepAction"> The action for the step. </param>
	public void ProcessStep(long expectedStep, Action stepAction)
	{
		var w = new SpinWait();
		var previousStep = expectedStep - 1;

		// Wait for our previous step to complete
		while (previousStep != Interlocked.Read(ref _completedStep))
		{
			w.SpinOnce();
		}

		stepAction.Invoke();

		Interlocked.Increment(ref _completedStep);
	}

	/// <summary>
	/// Wait for a step to complete.
	/// </summary>
	/// <param name="step"> The step to wait for. </param>
	/// <param name="timeout"> An optional timeout. </param>
	public void WaitForStepToComplete(long step, TimeSpan? timeout = null)
	{
		var w = new SpinWait();
		var watch = Stopwatch.StartNew();
		timeout ??= TimeSpan.FromSeconds(1);

		// Wait for our previous step to complete
		while (step > Interlocked.Read(ref _completedStep))
		{
			w.SpinOnce();

			if (watch.Elapsed >= timeout)
			{
				throw new TimeoutException("Step never completed");
			}
		}
	}

	#endregion
}