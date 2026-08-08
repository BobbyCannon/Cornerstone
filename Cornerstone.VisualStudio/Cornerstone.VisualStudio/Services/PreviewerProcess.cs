#region References

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xaml;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Remote.Protocol.Viewport;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using PixelFormat = System.Windows.Media.PixelFormat;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Manages running a XAML previewer process.
/// </summary>
public class PreviewerProcess : IDisposable, ILogEventEnricher
{
	#region Fields

	/// <summary>
	/// Minimum time between UI frame notifications. Pixel data is still applied for every
	/// processed frame; this only throttles <see cref="FrameReceived"/> (WPF Source refresh).
	/// ~60 FPS is fine because inactive tabs suspend their host — typically one live previewer.
	/// Frame coalescing still drops backlog if the UI thread falls behind.
	/// </summary>
	private static readonly TimeSpan MinUiFrameInterval = TimeSpan.FromMilliseconds(16); // ~60 FPS

	private string _assemblyPath;
	private WriteableBitmap _bitmap;
	private IAvaloniaRemoteTransportConnection _connection;
	private ExceptionDetails _error;
	private string _executablePath;
	private DateTime _lastUiFrameUtc = DateTime.MinValue;
	private IDisposable _listener;
	private readonly ILogger _log;
	private readonly SemaphoreSlim _messageGate = new(1, 1);
	private FrameMessage _pendingFrame;
	private Process _process;
	private int _stopping;
	private readonly object _frameGate = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PreviewerProcess" /> class.
	/// </summary>
	public PreviewerProcess()
	{
		_log = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Destructure.ToMaximumStringLength(32)
			.Enrich.With(this)
			.WriteTo.Logger(Log.Logger)
			.CreateLogger();

		Scaling = 1;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current preview as a <see cref="BitmapSource" />.
	/// </summary>
	public BitmapSource Bitmap => _bitmap;

	/// <summary>
	/// Gets the bitmap that should be shown in the designer. While markup is invalid the
	/// last good frame is kept so the preview freezes instead of blanking or thrashing.
	/// </summary>
	public BitmapSource DisplayBitmap => _bitmap;

	/// <summary>
	/// Gets the current error state as returned from the previewer process.
	/// </summary>
	public ExceptionDetails Error
	{
		get => _error;
		private set
		{
			if (!Equals(_error, value))
			{
				_error = value;
				ErrorChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether the preview is frozen due to invalid markup.
	/// The host process stays alive; new frames are acknowledged but not applied.
	/// </summary>
	public bool IsMarkupPaused => _error != null;

	/// <summary>
	/// Gets a value indicating whether the previewer process is ready to receive messages.
	/// </summary>
	public bool IsReady => IsRunning && (_connection != null);

	/// <summary>
	/// Gets a value indicating whether the previewer process is currently running.
	/// </summary>
	public bool IsRunning
	{
		get
		{
			try
			{
				return (_process != null) && !_process.HasExited;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}
	}

	/// <summary>
	/// Gets scaling for the preview.
	/// </summary>
	public double Scaling { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Stops the process and disposes of all resources.
	/// </summary>
	public void Dispose()
	{
		Stop();
		_messageGate.Dispose();
	}

	/// <summary>
	/// Sends an input message to the process.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <returns> A task tracking the operation. </returns>
	public async Task SendInputAsync(InputEventMessageBase message)
	{
		if (_process == null)
		{
			throw new InvalidOperationException("Process not started.");
		}

		if (_connection == null)
		{
			throw new InvalidOperationException("Process not finished initializing.");
		}

		await SendAsync(message);
	}

	/// <summary>
	/// Sets the scaling for the preview.
	/// </summary>
	/// <param name="scaling"> The scaling factor. </param>
	/// <returns> A task tracking the operation. </returns>
	public async Task SetScalingAsync(double scaling)
	{
		if (scaling <= 0)
		{
			scaling = 1;
		}

		// Round to avoid tiny float noise retriggering host re-renders.
		scaling = Math.Round(scaling, 4, MidpointRounding.AwayFromZero);

		if (Math.Abs(Scaling - scaling) < 0.0001)
		{
			return;
		}

		Scaling = scaling;

		if (IsReady)
		{
			await SendRenderInfoAsync();
		}
	}

	/// <summary>
	/// Starts the previewer process.
	/// </summary>
	/// <param name="assemblyPath"> The path to the assembly containing the XAML. </param>
	/// <param name="executablePath"> The path to the executable to use for the preview. </param>
	/// <param name="hostAppPath"> The path to the host application. </param>
	/// <returns> A task tracking the startup operation. </returns>
	public async Task StartAsync(
		string assemblyPath,
		string executablePath,
		string hostAppPath,
		bool isNetFx)
	{
		_log.Verbose("Started PreviewerProcess.StartAsync()");

		if (_listener != null)
		{
			throw new InvalidOperationException("Previewer process already started.");
		}

		if (string.IsNullOrWhiteSpace(assemblyPath))
		{
			throw new ArgumentException(
				"Assembly path may not be null or an empty string.",
				nameof(assemblyPath));
		}

		if (string.IsNullOrWhiteSpace(executablePath))
		{
			throw new ArgumentException(
				"Executable path may not be null or an empty string.",
				nameof(executablePath));
		}

		if (string.IsNullOrWhiteSpace(hostAppPath))
		{
			throw new ArgumentException(
				"Executable path may not be null or an empty string.",
				nameof(executablePath));
		}

		if (!File.Exists(assemblyPath))
		{
			throw new FileNotFoundException(
				$"Could not find '{assemblyPath}'. " +
				"Please build your project to enable previewing and IntelliSense.");
		}

		if (!File.Exists(executablePath))
		{
			throw new FileNotFoundException(
				$"Could not find executable '{executablePath}'. " +
				"Please build your project to enable previewing and IntelliSense.");
		}

		if (!File.Exists(hostAppPath))
		{
			throw new FileNotFoundException(
				$"Could not find executable '{hostAppPath}'. " +
				"Please build your project to enable previewing and IntelliSense.");
		}

		// Ensure any previous process state is fully cleared before starting again.
		CleanupProcessState();

		_assemblyPath = assemblyPath;
		_executablePath = executablePath;
		Interlocked.Exchange(ref _stopping, 0);
		Error = null;

		var port = FreeTcpPort();
		var tcs = new TaskCompletionSource<object>();

		_listener = new BsonTcpTransport().Listen(
			IPAddress.Loopback,
			port,
			#pragma warning disable VSTHRD101
			async t =>
			{
				try
				{
					await ConnectionInitializedAsync(t);
					tcs.TrySetResult(null);
				}
				catch (Exception ex)
				{
					_log.Error(ex, "Error initializing connection");
					tcs.TrySetException(ex);
				}
			});
		#pragma warning restore VSTHRD101

		var executableDir = Path.GetDirectoryName(_executablePath);
		var targetName = Path.GetFileNameWithoutExtension(_executablePath);
		string args;
		ProcessStartInfo processInfo;
		if (!isNetFx)
		{
			var runtimeConfigPath = Path.Combine(executableDir, targetName + ".runtimeconfig.json");
			var depsPath = Path.Combine(executableDir, targetName + ".deps.json");

			EnsureExists(runtimeConfigPath);
			EnsureExists(depsPath);
			args = $@"exec --runtimeconfig ""{runtimeConfigPath}"" --depsfile ""{depsPath}"" ""{hostAppPath}"" --transport tcp-bson://127.0.0.1:{port}/ ""{_executablePath}""";
			processInfo = new ProcessStartInfo
			{
				Arguments = args,
				CreateNoWindow = true,
				FileName = "dotnet",
				WorkingDirectory = executableDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};
		}
		else
		{
			args = $@"--transport tcp-bson://127.0.0.1:{port}/ ""{_executablePath}""";
			processInfo = new ProcessStartInfo
			{
				Arguments = args,
				CreateNoWindow = true,
				FileName = hostAppPath,
				WorkingDirectory = executableDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};
		}

		_log.Information("Starting previewer process for '{ExecutablePath}'", _executablePath);
		_log.Debug("> dotnet.exe {Args}", args);

		var process = _process = Process.Start(processInfo);
		process.EnableRaisingEvents = true;
		process.OutputDataReceived += OnProcessOutputReceived;
		process.ErrorDataReceived += OnProcessErrorReceived;
		process.Exited += Abort;
		process.Exited += OnProcessExited;
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();

		void Abort(object sender, EventArgs e)
		{
			_log.Information("Process exited while waiting for connection to be initialized.");
			tcs.TrySetException(new ApplicationException($"The previewer process exited unexpectedly with code {process.ExitCode}."));
		}

		try
		{
			_log.Information("Started previewer process for '{ExecutablePath}'. Waiting for connection to be initialized.", _executablePath);
			await tcs.Task;
		}
		finally
		{
			process.Exited -= Abort;
		}

		_log.Verbose("Finished PreviewerProcess.StartAsync()");
	}

	/// <summary>
	/// Stops the previewer process without waiting for exit (cleanup may finish on
	/// <see cref="Process.Exited"/>). Prefer <see cref="StopAndWaitAsync"/> when the
	/// host must be fully down before a restart (e.g. post-build recycle).
	/// </summary>
	public void Stop()
	{
		StopCore(waitForExit: false, timeout: TimeSpan.Zero);
	}

	/// <summary>
	/// Stops the previewer process and waits until it has exited and local state is cleared.
	/// Safe to call when already stopped.
	/// </summary>
	/// <param name="timeout"> Maximum time to wait after kill for process exit. </param>
	public Task StopAndWaitAsync(TimeSpan timeout)
	{
		// WaitForExit blocks; keep it off the UI thread.
		return Task.Run(() => StopCore(waitForExit: true, timeout: timeout));
	}

	/// <summary>
	/// Kills the host (if any) and optionally waits for exit + local cleanup.
	/// </summary>
	private void StopCore(bool waitForExit, TimeSpan timeout)
	{
		if (Interlocked.Exchange(ref _stopping, 1) == 1)
		{
			// Another stop is already in flight; still wait if the caller needs a fully quiet process.
			if (waitForExit)
			{
				WaitForProcessExit(timeout);
			}

			return;
		}

		_log.Verbose("Started PreviewerProcess.Stop(waitForExit={Wait})", waitForExit);
		_log.Information("Stopping previewer process");

		_listener?.Dispose();
		_listener = null;

		if (_connection is IAvaloniaRemoteTransportConnection connection)
		{
			_connection = null;
			connection.OnMessage -= ConnectionMessageReceived;
			connection.OnException -= ConnectionExceptionReceived;

			try
			{
				connection.Dispose();
			}
			catch (Exception ex)
			{
				_log.Debug(ex, "Failed to dispose previewer connection");
			}
		}

		lock (_frameGate)
		{
			_pendingFrame = null;
		}

		var process = _process;
		if (process != null)
		{
			try
			{
				if (!process.HasExited)
				{
					_log.Debug("Killing previewer process");
					process.Kill();
				}
			}
			catch (InvalidOperationException ex)
			{
				_log.Debug(ex, "Failed to kill previewer process");
			}
			catch (Exception ex)
			{
				_log.Debug(ex, "Unexpected error killing previewer process");
			}

			if (waitForExit)
			{
				try
				{
					if (!process.HasExited)
					{
						var ms = timeout <= TimeSpan.Zero
							? 5000
							: (int) Math.Min(timeout.TotalMilliseconds, int.MaxValue);
						if (!process.WaitForExit(ms))
						{
							_log.Warning("Previewer process did not exit within {TimeoutMs}ms", ms);
						}
					}
				}
				catch (Exception ex)
				{
					_log.Debug(ex, "WaitForExit failed");
				}

				CleanupProcessState();
			}
			else
			{
				// If the process has already exited, clean up immediately. Otherwise
				// OnProcessExited will finish cleanup when the Exited event fires.
				try
				{
					if (process.HasExited)
					{
						CleanupProcessState();
					}
				}
				catch (InvalidOperationException)
				{
					CleanupProcessState();
				}
			}
		}

		_executablePath = null;
		_assemblyPath = null;

		_log.Verbose("Finished PreviewerProcess.Stop()");
	}

	/// <summary>
	/// Best-effort wait until <see cref="_process"/> is null or has exited (for concurrent Stop).
	/// </summary>
	private void WaitForProcessExit(TimeSpan timeout)
	{
		var process = _process;
		if (process == null)
		{
			return;
		}

		try
		{
			if (process.HasExited)
			{
				return;
			}

			var ms = timeout <= TimeSpan.Zero
				? 5000
				: (int) Math.Min(timeout.TotalMilliseconds, int.MaxValue);
			process.WaitForExit(ms);
		}
		catch (Exception ex)
		{
			_log.Debug(ex, "WaitForProcessExit failed");
		}
	}

	/// <summary>
	/// Updates the XAML to be previewed.
	/// </summary>
	/// <param name="xaml"> The XAML. </param>
	/// <returns>
	/// <c>true</c> if the update message was sent; <c>false</c> on transport failure
	/// (surfaced via <see cref="Error"/> without throwing).
	/// </returns>
	public async Task<bool> UpdateXamlAsync(string xaml)
	{
		if (_process == null)
		{
			throw new InvalidOperationException("Process not started.");
		}

		if (_connection == null)
		{
			throw new InvalidOperationException("Process not finished initializing.");
		}

		try
		{
			await SendAsync(new UpdateXamlMessage
			{
				AssemblyPath = _assemblyPath,
				Xaml = xaml
			});
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Keep the designer alive on transport glitches — surface as markup pause
			// instead of letting an unhandled fault take down the session.
			_log.Error(ex, "Failed to send UpdateXamlMessage");
			Error = new ExceptionDetails
			{
				Message = "Failed to update preview: " + ex.Message
			};
			return false;
		}
	}

	private void CleanupProcessState()
	{
		var process = Interlocked.Exchange(ref _process, null);
		if (process == null)
		{
			return;
		}

		try
		{
			process.OutputDataReceived -= OnProcessOutputReceived;
			process.ErrorDataReceived -= OnProcessErrorReceived;
			process.Exited -= OnProcessExited;
		}
		catch
		{
			// Ignore handler detach failures on disposed processes.
		}

		try
		{
			process.Dispose();
		}
		catch
		{
			// Ignore dispose failures.
		}
	}

	private void ConnectionExceptionReceived(IAvaloniaRemoteTransportConnection connection, Exception ex)
	{
		_log.Error(ex, "Connection error");
	}

	private async Task ConnectionInitializedAsync(IAvaloniaRemoteTransportConnection connection)
	{
		_log.Verbose("Started PreviewerProcess.ConnectionInitializedAsync()");
		_log.Information("Connection initialized");

		if (!IsRunning)
		{
			_log.Verbose("ConnectionInitializedAsync detected process has stopped: aborting");
			return;
		}

		_connection = connection;
		_connection.OnException += ConnectionExceptionReceived;
		_connection.OnMessage += ConnectionMessageReceived;

		await SendAsync(new ClientSupportedPixelFormatsMessage
		{
			Formats =
			[
				Avalonia.Remote.Protocol.Viewport.PixelFormat.Bgra8888,
				Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgba8888
			]
		});

		// Always send render info after connect. SetScalingAsync alone is a no-op when
		// Scaling was already set before the connection became ready.
		if (Scaling <= 0)
		{
			Scaling = 1;
		}

		await SendRenderInfoAsync();

		_log.Verbose("Finished PreviewerProcess.ConnectionInitializedAsync()");
	}

	private void ConnectionMessageReceived(IAvaloniaRemoteTransportConnection connection, object message)
	{
		// Coalesce frames: only the latest pending frame is kept so a slow UI thread
		// cannot build an unbounded backlog of pixel buffers.
		if (message is FrameMessage frame)
		{
			FrameMessage dropped = null;
			lock (_frameGate)
			{
				dropped = _pendingFrame;
				_pendingFrame = frame;
			}

			// ACK dropped frames immediately so the host is not stalled waiting on them.
			if (dropped != null)
			{
				SendAsync(new FrameReceivedMessage { SequenceId = dropped.SequenceId }).FireAndForget();
			}

			ProcessPendingFrameAsync().FireAndForget();
			return;
		}

		ProcessNonFrameMessageAsync(message).FireAndForget();
	}

	void ILogEventEnricher.Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		try
		{
			if (_process?.HasExited != true)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Pid", _process?.Id ?? 0));
			}
		}
		catch (InvalidOperationException)
		{
			// Process handle may be invalid during teardown.
		}
	}

	private static void EnsureExists(string path)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Could not find '{path}'.");
		}
	}

	private static bool Equals(ExceptionDetails a, ExceptionDetails b)
	{
		if (ReferenceEquals(a, b))
		{
			return true;
		}

		return (a?.ExceptionType == b?.ExceptionType) &&
			(a?.Message == b?.Message) &&
			(a?.LineNumber == b?.LineNumber) &&
			(a?.LinePosition == b?.LinePosition);
	}

	private static int FreeTcpPort()
	{
		var l = new TcpListener(IPAddress.Loopback, 0);
		l.Start();
		var port = ((IPEndPoint) l.LocalEndpoint).Port;
		l.Stop();
		return port;
	}

	private void LogIncomingMessage(object message)
	{
		switch (message)
		{
			case FrameMessage frame:
				// Never destructure FrameMessage — frame.Data is the full pixel buffer.
				_log.Debug(
					"<= FrameMessage SequenceId={SequenceId} {Width}x{Height} Stride={Stride} Format={Format}",
					frame.SequenceId,
					frame.Width,
					frame.Height,
					frame.Stride,
					frame.Format);
				break;
			default:
				_log.Debug("<= {@Message}", message);
				break;
		}
	}

	private async Task OnFrameAsync(FrameMessage frame)
	{
		_log.Verbose("Started PreviewerProcess.OnFrameAsync()");
		LogIncomingMessage(frame);

		// While markup is invalid, freeze the last good frame: ACK the host so it does not
		// stall, but do not WritePixels or notify the UI (avoids 1x1 / blank thrash).
		if (Error != null)
		{
			await SendAsync(new FrameReceivedMessage
			{
				SequenceId = frame.SequenceId
			});
			_log.Verbose("Finished PreviewerProcess.OnFrameAsync() (frozen — invalid markup)");
			return;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// Ignore degenerate frames (host sometimes sends 1x1 after a failed load race).
		if ((frame.Width <= 1) && (frame.Height <= 1) && (_bitmap != null))
		{
			await SendAsync(new FrameReceivedMessage
			{
				SequenceId = frame.SequenceId
			});
			_log.Verbose("Finished PreviewerProcess.OnFrameAsync() (ignored degenerate frame)");
			return;
		}

		if ((_bitmap == null) || (_bitmap.PixelWidth != frame.Width) || (_bitmap.PixelHeight != frame.Height))
		{
			_bitmap = new WriteableBitmap(
				Math.Max(frame.Width, 1),
				Math.Max(frame.Height, 1),
				96,
				96,
				ToWpf(frame.Format),
				null);
		}

		if ((frame.Width > 0) && (frame.Height > 0))
		{
			_bitmap.WritePixels(
				new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight),
				frame.Data,
				frame.Stride,
				0);
		}

		// Always ACK so the host can continue. Throttle only the UI notification path.
		var now = DateTime.UtcNow;
		var notifyUi = (now - _lastUiFrameUtc) >= MinUiFrameInterval;
		if (notifyUi)
		{
			_lastUiFrameUtc = now;
			FrameReceived?.Invoke(this, EventArgs.Empty);
		}

		await SendAsync(new FrameReceivedMessage
		{
			SequenceId = frame.SequenceId
		});

		_log.Verbose("Finished PreviewerProcess.OnFrameAsync()");
	}

	private async Task OnNonFrameMessageAsync(object message)
	{
		_log.Verbose("Started PreviewerProcess.OnNonFrameMessageAsync()");
		LogIncomingMessage(message);

		if (message is UpdateXamlResultMessage update)
		{
			var exception = update.Exception;

			if ((exception == null) && !string.IsNullOrWhiteSpace(update.Error))
			{
				exception = new ExceptionDetails { Message = update.Error };
			}

			var hadError = Error != null;
			Error = exception;

			if (exception != null)
			{
				_log.Information(
					"Preview paused on invalid markup (line {Line}, col {Col}): {Message}",
					exception.LineNumber,
					exception.LinePosition,
					exception.Message);
				_log.Error(new XamlException(exception.Message, null, exception.LineNumber ?? 0, exception.LinePosition ?? 0), "UpdateXamlResult error");
				if (!string.IsNullOrWhiteSpace(update.Error))
				{
					_log.Error("UpdateXamlResult error details: {0}", update.Error);
				}
			}
			else if (hadError)
			{
				_log.Information("Preview resumed — markup is valid again");
			}
		}

		_log.Verbose("Finished PreviewerProcess.OnNonFrameMessageAsync()");
	}

	private void OnProcessErrorReceived(object sender, DataReceivedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(e.Data))
		{
			_log.Error("<= {Data}", e.Data);
		}
	}

	private void OnProcessExited(object sender, EventArgs e)
	{
		_log.Information("Process exited");
		Stop();
		CleanupProcessState();
		ProcessExited?.Invoke(this, EventArgs.Empty);
	}

	private void OnProcessOutputReceived(object sender, DataReceivedEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(e.Data))
		{
			_log.Debug("<= {Data}", e.Data);
		}
	}

