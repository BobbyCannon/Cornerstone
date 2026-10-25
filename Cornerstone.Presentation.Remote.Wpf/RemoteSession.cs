#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cornerstone.Presentation.Remote.Protocol;
using Cornerstone.Presentation.Remote.Protocol.Input;
using Cornerstone.Presentation.Remote.Protocol.Viewport;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// Cornerstone remote protocol client: listen or connect, handshake, frames, input.
/// Visual Studio designer policy (XAML update) stays out of this type.
/// </summary>
public sealed class RemoteSession : IDisposable
{
	#region Fields

	private ICornerstoneRemoteTransportConnection _connection;
	private Dispatcher _dispatcher;
	private readonly object _frameGate;
	private IDisposable _listener;
	private RemoteFrame _pendingFrame;
	private bool _processingFrame;
	private int _writeSlot;
	private int _disposed;
	private int _copyScheduled;
	private int _preferInputCopy;
	private int _pumpStopped;
	private int _pumpSuspended;
	private byte[] _slot0;
	private byte[] _slot1;

	#endregion

	#region Constructors

	public RemoteSession()
	{
		_frameGate = new object();
		_pendingFrame = null;
		_processingFrame = false;
		_writeSlot = 0;
		_disposed = 0;
		_copyScheduled = 0;
		_preferInputCopy = 1;
		_pumpStopped = 0;
		_pumpSuspended = 0;
		_slot0 = Array.Empty<byte>();
		_slot1 = Array.Empty<byte>();
		PauseFrames = false;
		Scaling = 1;
	}

	#endregion

	#region Properties

	public WriteableBitmap Bitmap { get; private set; }

	public bool IsConnected => _connection != null;

	public bool IsListening => _listener != null;

	public int IgnoredFrameWidth { get; private set; }

	public int IgnoredFrameHeight { get; private set; }

	/// <summary>
	/// When true, frames are acknowledged but not applied (invalid markup freeze).
	/// </summary>
	public bool PauseFrames { get; set; }

	public double Scaling { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Binds frame application to <paramref name="dispatcher"/>.
	/// The first copy after attach, resume, or a new listen is posted at Input
	/// so a restored document paints. Later copies are posted at Background.
	/// Background yields while a message is waiting. A chain of Input copies
	/// does not: with no key down, each one posts a dispatcher message that
	/// sits in front of a build until Ctrl. There is no CompositionTarget.Rendering hook.
	/// </summary>
	public void AttachDispatcher(Dispatcher dispatcher)
	{
		if ((dispatcher == null) || (_disposed != 0))
		{
			return;
		}

		_dispatcher = dispatcher;

		var hasPending = false;
		lock (_frameGate)
		{
			hasPending = _pendingFrame != null;
		}

		if (hasPending)
		{
			Interlocked.Exchange(ref _preferInputCopy, 1);
			EnsurePump();
		}
	}

	[RequiresUnreferencedCode("Bson uses reflection")]
	public Task ConnectAsync(IPAddress address, int port)
	{
		return ConnectCoreAsync(address, port);
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _disposed, 1);
		Stop();
	}

	[RequiresUnreferencedCode("Bson uses reflection")]
	public int ListenLoopback()
	{
		if (_disposed != 0)
		{
			throw new ObjectDisposedException(nameof(RemoteSession));
		}

		if (_listener != null)
		{
			throw new InvalidOperationException("Remote session already listening.");
		}

		// Stop() leaves the pump stopped. Recycle listens again on this same
		// session, and those frames must present.
		Interlocked.Exchange(ref _pumpStopped, 0);
		Interlocked.Exchange(ref _pumpSuspended, 0);
		Interlocked.Exchange(ref _preferInputCopy, 1);
		lock (_frameGate)
		{
			_copyScheduled = 0;
		}

		var port = FreeTcpPort();
		var transport = new BsonTcpTransport();
		_listener = transport.Listen(IPAddress.Loopback, port, OnTransportConnected);
		return port;
	}

	public Task SendAsync(object message)
	{
		var connection = _connection;
		if (connection == null)
		{
			return Task.CompletedTask;
		}

		return connection.Send(message);
	}

