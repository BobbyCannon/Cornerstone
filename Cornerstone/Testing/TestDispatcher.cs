#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Represents a test dispatcher
/// </summary>
public class TestDispatcher : Dispatcher
{
	#region Constructors

	public TestDispatcher()
	{
		IsDispatcherThread = true;
	}

	#endregion

	#region Properties

	public override bool IsDispatcherThread { get; }

	#endregion

	#region Methods

	protected override void ExecuteOnDispatcher(Action action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		action();
	}

	protected override T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return action();
	}

	protected override Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return Task.Run(action);
	}

	protected override Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return Task.Run(action);
	}

	#endregion
}