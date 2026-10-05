#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.AirPlay.Models.Configs;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

/// <summary>
/// App-wide AirPlay mirroring receiver. Tabs add/remove clients so mDNS is advertised once.
/// </summary>
[SourceReflection]
[DependencyInjected(TypeLifetime.Singleton)]
public sealed class AirPlayMirrorService : IDisposable
{
	#region Fields

	private int _clients;
	private AirPlayH264Decoder _decoder;
	private readonly IDispatcher _dispatcher;

	private readonly object _gate = new();
	private int _lastStatusDecoded;
	private int _lastStatusPackets;
	private int _lastStatusType0;
	private byte[] _previewPixels;
	private AirPlayReceiver _receiver;
	private CancellationTokenSource _runToken;
	private int _uiQueued;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AirPlayMirrorService(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		StatusText = "Idle";
		ReceiverName = Environment.MachineName;
	}

	#endregion

	#region Properties

	public bool IsAdvertising { get; private set; }

	public bool IsConnected { get; private set; }

	public int PreviewHeight { get; private set; }

	public byte[] PreviewPixels { get; private set; }

	public int PreviewWidth { get; private set; }

	public string ReceiverName { get; }

	public string StatusText { get; private set; }

	#endregion

	#region Methods

	public void AddClient()
	{
		lock (_gate)
		{
			_clients++;
			if (_clients == 1)
			{
				StartReceiver();
			}
		}

		RaiseChanged();
	}

	public void Disconnect()
	{
		lock (_gate)
		{
			if (_receiver == null)
			{
				return;
			}

			try
			{
				_receiver.DisconnectMirroringAsync().GetAwaiter().GetResult();
			}
			catch
			{
			}

			_decoder?.Reset();
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			_clients = 0;
			StopReceiver();
		}
	}

	public void RemoveClient()
	{
		lock (_gate)
		{
			if (_clients > 0)
			{
				_clients--;
			}

			if (_clients == 0)
			{
				StopReceiver();
			}
		}

		RaiseChanged();
	}

	private void FlushUi()
	{
		Interlocked.Exchange(ref _uiQueued, 0);
		var decoder = _decoder;
		if ((decoder != null) && decoder.CopyPixels(ref _previewPixels, out var width, out var height))
		{
			PreviewPixels = _previewPixels;
			PreviewWidth = width;
			PreviewHeight = height;
		}
		else
		{
			PreviewPixels = null;
			PreviewWidth = 0;
			PreviewHeight = 0;
		}

		if (IsConnected && (decoder != null))
		{
			var type0 = _receiver?.MirroringType0 ?? 0;
			if ((decoder.Packets != _lastStatusPackets)
				|| (decoder.DecodedFrames != _lastStatusDecoded)
				|| (type0 != _lastStatusType0)
				|| string.IsNullOrEmpty(StatusText)
				|| StatusText.StartsWith("Waiting", StringComparison.Ordinal))
			{
				// Status is diagnostic; refresh at most every 8 decoded frames.
				if (((decoder.DecodedFrames - _lastStatusDecoded) >= 8)
					|| (decoder.DecodedFrames < _lastStatusDecoded)
					|| (_lastStatusPackets == 0))
				{
					_lastStatusPackets = decoder.Packets;
					_lastStatusDecoded = decoder.DecodedFrames;
					_lastStatusType0 = type0;
					var error = decoder.LastError;
					var pictureWidth = PreviewWidth > 0 ? PreviewWidth : decoder.PictureWidth;
					var pictureHeight = PreviewHeight > 0 ? PreviewHeight : decoder.PictureHeight;
					var layout = "";
					if ((pictureWidth > 0) && (pictureHeight > 0))
					{
						layout = pictureHeight > pictureWidth ? "portrait" : "landscape";
						layout = $" · {layout} · {pictureWidth}×{pictureHeight}";
					}

					var wire = _receiver == null
						? ""
						: $" · wire t0={_receiver.MirroringType0} t1={_receiver.MirroringType1} last={_receiver.MirroringLastType}/{_receiver.MirroringLastSize}";
					StatusText = string.IsNullOrEmpty(error)
						? $"Mirroring{layout} · {decoder.Packets} in · {decoder.DecodedFrames} out{wire}"
						: $"Mirroring{layout} · {decoder.Packets} in · {decoder.DecodedFrames} out{wire} · {error}";
				}
			}
		}

		Changed?.Invoke(this, EventArgs.Empty);
	}

	private void OnH264(object sender, H264Data data)
	{
		_decoder?.Submit(data);
		QueueUi();
	}

	private void OnMirroringProgress(object sender, EventArgs e)
	{
		QueueUi();
	}

	private void OnMirroringStarted(object sender, EventArgs e)
	{
		_decoder?.Reset();
		IsConnected = true;
		QueueUi();
	}

	private void OnMirroringStopped(object sender, EventArgs e)
	{
		IsConnected = false;
		StatusText = IsAdvertising
			? "Waiting — Control Center → Screen Mirroring → " + ReceiverName
			: "Idle";
		QueueUi();
	}

	private void QueueUi()
	{
		if (Interlocked.CompareExchange(ref _uiQueued, 1, 0) != 0)
		{
			return;
		}

		_dispatcher.Post(FlushUi);
	}

	private void RaiseChanged()
	{
		_dispatcher.Post(() => Changed?.Invoke(this, EventArgs.Empty));
	}

	private void StartReceiver()
	{
		StopReceiver();
		try
		{
			_decoder = new AirPlayH264Decoder();

			var config = new AirPlayReceiverConfig
			{
				Instance = ReceiverName,
				AirTunesPort = 5000,
				AirPlayPort = 7000,
				DeviceMacAddress = "11:22:33:44:55:66"
			};
			_receiver = new AirPlayReceiver(config);
			_receiver.OnH264DataReceived += OnH264;
			_receiver.OnMirroringStartedReceived += OnMirroringStarted;
			_receiver.OnMirroringStoppedReceived += OnMirroringStopped;
			_receiver.OnMirroringProgressReceived += OnMirroringProgress;
			_runToken = new CancellationTokenSource();
			var token = _runToken.Token;
			_ = Task.Run(async () =>
			{
				await _receiver.StartListeners(token).ConfigureAwait(false);
				await _receiver.StartMdnsAsync().ConfigureAwait(false);
			}, token);
			IsAdvertising = true;
			StatusText = "Waiting — Control Center → Screen Mirroring → " + ReceiverName;
		}
		catch (Exception ex)
		{
			IsAdvertising = false;
			StatusText = "Failed to start AirPlay: " + ex.Message;
			StopReceiver();
		}
	}

	private void StopReceiver()
	{
		IsAdvertising = false;
		IsConnected = false;
		try
		{
			_runToken?.Cancel();
		}
		catch
		{
		}

		try
		{
			_receiver?.StopAsync().GetAwaiter().GetResult();
		}
		catch
		{
		}

		if (_receiver != null)
		{
			_receiver.OnH264DataReceived -= OnH264;
			_receiver.OnMirroringStartedReceived -= OnMirroringStarted;
			_receiver.OnMirroringStoppedReceived -= OnMirroringStopped;
			_receiver.OnMirroringProgressReceived -= OnMirroringProgress;
			_receiver.Dispose();
			_receiver = null;
		}

		_decoder?.Dispose();
		_decoder = null;
		_runToken?.Dispose();
		_runToken = null;
		PreviewPixels = null;
		PreviewWidth = 0;
		PreviewHeight = 0;
		StatusText = "Idle";
	}

	#endregion

	#region Events

	public event EventHandler Changed;

	#endregion
}