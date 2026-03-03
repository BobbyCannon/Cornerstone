#region References

using System;
using System.Diagnostics;
using System.Threading;

// for OperatingSystem.IsBrowser()

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Keeps track of multi-threaded task steps. Allows actions on different threads/tasks
/// to execute in a desired order by waiting for each step.
/// 
/// On browser/WASM: uses polling + yielding (no blocking allowed).
/// On other platforms: uses efficient ManualResetEventSlim.
/// </summary>
public class TestCoordinator : IDisposable
{
	#region Constants

	private const int BrowserPollingIntervalMs = 1;

	#endregion

	#region Fields

	private readonly int _maxSteps;
	private readonly long[] _stepCompleted;
	private readonly ManualResetEventSlim[] _steps;
	private static readonly TimeSpan DefaultTimeout;

	#endregion

	#region Constructors

	public TestCoordinator(int maxSteps)
	{
		if (maxSteps < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(maxSteps));
		}

		_maxSteps = maxSteps;

		if (!OperatingSystem.IsBrowser())
		{
			_steps = new ManualResetEventSlim[maxSteps + 1];
			for (var i = 0; i < _steps.Length; i++)
			{
				_steps[i] = new ManualResetEventSlim(false);
			}
			_steps[0].Set(); // step 0 is always "done"
		}
		else
		{
			// Browser fallback: track completion with array + Interlocked
			_stepCompleted = new long[maxSteps + 1];

			// step 0 is done immediately
			Interlocked.Exchange(ref _stepCompleted[0], 1L);
		}
	}

	static TestCoordinator()
	{
		DefaultTimeout = TimeSpan.FromSeconds(5);
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (!OperatingSystem.IsBrowser())
		{
			if (_steps != null)
			{
				foreach (var e in _steps)
				{
					e?.Dispose();
				}
			}
		}

		// browser: nothing to dispose
	}

	public void ProcessStep(int step, Action action)
	{
		if ((step < 1) || (step > _maxSteps))
		{
			throw new ArgumentOutOfRangeException(nameof(step));
		}

		if (!OperatingSystem.IsBrowser())
		{
			// Native platforms: efficient blocking wait
			_steps![step - 1].Wait();
			action();
			_steps[step].Set();
		}
		else
		{
			// Browser: poll previous step + yield to JS event loop
			WaitForStepBrowser(step - 1);

			action();

			// Signal next step
			Interlocked.Exchange(ref _stepCompleted![step], 1L);
		}
	}

	public void WaitForStep(int step, TimeSpan? timeout = null)
	{
		timeout ??= DefaultTimeout;

		if ((step < 0) || (step > _maxSteps))
		{
			throw new ArgumentOutOfRangeException(nameof(step));
		}

		if (!OperatingSystem.IsBrowser())
		{
			_steps![step].Wait(timeout.Value);
		}
		else
		{
			WaitForStepBrowser(step, timeout.Value);
		}
	}

	private void WaitForStepBrowser(int stepIndex, TimeSpan? timeout = null)
	{
		timeout ??= DefaultTimeout;
		var stopwatch = Stopwatch.StartNew();

		while (Interlocked.Read(ref _stepCompleted![stepIndex]) == 0)
		{
			// Yield control back to browser event loop (non-blocking)
			// Thread.Sleep(0) is usually mapped to a short yield in .NET WASM
			Thread.Sleep(BrowserPollingIntervalMs);

			if (stopwatch.Elapsed >= timeout.Value)
			{
				throw new TimeoutException(
					$"Step {stepIndex + 1} did not complete within {timeout.Value.TotalSeconds} seconds (browser mode)");
			}
		}
	}

	#endregion
}