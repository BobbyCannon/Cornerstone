#region References

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for Task
/// </summary>
public static class TaskExtensions
{
	#region Methods

	/// <summary>
	/// Synchronously await the results of an asynchronous operation without deadlocking.
	/// </summary>
	/// <param name="task"> The <see cref="Task" /> representing the pending operation. </param>
	public static void AwaitResults(this Task task)
	{
		SynchronousAwaiter.GetResult(task);
	}

	/// <summary>
	/// Synchronously await the results of an asynchronous operation without deadlocking.
	/// </summary>
	/// <typeparam name="T"> The result type of the operation. </typeparam>
	/// <param name="task"> The <see cref="Task" /> representing the pending operation. </param>
	/// <returns> The result of the operation. </returns>
	public static T AwaitResults<T>(this Task<T> task)
	{
		return SynchronousAwaiter.GetResult(task);
	}

	/// <summary>
	/// Synchronously await the results of an asynchronous operation without deadlocking.
	/// </summary>
	/// <typeparam name="T"> The result type of the operation. </typeparam>
	/// <param name="task"> The <see cref="Task" /> representing the pending operation. </param>
	/// <param name="timeout"> The timeout if the task does not complete. </param>
	/// <returns> The result of the operation. </returns>
	public static T AwaitResults<T>(this Task<T> task, TimeSpan timeout)
	{
		return SynchronousAwaiter.GetResult(task, timeout);
	}

	/// <summary>
	/// Determine if a task has started and is completed.
	/// </summary>
	/// <param name="task"> The task to check. </param>
	/// <returns> True if the task is Cancelled, Faulted, or RanToCompletion otherwise false. </returns>
	public static bool IsCompleted(this Task task)
	{
		return (task.Status == TaskStatus.Canceled)
			|| (task.Status == TaskStatus.Faulted)
			|| (task.Status == TaskStatus.RanToCompletion);
	}

	/// <summary>
	/// Timeout after some amount time.
	/// </summary>
	/// <typeparam name="TResult"> The type for the result. </typeparam>
	/// <param name="task"> The task to wait for. </param>
	/// <param name="timeout"> The maximum about of time to wait for. </param>
	/// <returns> The task with the result after waiting. </returns>
	/// <exception cref="TimeoutException"> </exception>
	public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
	{
		using var timeoutCancellationTokenSource = new CancellationTokenSource();
		using var delay = Task.Delay(timeout, timeoutCancellationTokenSource.Token);

		var completedTask = await Task.WhenAny(task, delay).ConfigureAwait(false);
		if (completedTask != task)
		{
			throw new TimeoutException("The operation has timed out.");
		}

		timeoutCancellationTokenSource.Cancel();

		// Very important in order to propagate exceptions
		return await task;
	}

	#endregion

	#region Classes

	private class SynchronousAwaiter
	{
		#region Methods

		public static T GetResult<T>(Task<T> task)
		{
			var tcs = new TaskCompletionSource<bool>();
			task.ContinueWith(t =>
				{
					if (t is { IsFaulted: true, Exception: not null })
					{
						tcs.SetException(t.Exception.InnerExceptions);
					}
					else if (t.IsCanceled)
					{
						tcs.SetCanceled();
					}
					else
					{
						tcs.SetResult(true);
					}
				},
				TaskContinuationOptions.ExecuteSynchronously
			);

			tcs.Task.Wait();
			return task.Result;
		}

		public static T GetResult<T>(Task<T> task, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>();
			task.ContinueWith(t =>
				{
					if (t is { IsFaulted: true, Exception: not null })
					{
						tcs.SetException(t.Exception.InnerExceptions);
					}
					else if (t.IsCanceled)
					{
						tcs.SetCanceled();
					}
					else
					{
						tcs.SetResult(true);
					}
				},
				TaskContinuationOptions.ExecuteSynchronously
			);

			if (!tcs.Task.Wait(timeout))
			{
				throw new TimeoutException("The operation has timed out.");
			}

			return task.Result;
		}

		public static void GetResult(Task task)
		{
			var tcs = new TaskCompletionSource<bool>();
			task.ContinueWith(t =>
				{
					if (t is { IsFaulted: true, Exception: not null })
					{
						tcs.SetException(t.Exception.InnerExceptions);
					}
					else if (t.IsCanceled)
					{
						tcs.SetCanceled();
					}
					else
					{
						tcs.SetResult(true);
					}
				},
				TaskContinuationOptions.ExecuteSynchronously
			);

			tcs.Task.Wait();
		}

		#endregion
	}

	#endregion
}