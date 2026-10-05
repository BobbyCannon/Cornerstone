#region References

using System;
using System.Threading;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Threading;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using DispatcherPriority = Cornerstone.Presentation.DispatcherPriority;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Observe;

[SourceReflection]
public partial class TabDateTime : UserControl
{
	#region Constants

	public const string HeaderName = "DateTime Provider";

	#endregion

	#region Fields

	private readonly IDispatcher _dispatcher;
	private bool _hasSample;
	private readonly IKeepAlive _keepAlive;
	private DateTime _lastHeartbeat;
	private long _lastWallTicks;
	private readonly Timer _sampleTimer;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabDateTime()
		: this(AppBootstrap.GetInstance<IDateTimeProvider>(), ResolveKeepAlive())
	{
	}

	public TabDateTime(IDateTimeProvider dateTimeProvider)
		: this(dateTimeProvider, null)
	{
	}

	[DependencyInjectionConstructor]
	public TabDateTime(IDateTimeProvider dateTimeProvider, IKeepAlive keepAlive)
	{
		DateTimeProvider = dateTimeProvider;
		IDispatcher dispatcher = null;
		if (AppBootstrap.DependencyProvider != null)
		{
			AppBootstrap.DependencyProvider.TryGetInstance(out dispatcher);
		}

		_dispatcher = dispatcher;
		_keepAlive = keepAlive ?? new UnsupportedKeepAlive();
		WallUtcNow = string.Empty;
		ProviderUtcNow = string.Empty;
		DeltaTicksText = string.Empty;
		KeepAliveHelp = string.Empty;
		KeepAliveStatus = string.Empty;
		Log = new PresentationList<TabDateTimeLogEntry> { Limit = 100 };
		_hasSample = false;
		_lastHeartbeat = DateTime.MinValue;
		_lastWallTicks = 0;

		DataContext = this;
		InitializeComponent();

		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, TimerTick) { IsEnabled = false };
		_sampleTimer = new Timer(SampleTimerTick);
		IsKeepAliveSupported = _keepAlive.IsSupported;
		IsKeepAliveOn = _keepAlive.IsActive;
		RefreshKeepAliveCopy();
		RefreshDisplay(true);
	}

	#endregion

	#region Properties

	public IDateTimeProvider DateTimeProvider { get; }

	[Notify]
	public partial string DeltaTicksText { get; set; }

	[Notify]
	public partial string KeepAliveHelp { get; set; }

	[Notify]
	public partial string KeepAliveStatus { get; set; }

	[Notify]
	public partial bool IsKeepAliveOn { get; set; }

	[Notify]
	public partial bool IsKeepAliveSupported { get; set; }

	public PresentationList<TabDateTimeLogEntry> Log { get; }

	[Notify]
	public partial string ProviderUtcNow { get; set; }

	[Notify]
	public partial string WallUtcNow { get; set; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		AddLog("Watch", "Tab attached. Leave this page open, power the device off, then on.");
		_timer.IsEnabled = true;
		StartSampleTimer();
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.IsEnabled = false;
		if (!_keepAlive.IsActive)
		{
			StopSampleTimer();
			AddLog("Watch", "Tab detached. Timer stopped.");
		}
		else
		{
			AddLog("Watch", "Tab detached. Keep running still sampling.");
		}

		base.OnDetachedFromVisualTree(e);
	}

	private void AddLog(string kind, string message)
	{
		Log.Insert(0, new TabDateTimeLogEntry(kind, message));
	}

	private void ClearLog(object sender, RoutedEventArgs e)
	{
		Log.Clear();
	}

	private void KeepAliveClick(object sender, RoutedEventArgs e)
	{
		if (!_keepAlive.IsSupported)
		{
			IsKeepAliveOn = false;
			return;
		}

		if (IsKeepAliveOn)
		{
			_keepAlive.Start();
			AddLog("Keep", "Keep running on.");
			StartSampleTimer();
		}
		else
		{
			_keepAlive.Stop();
			AddLog("Keep", "Keep running off.");
			if (!_timer.IsEnabled)
			{
				StopSampleTimer();
			}
		}

		RefreshKeepAliveCopy();
	}

	private static string FormatUtc(DateTime value)
	{
		return value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff") + " UTC";
	}

	private void RefreshDisplay(bool writeLog)
	{
		var wall = DateTime.UtcNow;
		var provider = DateTimeProvider.UtcNow;

		WallUtcNow = FormatUtc(wall);
		ProviderUtcNow = FormatUtc(provider);
		DeltaTicksText = (provider.Ticks - wall.Ticks).ToString("N0");

		if (writeLog)
		{
			if (_hasSample)
			{
				var wallElapsed = TimeSpan.FromTicks(wall.Ticks - _lastWallTicks);

				if (wallElapsed.Ticks > TimeSpan.TicksPerSecond)
				{
					AddLog("Resume",
						$"Pause {wallElapsed.TotalSeconds:N1} s. "
						+ $"Provider {FormatUtc(provider)}. Wall {FormatUtc(wall)}. "
						+ $"Delta (provider - wall) {(provider.Ticks - wall.Ticks):N0} ticks.");
				}
				else
				{
					AddLog("Sample", $"Wall {FormatUtc(wall)}. Provider {FormatUtc(provider)}. Delta {(provider.Ticks - wall.Ticks):N0} ticks.");
				}
			}
			else
			{
				AddLog("Sample", $"Wall {FormatUtc(wall)}. Provider {FormatUtc(provider)}. Delta {(provider.Ticks - wall.Ticks):N0} ticks.");
				_hasSample = true;
			}

			_lastHeartbeat = wall;
		}
		else if (_hasSample)
		{
			var wallElapsed = TimeSpan.FromTicks(wall.Ticks - _lastWallTicks);
			if (wallElapsed.Ticks > TimeSpan.TicksPerSecond)
			{
				AddLog("Resume",
					$"Pause {wallElapsed.TotalSeconds:N1} s. "
					+ $"Provider {FormatUtc(provider)}. Wall {FormatUtc(wall)}. "
					+ $"Delta (provider - wall) {(provider.Ticks - wall.Ticks):N0} ticks.");
				_lastHeartbeat = wall;
			}
		}

		_lastWallTicks = wall.Ticks;
	}

	private void RefreshKeepAliveCopy()
	{
		if (!_keepAlive.IsSupported)
		{
			KeepAliveStatus = "Unsupported";
			KeepAliveHelp = "This host cannot keep the CPU running in the background (browser and iOS). Android uses a foreground service and a wakelock. Desktop asks the OS not to sleep.";
			return;
		}

		KeepAliveStatus = _keepAlive.IsActive ? "On" : "Off";
		KeepAliveHelp = "Android starts a foreground service and a partial wakelock so Stopwatch can keep ticking with the screen off. Desktop asks the OS not to sleep. Samples continue if you leave this page while this is on.";
	}

	private static IKeepAlive ResolveKeepAlive()
	{
		if ((AppBootstrap.DependencyProvider != null)
			&& AppBootstrap.DependencyProvider.TryGetInstance<IKeepAlive>(out var keepAlive))
		{
			return keepAlive;
		}

		return new UnsupportedKeepAlive();
	}

	private void SampleTimerTick(object state)
	{
		if (_dispatcher != null)
		{
			_dispatcher.Dispatch(() => RefreshDisplay(true));
		}
		else
		{
			RefreshDisplay(true);
		}
	}

	private void StartSampleTimer()
	{
		_sampleTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
	}

	private void StopSampleTimer()
	{
		_sampleTimer.Change(Timeout.Infinite, Timeout.Infinite);
	}

	private void TimerTick(object sender, EventArgs e)
	{
		RefreshDisplay(false);
	}

	#endregion
}

public class TabDateTimeLogEntry
{
	#region Constructors

	public TabDateTimeLogEntry(string kind, string message)
	{
		Kind = kind;
		Message = message;
	}

	#endregion

	#region Properties

	public string Kind { get; }

	public string Message { get; }

	#endregion
}
