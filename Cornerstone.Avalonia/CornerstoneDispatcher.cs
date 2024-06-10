#region References

using System;
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

	/// <inheritdoc />
	public override bool IsDispatcherThread => _dispatcher.CheckAccess();

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void ExecuteOnDispatcher(Action action, DispatcherPriority priority)
	{
		_dispatcher.Invoke(action, ToPriority(priority));
	}

	/// <inheritdoc />
	protected override T ExecuteOnDispatcher<T>(Func<T> action, DispatcherPriority priority)
	{
		return _dispatcher.Invoke(action, ToPriority(priority));
	}

	/// <inheritdoc />
	protected override Task ExecuteOnDispatcherAsync(Action action, DispatcherPriority priority)
	{
		return _dispatcher.InvokeAsync(action, ToPriority(priority)).GetTask();
	}

	/// <inheritdoc />
	protected override Task<T> ExecuteOnDispatcherAsync<T>(Func<T> action, DispatcherPriority priority)
	{
		return _dispatcher.InvokeAsync(action, ToPriority(priority)).GetTask();
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