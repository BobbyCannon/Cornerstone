#region References

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabDevelopment : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Development";

	#endregion

	#region Constructors

	public TabDevelopment()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

private void Button_OnClick(object sender, RoutedEventArgs e)
{
	StartButton.IsEnabled = false;

	Task.Run(() =>
	{
		using var server = new RateLimitServer<string>(
			TimeSpan.FromMilliseconds(500),
			TimeSpan.FromMilliseconds(1000),
			message => WriteLine($"Processed: {message} at {DateTime.Now:HH:mm:ss.fff}"),
			true
		);

		// Test Debounce
		WriteLine("Testing Debounce:");
		server.Debounce("Debounce 1");
		Thread.Sleep(100);
		server.Debounce("Debounce 2");
		Thread.Sleep(100);
		server.Debounce("Debounce 3");
		Thread.Sleep(2000); // Increased wait time to see all debounces

		// Test Throttle
		WriteLine("\nTesting Throttle:");
		server.Throttle("Throttle 1");
		server.Throttle("Throttle 2");
		server.Throttle("Throttle 3");
		Thread.Sleep(3500);

		this.DispatchAsync(() => StartButton.IsEnabled = true);
	});
}

	private void WriteLine(string s)
	{
		this.DispatchAsync(() => Log.AppendText(s + Environment.NewLine));
	}

	#endregion
}

public class RateLimitServer<T> : IDisposable
{
	#region Fields

	private readonly Action<T> _callback;
	private CancellationTokenSource _debounceCts;
	private readonly TimeSpan _debounceDelay;
	private bool _isDebouncing;
	private bool _isThrottling;
	private DateTime _lastInvocation;
	private readonly object _lock = new();
	private readonly TimeSpan _throttleDelay;
	private readonly ConcurrentQueue<T> _triggerQueue;
	private readonly bool _useQueue;

	#endregion

	#region Constructors

	public RateLimitServer(
		TimeSpan debounceDelay,
		TimeSpan throttleDelay,
		Action<T> callback,
		bool useQueue = false)
	{
		_debounceDelay = debounceDelay;
		_throttleDelay = throttleDelay;
		_callback = callback ?? throw new ArgumentNullException(nameof(callback));
		_useQueue = useQueue;
		_triggerQueue = new ConcurrentQueue<T>();
		_debounceCts = new CancellationTokenSource();
		_lastInvocation = DateTime.MinValue;
		_isThrottling = false;
		_isDebouncing = false;
	}

	#endregion

	#region Methods

	public void Debounce(T trigger)
	{
		lock (_lock)
		{
			_triggerQueue.Enqueue(trigger);

			if (!_isDebouncing)
			{
				_isDebouncing = true;
				_debounceCts.Cancel();
				_debounceCts = new CancellationTokenSource();
				Task.Run(async () => await ProcessDebounceAsync(_debounceCts.Token));
			}
		}
	}

	public void Dispose()
	{
		_debounceCts.Cancel();
		_debounceCts.Dispose();
	}

	public void Throttle(T trigger)
	{
		lock (_lock)
		{
			var now = DateTime.UtcNow;
			if (!_useQueue)
			{
				if ((now - _lastInvocation) >= _throttleDelay)
				{
					_lastInvocation = now;
					_callback(trigger);
				}
				return;
			}

			_triggerQueue.Enqueue(trigger);

			if (!_isThrottling)
			{
				_isThrottling = true;
				_lastInvocation = now;
				Task.Run(async () => await ProcessThrottleQueueAsync());
			}
		}
	}

	private async Task ProcessDebounceAsync(CancellationToken cancellationToken)
	{
		while (true)
		{
			T trigger;
			lock (_lock)
			{
				if (!_triggerQueue.TryDequeue(out trigger))
				{
					_isDebouncing = false;
					return;
				}
			}

			try
			{
				await Task.Delay(_debounceDelay, cancellationToken);
				_callback(trigger);
			}
			catch (TaskCanceledException)
			{
				// If cancelled, put the trigger back and restart
				lock (_lock)
				{
					_triggerQueue.Enqueue(trigger);
					_isDebouncing = false;
				}
				return;
			}
		}
	}

	private async Task ProcessThrottleQueueAsync()
	{
		while (true)
		{
			T trigger;
			lock (_lock)
			{
				if (!_triggerQueue.TryDequeue(out trigger))
				{
					_isThrottling = false;
					return;
				}
			}

			_callback(trigger);
			_lastInvocation = DateTime.UtcNow;
			await Task.Delay(_throttleDelay);
		}
	}

	#endregion
}