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
	private bool _compositionHooked;
	private TimeSpan _lastRenderingTime;
	private IDisposable _listener;
	private RemoteFrame _pendingFrame;
	private bool _processingFrame;
	private int _writeSlot;
	private int _disposed;
	private int _presentScheduled;
	private int _renderKickScheduled;
	private int _pumpStopped;
	private int _pumpSuspended;
	private TimeSpan _lastFrameRenderingTime;
	private byte[] _slot0;
	private byte[] _slot1;

	/// <summary>
	/// How long the composition hook stays up after the last painted frame.
	/// Host frames arrive between WPF passes. Dropping the hook on the first
	/// empty pass made the next frame wait on the Background queue, which is
	/// the pause-then-burst jitter. A static preview still unhooks after this.
	/// </summary>
	private const int RenderHookHoldMilliseconds = 200;

	#endregion

	#region Constructors

	public RemoteSession()
	{
		_frameGate = new object();
		_compositionHooked = false;
		_lastRenderingTime = TimeSpan.MinValue;
		_pendingFrame = null;
		_processingFrame = false;
		_writeSlot = 0;
		_disposed = 0;
		_presentScheduled = 0;
		_renderKickScheduled = 0;
		_pumpStopped = 0;
		_pumpSuspended = 0;
		_lastFrameRenderingTime = TimeSpan.MinValue;
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
	/// One callback below keyboard input installs the render hook. Later frames
	/// ride that hook. One coalesced kick, also below Render, asks for the next
	/// pass. The hook stays up briefly after the last frame so a live preview
	/// does not re-enter through the Background queue, then it drops.
	/// A size change posts layout below input instead of running it on the render pass.
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
		lock (_frameGate)
		{
			_presentScheduled = 0;
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
	/// Drops the composition hook without closing the socket. Safe on the UI thread.
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

		// Send runs before a queued present. A Background unhook never ran while
		// frames were still posting work above the keyboard.
		PostToDispatcher(StopPumpOnUi, DispatcherPriority.Send);
	}

	/// <summary>
	/// Unhooks the render pump until <see cref="ResumePump"/>. Frames are still
	/// acknowledged. Used while the document is hidden or showing source only,
	/// so a live host does not keep the shell's render band full.
	/// </summary>
	public void SuspendPump()
	{
		Interlocked.Exchange(ref _pumpSuspended, 1);
		lock (_frameGate)
		{
			_pendingFrame = null;
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
			if (AcceptsFrames && CopyFrameToPending(frame))
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

		// One posted callback installs the hook. A Render or Normal post per frame
		// sits above the keyboard, and the shell ignores input until a resize.
		// While the hook is up, one coalesced Input kick schedules the next pass.
		// That kick is not a Render post, and a second frame does not queue another.
		var start = false;
		var hooked = false;
		lock (_frameGate)
		{
			hooked = _compositionHooked;
			if (!hooked && (_presentScheduled == 0))
			{
				_presentScheduled = 1;
				start = true;
			}
		}

		if (hooked)
		{
			SchedulePresentKick();
		}

		if (start && !BeginOnDispatcher(StartPresentOnUi, DispatcherPriority.Background))
		{
			lock (_frameGate)
			{
				_presentScheduled = 0;
			}
		}
	}

	private void StartPresentOnUi()
	{
		if (!AcceptsFrames)
		{
			StopPumpOnUi();
			return;
		}

		if (_dispatcher == null)
		{
			lock (_frameGate)
			{
				_presentScheduled = 0;
			}

			return;
		}

		var subscribe = false;
		lock (_frameGate)
		{
			_presentScheduled = 0;
			if (!_compositionHooked)
			{
				_compositionHooked = true;
				subscribe = true;
			}
		}

		if (!subscribe)
		{
			return;
		}

		System.Windows.Media.CompositionTarget.Rendering += OnCompositionRendering;
		WakePresent();
	}

	private void OnCompositionRendering(object sender, EventArgs e)
	{
		if (!AcceptsFrames)
		{
			StopPumpOnUi();
			return;
		}

		var rendering = e as System.Windows.Media.RenderingEventArgs;
		if (rendering != null)
		{
			// WPF raises this twice for one pass. The second call has the same time.
			if (rendering.RenderingTime == _lastRenderingTime)
			{
				return;
			}

			_lastRenderingTime = rendering.RenderingTime;
		}

		var painted = ProcessPendingFrameOnUi();
		if (painted)
		{
			_lastFrameRenderingTime = _lastRenderingTime;
		}

		var pending = false;
		lock (_frameGate)
		{
			pending = (_pendingFrame != null) || _processingFrame;
		}

		// Stay hooked across the gap between host frames. The queue is often
		// empty at the pass even while the preview is moving, because the next
		// frame has not arrived yet. Unhooking there sent every frame back
		// through the Background queue.
		if (!pending && !painted && !WithinRenderHookHold())
		{
			var unhook = false;
			lock (_frameGate)
			{
				if ((_pendingFrame == null) && !_processingFrame && _compositionHooked)
				{
					_compositionHooked = false;
					unhook = true;
				}
			}

			if (unhook)
			{
				System.Windows.Media.CompositionTarget.Rendering -= OnCompositionRendering;
				return;
			}
		}

		SchedulePresentKick();
	}

	private bool WithinRenderHookHold()
	{
		if ((_lastFrameRenderingTime == TimeSpan.MinValue) || (_lastRenderingTime == TimeSpan.MinValue))
		{
			return false;
		}

		var elapsed = _lastRenderingTime - _lastFrameRenderingTime;
		return (elapsed >= TimeSpan.Zero) && (elapsed.TotalMilliseconds < RenderHookHoldMilliseconds);
	}

	/// <summary>
	/// Asks WPF for another composition pass. At most one kick is queued.
	/// Input is below Render, so this does not sit above the keyboard, and the
	/// same band still drains.
	/// </summary>
	private void SchedulePresentKick()
	{
		if (!AcceptsFrames)
		{
			return;
		}

		lock (_frameGate)
		{
			if (!_compositionHooked || (_renderKickScheduled != 0))
			{
				return;
			}

			_renderKickScheduled = 1;
		}

		if (!BeginOnDispatcher(KickPresentOnUi, DispatcherPriority.Input))
		{
			lock (_frameGate)
			{
				_renderKickScheduled = 0;
			}
		}
	}

	private void KickPresentOnUi()
	{
		var hooked = false;
		var pending = false;
		lock (_frameGate)
		{
			_renderKickScheduled = 0;
			hooked = _compositionHooked;
			pending = _pendingFrame != null;
		}

		if (!AcceptsFrames || !hooked)
		{
			return;
		}

		if (pending || WithinRenderHookHold())
		{
			WakePresent();
		}
	}

	private void WakePresent()
	{
		var bitmap = Bitmap;
		if ((bitmap == null) || (bitmap.PixelWidth <= 0) || (bitmap.PixelHeight <= 0))
		{
			return;
		}

		// A dirty bitmap schedules the render pass that raises Rendering.
		try
		{
			bitmap.Lock();
			try
			{
				bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
			}
			finally
			{
				bitmap.Unlock();
			}
		}
		catch (InvalidOperationException)
		{
			// The bitmap can already be locked by the render pass that subscribed us.
		}
	}

	private void StopPumpOnUi()
	{
		_lastFrameRenderingTime = TimeSpan.MinValue;
		lock (_frameGate)
		{
			_presentScheduled = 0;
			_renderKickScheduled = 0;
			if (!_compositionHooked)
			{
				return;
			}

			_compositionHooked = false;
		}

		System.Windows.Media.CompositionTarget.Rendering -= OnCompositionRendering;
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
			// WritePixels already invalidates the image. Layout subscribers run only
			// when the bitmap instance changes (first frame or a new size), and they
			// run below input. Raising them on this stack is inside the render pass.
			FramePainted?.Invoke(this, EventArgs.Empty);
			if (!ReferenceEquals(previous, Bitmap))
			{
				BeginOnDispatcher(RaiseFrameReceived, DispatcherPriority.Background);
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