	public Task SendPointerMovedAsync(double x, double y, MouseEventArgs e)
	{
		return SendAsync(new PointerMovedEventMessage
		{
			X = x,
			Y = y,
			Modifiers = RemoteInput.GetModifiers(e)
		});
	}

	public Task SendPointerPressedAsync(double x, double y, MouseButtonEventArgs e)
	{
		return SendAsync(new PointerPressedEventMessage
		{
			X = x,
			Y = y,
			Button = RemoteInput.GetButton(e.ChangedButton),
			Modifiers = RemoteInput.GetModifiers(e)
		});
	}

	public Task SendPointerReleasedAsync(double x, double y, MouseButtonEventArgs e)
	{
		return SendAsync(new PointerReleasedEventMessage
		{
			X = x,
			Y = y,
			Button = RemoteInput.GetButton(e.ChangedButton),
			Modifiers = RemoteInput.GetModifiers(e)
		});
	}

	public Task SendScrollAsync(double x, double y, MouseWheelEventArgs e)
	{
		var delta = e.Delta / 120.0;
		var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
		return SendAsync(new ScrollEventMessage
		{
			X = x,
			Y = y,
			DeltaX = shift ? delta : 0,
			DeltaY = shift ? 0 : delta,
			Modifiers = RemoteInput.GetModifiers(e)
		});
	}

	public Task SendRenderInfoAsync()
	{
		var scaling = Scaling > 0 ? Scaling : 1;
		return SendAsync(new ClientRenderInfoMessage
		{
			DpiX = 96 * scaling,
			DpiY = 96 * scaling
		});
	}

	public Task SendViewportAllocatedAsync(double width, double height, double dpiX, double dpiY)
	{
		return SendAsync(new ClientViewportAllocatedMessage
		{
			Width = width,
			Height = height,
			DpiX = dpiX,
			DpiY = dpiY
		});
	}

	public async Task SetScalingAsync(double scaling)
	{
		if (scaling <= 0)
		{
			scaling = 1;
		}

		scaling = Math.Round(scaling, 4, MidpointRounding.AwayFromZero);
		if (Math.Abs(Scaling - scaling) < 0.0001)
		{
			return;
		}

		Scaling = scaling;
		if (IsConnected)
		{
			await SendRenderInfoAsync().ConfigureAwait(false);
		}
	}

	public void Stop()
	{
		RequestStopPump();
		_listener?.Dispose();
		_listener = null;
		if (_connection != null)
		{
			_connection.OnMessage -= OnMessage;
			_connection.OnException -= OnException;
			_connection.Dispose();
			_connection = null;
		}
	}

	/// <summary>
	/// Stops presenting without closing the socket. Safe on the UI thread.
	/// A later <see cref="Stop"/> still disposes the connection.
	/// The pump stays stopped until the next <see cref="ListenLoopback"/>.
	/// </summary>
	public void RequestStopPump()
	{
		Interlocked.Exchange(ref _pumpStopped, 1);
		lock (_frameGate)
		{
			_pendingFrame = null;
			_processingFrame = false;
		}

		// Send runs before a queued copy so a frame already in the dispatcher
		// does not paint after the pump is stopped.
		PostToDispatcher(StopPumpOnUi, DispatcherPriority.Send);
	}

	/// <summary>
	/// Stops presenting until <see cref="ResumePump"/>. Frames are still
	/// acknowledged. Used while the document is hidden or showing source only.
	/// The latest frame is kept so resume can show it.
	/// </summary>
	public void SuspendPump()
	{
		Interlocked.Exchange(ref _pumpSuspended, 1);
		lock (_frameGate)
		{
			// Keep the latest frame. ResumePump presents it. Clearing it here
			// acknowledged a paint the surface never showed.
			_processingFrame = false;
		}

		var dispatcher = _dispatcher;
		if ((dispatcher != null) && dispatcher.CheckAccess())
		{
			StopPumpOnUi();
			return;
		}

		BeginOnDispatcher(StopPumpOnUi, DispatcherPriority.Send);
	}

