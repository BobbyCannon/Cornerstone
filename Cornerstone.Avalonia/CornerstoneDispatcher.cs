#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using AvaloniaDispatcher = Avalonia.Threading.Dispatcher;
using AvaloniaDispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneDispatcher : Dispatcher
{
	#region Fields

	private readonly AvaloniaDispatcher _dispatcher;
	private static readonly CornerstoneDispatcher _instance;

	#endregion

	#region Constructors

	public CornerstoneDispatcher()
	{
		_dispatcher = AvaloniaDispatcher.UIThread;
	}

	static CornerstoneDispatcher()
	{
		_instance = new CornerstoneDispatcher();
	}

	#endregion

	#region Properties

	public static IDispatcher Instance => _instance;

	public override bool IsDispatcherThread => _dispatcher.CheckAccess();

	#endregion

	#region Methods

	protected override void ExecuteOnDispatcher(Action action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		_dispatcher.Invoke(action, ToPriority(priority), cancellationToken);
	}

	protected override T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return _dispatcher.Invoke(action, ToPriority(priority), cancellationToken);
	}

	protected override Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return _dispatcher.InvokeAsync(action, ToPriority(priority), cancellationToken).GetTask();
	}

	protected override Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority, CancellationToken cancellationToken)
	{
		return _dispatcher.InvokeAsync(action, ToPriority(priority), cancellationToken).GetTask();
	}

	private AvaloniaDispatcherPriority ToPriority(DispatcherPriority priority)
	{
		return priority switch
		{
			DispatcherPriority.ApplicationIdle => AvaloniaDispatcherPriority.ApplicationIdle,
			DispatcherPriority.Background => AvaloniaDispatcherPriority.Background,
			DispatcherPriority.ContextIdle => AvaloniaDispatcherPriority.ContextIdle,
			DispatcherPriority.Normal => AvaloniaDispatcherPriority.Normal,
			DispatcherPriority.SystemIdle => AvaloniaDispatcherPriority.SystemIdle,
			_ => AvaloniaDispatcherPriority.Normal
		};
	}

	#endregion
}