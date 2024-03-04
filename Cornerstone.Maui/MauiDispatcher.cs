#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Microsoft.Maui.Dispatching;
using Dispatcher = Cornerstone.Presentation.Dispatcher;
using IMauiDispatcher = Microsoft.Maui.Dispatching.IDispatcher;

#endregion

namespace Cornerstone.Maui;

/// <inheritdoc />
public class MauiDispatcher : Dispatcher
{
	#region Fields

	private readonly IMauiDispatcher _dispatcher;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public MauiDispatcher(IMauiDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public override bool IsDispatcherThread => !_dispatcher.IsDispatchRequired;

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void ExecuteOnDispatcher(Action action, DispatcherPriority priority)
	{
		_dispatcher.Dispatch(action);
	}

	/// <inheritdoc />
	protected override T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority)
	{
		return _dispatcher.DispatchAsync(action).AwaitResults();
	}

	/// <inheritdoc />
	protected override Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority)
	{
		return _dispatcher.DispatchAsync(action);
	}

	/// <inheritdoc />
	protected override Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority)
	{
		return _dispatcher.DispatchAsync(action);
	}

	#endregion
}