	/// <summary>
	/// Allows frames to present again after <see cref="SuspendPump"/>.
	/// </summary>
	public void ResumePump()
	{
		Interlocked.Exchange(ref _pumpSuspended, 0);

		var hasPending = false;
		lock (_frameGate)
		{
			hasPending = _pendingFrame != null;
		}

		if (hasPending)
		{
			Interlocked.Exchange(ref _preferInputCopy, 1);
			EnsurePump();
		}
	}

	private async Task CompleteConnectAsync()
	{
		try
		{
			await HandshakeAsync().ConfigureAwait(false);
			Connected?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Faulted?.Invoke(this, ex);
		}
	}

	[RequiresUnreferencedCode("Bson uses reflection")]
	private async Task ConnectCoreAsync(IPAddress address, int port)
	{
		var transport = new BsonTcpTransport();
		var connection = await transport.Connect(address, port).ConfigureAwait(false);
		OnTransportConnected(connection);
	}

	private static ClientSupportedPixelFormatsMessage CreatePixelFormats()
	{
		return new ClientSupportedPixelFormatsMessage
		{
			Formats = new[]
			{
				PixelFormat.Bgra8888,
				PixelFormat.Rgba8888
			}
		};
	}

	private static int FreeTcpPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint) listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private async Task HandshakeAsync()
	{
		await SendAsync(CreatePixelFormats()).ConfigureAwait(false);
		if (Scaling <= 0)
		{
			Scaling = 1;
		}

		await SendRenderInfoAsync().ConfigureAwait(false);
	}

	private void OnException(ICornerstoneRemoteTransportConnection connection, Exception exception)
	{
		Faulted?.Invoke(this, exception);
	}

	private void OnMessage(ICornerstoneRemoteTransportConnection connection, object message)
	{
		if (message is FrameMessage frame)
		{
			// Store even while the surface is suspended. A restored document can
			// hide and show during solution open. Acknowledging a frame that was
			// not stored drops the only paint; the host will not send it again.
			if ((_disposed == 0) && CopyFrameToPending(frame) && AcceptsFrames)
			{
				EnsurePump();
			}

			_ = SendAsync(new FrameReceivedMessage { SequenceId = frame.SequenceId });
			return;
		}

		MessageReceived?.Invoke(this, message);
	}

	private bool CopyFrameToPending(FrameMessage frame)
	{
		// Invalid markup keeps the last good frame. Still acknowledge below.
		if ((_disposed != 0) || PauseFrames)
		{
			return false;
		}

		var source = frame.Data;
		var length = source != null ? source.Length : 0;
		lock (_frameGate)
		{
			if ((_disposed != 0) || PauseFrames)
			{
				return false;
			}

			var slot = _writeSlot == 0 ? _slot0 : _slot1;
			if ((slot == null) || (slot.Length < length))
			{
				slot = new byte[Math.Max(length, 1)];
				if (_writeSlot == 0)
				{
					_slot0 = slot;
				}
				else
				{
					_slot1 = slot;
				}
			}

			if ((length > 0) && (source != null))
			{
				Buffer.BlockCopy(source, 0, slot, 0, length);
			}

			_pendingFrame = new RemoteFrame
			{
				SequenceId = frame.SequenceId,
				Data = slot,
				Format = frame.Format,
				Width = frame.Width,
				Height = frame.Height,
				Stride = frame.Stride
			};
		}

		return true;
	}

	private bool AcceptsFrames =>
		(_disposed == 0) && (_pumpStopped == 0) && (_pumpSuspended == 0);

	private void EnsurePump()
	{
		if (!AcceptsFrames || (_dispatcher == null))
		{
			return;
		}

		// Loaded, Normal, and Render are foreground: WPF posts a dispatcher
		// message for each of them even while a key is waiting. Input does the
		// same when no key is already waiting, so a stream of Input copies keeps
		// a build sitting until Ctrl. One Input copy paints a restored document.
		// Every copy after that is Background, which arms a timer and yields.
		// The scheduled flag stays set until the copy returns, so a frame that
		// arrives during WritePixels cannot queue another Input copy into this
		// same drain. Do not subscribe to CompositionTarget.Rendering.
		var input = false;
		lock (_frameGate)
		{
			if (_copyScheduled != 0)
			{
				return;
			}

			_copyScheduled = 1;
			input = Interlocked.Exchange(ref _preferInputCopy, 0) == 1;
		}

		var priority = input ? DispatcherPriority.Input : DispatcherPriority.Background;
		if (!BeginOnDispatcher(CopyOnUi, priority))
		{
			lock (_frameGate)
			{
				_copyScheduled = 0;
			}

			if (input)
			{
				Interlocked.Exchange(ref _preferInputCopy, 1);
			}
		}
	}

	private void CopyOnUi()
	{
		if (!AcceptsFrames)
		{
			lock (_frameGate)
			{
				_copyScheduled = 0;
			}

			return;
		}

		ProcessPendingFrameOnUi();

		var hasPending = false;
		lock (_frameGate)
		{
			_copyScheduled = 0;
			hasPending = _pendingFrame != null;
		}

		// The frame that arrived during the copy was acknowledged and stored.
		// One Background follow-up shows it. Posting that follow-up at Input
		// would run before this callback returned to the message loop.
		if (hasPending)
		{
			EnsurePump();
		}
	}

	private void StopPumpOnUi()
	{
		lock (_frameGate)
		{
			_copyScheduled = 0;
		}
	}

	private bool BeginOnDispatcher(Action action, DispatcherPriority priority)
	{
		var dispatcher = _dispatcher;
		if ((dispatcher == null) || (action == null))
		{
			return false;
		}

		try
		{
			dispatcher.BeginInvoke(priority, action);
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private void RaiseFrameReceived()
	{
		if (_disposed != 0)
		{
			return;
		}

		FrameReceived?.Invoke(this, EventArgs.Empty);
	}

	private void PostToDispatcher(Action action, DispatcherPriority priority)
	{
		var dispatcher = _dispatcher;
		if ((dispatcher == null) || (action == null))
		{
			return;
		}

		try
		{
			if (dispatcher.CheckAccess())
			{
				action();
			}
			else
			{
				dispatcher.BeginInvoke(priority, action);
			}
		}
		catch (InvalidOperationException)
		{
			// Dispatcher is already shut down.
		}
	}

	private void OnTransportConnected(ICornerstoneRemoteTransportConnection connection)
	{
		if (_disposed != 0)
		{
			connection.Dispose();
			return;
		}

		_connection = connection;
		connection.OnMessage += OnMessage;
		connection.OnException += OnException;
		_ = CompleteConnectAsync();
	}

	private bool ProcessPendingFrameOnUi()
	{
		RemoteFrame frame;
		lock (_frameGate)
		{
			if (_processingFrame)
			{
				return false;
			}

			frame = _pendingFrame;
			_pendingFrame = null;
			if (frame == null)
			{
				return false;
			}

			_processingFrame = true;
			_writeSlot = _writeSlot == 0 ? 1 : 0;
		}

		try
		{
			if (PauseFrames || !AcceptsFrames)
			{
				return false;
			}

			if ((frame.Width <= 1) && (frame.Height <= 1) && (Bitmap != null))
			{
				IgnoredFrameWidth = frame.Width;
				IgnoredFrameHeight = frame.Height;
				FrameIgnored?.Invoke(this, EventArgs.Empty);
				return false;
			}

			var previous = Bitmap;
			Bitmap = RemoteFrameBitmap.Apply(Bitmap, frame);
			// WritePixels invalidates the image. Layout runs on this callback
			// only when the bitmap instance changes.
			FramePainted?.Invoke(this, EventArgs.Empty);
			if (!ReferenceEquals(previous, Bitmap))
			{
				RaiseFrameReceived();
			}

			return true;
		}
		finally
		{
			lock (_frameGate)
			{
				_processingFrame = false;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler Connected;

	public event EventHandler<Exception> Faulted;

	public event EventHandler FrameReceived;

	/// <summary>
	/// Raised on the UI thread after pixels are copied, including when the size did not change.
	/// </summary>
	public event EventHandler FramePainted;

	public event EventHandler FrameIgnored;

	public event EventHandler<object> MessageReceived;

	#endregion
}