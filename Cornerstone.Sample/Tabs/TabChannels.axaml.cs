#region References

using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Charts;
using Cornerstone.Communications;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabChannels : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Channels";

	#endregion

	#region Fields

	private readonly BackgroundWorker _benchmark;
	private readonly byte[] _buffer;
	private readonly MemoryChannel _client;
	private readonly ChannelControl _clientControl;
	private readonly byte[] _lengthBuffer;
	private readonly byte[] _readBuffer;
	private readonly MemoryChannel _server;
	private readonly ChannelControl _serverControl;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabChannels()
	{
		_buffer = new byte[288];
		_server = new MemoryChannel(_buffer);
		_client = new MemoryChannel(_server);

		_benchmark = new BackgroundWorker();
		_benchmark.WorkerSupportsCancellation = true;
		_readBuffer = new byte[_server.DataCapacity];
		_lengthBuffer = new byte[4];

		Profiler = new Profiler();
		(RoundTripData, PerSecondData) = Profiler.SetupScopeHistory("RoundTrip");

		DataContext = this;
		InitializeComponent();

		_serverControl = this.FindControl<ChannelControl>("ServerChannel")!;
		_serverControl.Channel = _server;

		_clientControl = this.FindControl<ChannelControl>("ClientChannel")!;
		_clientControl.Channel = _client;

		_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, TimerUpdate) { IsEnabled = false };
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool GraphDuringBenchmarking { get; set; }

	[Notify]
	public partial bool IsBenchmarking { get; set; }

	public ISeriesDataProvider PerSecondData { get; }

	public ISeriesDataProvider RoundTripData { get; }

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		RenderChart.ValueFormatter = x => TimeSpan.FromTicks((long) x).Humanize();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_benchmark.DoWork += BenchmarkOnDoWork;
		_timer.IsEnabled = true;
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
				if (!GraphDuringBenchmarking)
				{
					_clientControl.Channel = null;
					_serverControl.Channel = null;
				}
				_benchmark.RunWorkerAsync();
			}
			if (!IsBenchmarking && _benchmark.IsBusy)
			{
				_benchmark.CancelAsync();
				_clientControl.Channel = _client;
				_serverControl.Channel = _server;
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
		var testBuffer = new byte[16];
		var readBuffer = new byte[64];
		var lengthBuffer = new byte[4];
		var testLength = 7;

		RandomGenerator.Populate(ref testBuffer);

		while (!_benchmark.CancellationPending)
		{
			if (!IsBenchmarking)
			{
				Thread.Sleep(10);
				continue;
			}

			testLength = 8 + (((testLength + 1) - 8) % 9);
			var start = 16 - testLength;

			using (ProfilerExtensions.Start(Profiler, "RoundTrip"))
			{
				_server.Send(testBuffer, start, testLength);
				if (_client.TryToRead(readBuffer, lengthBuffer))
				{
					var readLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
					_client.Send(readBuffer, 0, readLength);
				}
				_server.TryToRead(readBuffer, lengthBuffer);
			}
		}
	}

	private void ReadFromClient(object sender, RoutedEventArgs e)
	{
		if (!_client.TryToRead(_readBuffer, _lengthBuffer))
		{
			return;
		}

		var length = BinaryPrimitives.ReadInt32LittleEndian(_lengthBuffer.AsSpan());
		var ticks = BinaryPrimitives.ReadInt64LittleEndian(_readBuffer.AsSpan(0, 8));
		var date = new DateTime(ticks);
		Monitor.ViewModel.Append($"R < Client: {date:G}{Environment.NewLine}");
	}

	private void ReadFromServer(object sender, RoutedEventArgs e)
	{
		if (!_server.TryToRead(_readBuffer, _lengthBuffer))
		{
			return;
		}

		var length = BinaryPrimitives.ReadInt32LittleEndian(_lengthBuffer.AsSpan());
		var ticks = BinaryPrimitives.ReadInt64LittleEndian(_readBuffer.AsSpan(0, 8));
		var date = new DateTime(ticks);
		Monitor.ViewModel.Append($"R < Server: {date:G}{Environment.NewLine}");
	}

	private void SendMessageToClient(object sender, RoutedEventArgs e)
	{
		var currentTime = DateTime.Now;
		var bytes = BitConverter.GetBytes(currentTime.Ticks);
		_server.Send(bytes, 0, bytes.Length);
		Monitor.ViewModel.Append($"W > Client {DateTime.Now:G}{Environment.NewLine}");
		_clientControl.InvalidateVisual();
	}

	private void SendMessageToServer(object sender, RoutedEventArgs e)
	{
		var currentTime = DateTime.Now;
		var bytes = BitConverter.GetBytes(currentTime.Ticks);
		_client.Send(bytes, 0, bytes.Length);
		Monitor.ViewModel.Append($"W > Server {DateTime.Now:G}{Environment.NewLine}");
		_serverControl.InvalidateVisual();
	}

	private void TimerUpdate(object sender, EventArgs e)
	{
		Profiler.Refresh();
	}

	#endregion
}