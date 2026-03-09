#region References

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Cornerstone.Serialization;
using Cornerstone.Text;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabSpeedyPack : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Speedy Pack";

	#endregion

	#region Fields

	private readonly BackgroundWorker _benchmark;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabSpeedyPack()
	{
		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, ProviderUpdate) { IsEnabled = false };
		_benchmark = new BackgroundWorker();
		_benchmark.WorkerSupportsCancellation = true;

		Profiler = new Profiler();

		(SpeedyPackAverageData, SpeedyPackPerSecondData) = Profiler.SetupScopeHistory("SpeedyPack");
		(SystemJsonAverageData, SystemJsonPerSecondData) = Profiler.SetupScopeHistory("SystemJson");

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool IsBenchmarking { get; set; }

	public ISeriesDataProvider SpeedyPackAverageData { get; }

	public ISeriesDataProvider SpeedyPackPerSecondData { get; }

	public uint SpeedyPackSize { get; private set; }

	public ISeriesDataProvider SystemJsonAverageData { get; }

	public ISeriesDataProvider SystemJsonPerSecondData { get; }

	public uint SystemJsonSize { get; private set; }

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		SpeedyPackChart.ValueFormatter = x => TimeSpan.FromTicks((long) x).Humanize();
		SystemJsonChart.ValueFormatter = x => TimeSpan.FromTicks((long) x).Humanize();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (!Design.IsDesignMode)
		{
			_timer.IsEnabled = true;
		}
		_benchmark.DoWork += BenchmarkOnDoWork;
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_benchmark.DoWork -= BenchmarkOnDoWork;
		_timer.IsEnabled = false;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(string propertyName)
	{
		if (propertyName == nameof(IsBenchmarking))
		{
			if (IsBenchmarking && !_benchmark.IsBusy)
			{
				_benchmark.RunWorkerAsync();
			}
			if (!IsBenchmarking && _benchmark.IsBusy)
			{
				_benchmark.CancelAsync();
			}
		}
		base.OnPropertyChanged(propertyName);
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		IsBenchmarking = false;
		_benchmark.CancelAsync();
		base.OnUnloaded(e);
	}

	private void BenchmarkOnDoWork(object sender, DoWorkEventArgs e)
	{
		var buffer = new SpeedyBuffer<byte>();
		var options = new JsonSerializerOptions
		{
			TypeInfoResolver = AppJsonSerializerContext.Default
		};

		var account = new Account();

		while (!_benchmark.CancellationPending)
		{
			if (!IsBenchmarking)
			{
				Thread.Sleep(10);
				continue;
			}

			account.CreatedOn = DateTime.UtcNow;
			account.ModifiedOn = DateTime.UtcNow;
			account.DisplayName = RandomGenerator.NextString(RandomGenerator.NextInteger(10, 100));
			account.EmailAddress = RandomGenerator.NextString(RandomGenerator.NextInteger(25, 255));
			account.SyncId = Guid.NewGuid();
			account.TimeZoneId = TimeZoneInfo.Local.Id;
			account.IsEnabled = true;
			account.LastLoginDate = DateTime.UtcNow;

			try
			{
				var t1 = ProfilerExtensions.Start(Profiler, "SpeedyPack");
				SpeedyPackWriter.Write([account], buffer);
				var r = new SpeedyPackReader(buffer.AsSpan());
				t1.Dispose();

				var t2 = ProfilerExtensions.Start(Profiler, "SystemJson");
				var json = JsonSerializer.Serialize(account, options);
				var account2 = JsonSerializer.Deserialize<Account>(json, options);
				t2.Dispose();

				SpeedyPackSize += (uint) buffer.Length;
				SystemJsonSize += (uint) json.Length;
			}
			catch (Exception ex)
			{
				this.Dispatch(() =>
				{
					Monitor.ViewModel.Document.Add(ex.Message);
					Monitor.ViewModel.Document.Add(Environment.NewLine);
				});

				Thread.Sleep(100);
			}
		}
	}

	private void ProviderUpdate(object sender, EventArgs e)
	{
		Profiler.Refresh();
		OnPropertyChanged(nameof(SpeedyPackSize));
		OnPropertyChanged(nameof(SystemJsonSize));
	}

	#endregion
}