	private async Task ProcessNonFrameMessageAsync(object message)
	{
		await _messageGate.WaitAsync().ConfigureAwait(false);
		try
		{
			if (_connection == null)
			{
				return;
			}

			await OnNonFrameMessageAsync(message);
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Error processing previewer message");
		}
		finally
		{
			_messageGate.Release();
		}
	}

	private async Task ProcessPendingFrameAsync()
	{
		// Only one frame processor at a time; loop so the latest pending frame is not skipped.
		if (!await _messageGate.WaitAsync(0).ConfigureAwait(false))
		{
			return;
		}

		try
		{
			while (true)
			{
				FrameMessage frame;
				lock (_frameGate)
				{
					frame = _pendingFrame;
					_pendingFrame = null;
				}

				if (frame == null || _connection == null)
				{
					break;
				}

				await OnFrameAsync(frame);
			}
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Error processing previewer frame");
		}
		finally
		{
			_messageGate.Release();
		}

		// A frame may have been queued after we released the last frame but before Release.
		lock (_frameGate)
		{
			if (_pendingFrame != null)
			{
				ProcessPendingFrameAsync().FireAndForget();
			}
		}
	}

	private async Task SendAsync(object message)
	{
		if (message is FrameReceivedMessage ack)
		{
			_log.Debug("=> FrameReceivedMessage SequenceId={SequenceId}", ack.SequenceId);
		}
		else if (message is ClientRenderInfoMessage renderInfo)
		{
			_log.Debug("=> ClientRenderInfoMessage DpiX={DpiX} DpiY={DpiY}", renderInfo.DpiX, renderInfo.DpiY);
		}
		else if (message is UpdateXamlMessage)
		{
			_log.Debug("=> UpdateXamlMessage (xaml omitted)");
		}
		else
		{
			_log.Debug("=> Sending {@Message}", message);
		}

		if (_connection is IAvaloniaRemoteTransportConnection connection)
		{
			await connection.Send(message);
		}
	}

	private Task SendRenderInfoAsync()
	{
		var scaling = Scaling > 0 ? Scaling : 1;
		return SendAsync(new ClientRenderInfoMessage
		{
			DpiX = 96 * scaling,
			DpiY = 96 * scaling
		});
	}

	private PixelFormat ToWpf(Avalonia.Remote.Protocol.Viewport.PixelFormat format)
	{
		switch (format)
		{
			case Avalonia.Remote.Protocol.Viewport.PixelFormat.Bgra8888:
				return PixelFormats.Bgra32;
			case Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgb565:
				return PixelFormats.Bgr565;
			case Avalonia.Remote.Protocol.Viewport.PixelFormat.Rgba8888:
				return PixelFormats.Pbgra32;
			default:
				throw new NotSupportedException("Unsupported pixel format.");
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when the <see cref="Error" /> state changes.
	/// </summary>
	public event EventHandler ErrorChanged;

	/// <summary>
	/// Raised when a new frame is available in <see cref="Bitmap" />.
	/// </summary>
	public event EventHandler FrameReceived;

	/// <summary>
	/// Raised when the underlying system process exits.
	/// </summary>
	public event EventHandler ProcessExited;

	#endregion
}