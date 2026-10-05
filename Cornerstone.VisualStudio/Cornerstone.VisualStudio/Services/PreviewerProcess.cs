#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xaml;
using Cornerstone.Presentation.Remote.Wpf;
using Cornerstone.VisualStudio.Avalonia;
using Cornerstone.VisualStudio.Services.Preview;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
	/// Host must complete BSON handshake within this window or start is aborted.
	/// Unbounded wait here can freeze Visual Studio when the host dies quietly.
	/// </summary>
	private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(15);

	/// <summary>
	/// A send that stays in flight this long means the socket is wedged; stop the host.
	/// Detected by the watchdog so pointer/frame ACKs do not each allocate a Delay timer.
	/// </summary>
	private static readonly TimeSpan TransportSendTimeout = TimeSpan.FromSeconds(5);

	private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(2);

	private string _assemblyPath;
	private WriteableBitmap _bitmap;
	private object _connection;
	private PreviewExceptionDetails _error;
	private string _executablePath;
	private readonly IPreviewProtocol _protocol;
	private Dispatcher _uiDispatcher;
	private IDisposable _listener;
	private readonly ILogger _log;
	private readonly SemaphoreSlim _messageGate = new(1, 1);
	private PreviewFrameData _pendingFrame;
	private Process _process;
	private TaskCompletionSource<object> _remoteConnected;
	private readonly RemoteSession _session;
	private int _stopping;
	private int _stopWaitsForExit;
	private int _stoppedByUser;
	private int _exitHandled;
	private int _framePainted;
	private int _lastFrameWidth;
	private int _lastFrameHeight;
	private readonly object _lifetimeGate;
	private int _runId;
	private int _disposedProcess;
	private Timer _watchdog;
	private CancellationTokenSource _runCts;
	private int _sendInFlight;
	private int _surfaceSuspended;
	private long _sendStartedUtcTicks;
	private readonly object _frameGate = new();
	private static readonly List<PreviewerProcess> _live = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PreviewerProcess" /> class.
	/// </summary>
	public PreviewerProcess()
		: this(CornerstonePreviewProtocol.Instance)
	{
	}

	internal PreviewerProcess(IPreviewProtocol protocol)
	{
		_protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
		_log = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Destructure.ToMaximumStringLength(32)
			.Enrich.With(this)
			.WriteTo.Logger(Log.Logger)
			.CreateLogger();

		Scaling = 1;
		_lifetimeGate = new object();
		_runId = 0;
		_stopWaitsForExit = 0;
		_disposedProcess = 0;
		_runCts = new CancellationTokenSource();
		_sendInFlight = 0;
		_surfaceSuspended = 0;
		_sendStartedUtcTicks = 0;
		_uiDispatcher = null;
		_session = protocol.Platform == XamlPreviewPlatform.Cornerstone
			? new RemoteSession()
			: null;
		if (_session != null)
		{
			_session.FramePainted += OnRemoteFramePainted;
			_session.FrameReceived += OnRemoteFrameReceived;
			_session.FrameIgnored += OnRemoteFrameIgnored;
			_session.MessageReceived += OnRemoteMessageReceived;
			_session.Connected += OnRemoteConnected;
			_session.Faulted += OnRemoteFaulted;
		}

		lock (_live)
		{
			_live.Add(this);
		}

		_framePainted = 0;
		_lastFrameWidth = 0;
		_lastFrameHeight = 0;
		Status = "Idle";
		Activity = "Idle";
	}

	public static PreviewerProcess Create(XamlPreviewPlatform platform)
	{
		IPreviewProtocol protocol = platform == XamlPreviewPlatform.Cornerstone
			? CornerstonePreviewProtocol.Instance
			: AvaloniaPreviewProtocol.Instance;
		return new PreviewerProcess(protocol);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the current preview as a <see cref="BitmapSource" />.
	/// </summary>
	public BitmapSource Bitmap => _session != null ? _session.Bitmap : _bitmap;

	/// <summary>
	/// Gets the bitmap that should be shown in the designer. While markup is invalid the
	/// last good frame is kept so the preview freezes instead of blanking or thrashing.
	/// </summary>
	public BitmapSource DisplayBitmap => Bitmap;

	/// <summary>
	/// Short state for the process panel: Awaiting build, Starting, Updating, Showing, Paused.
	/// Updating means XAML was sent and no frame has been painted yet. Showing means a frame was painted.
	/// </summary>
	public string Status { get; private set; }

	/// <summary>
	/// What the preview is doing right now, shown in the processes window.
	/// </summary>
	public string Activity { get; private set; }

	/// <summary>
	/// Forced preview theme sent with the next XAML update: Default, Light, or Dark.
	/// </summary>
	public string PreviewTheme { get; set; }

	/// <summary>
	/// Preview accent sent with the next XAML update, such as Blue.
	/// </summary>
	public string PreviewThemeColor { get; set; }

	/// <summary>
	/// Preview density sent with the next XAML update: Compact, Normal, or Large.
	/// </summary>
	public string PreviewThemeDensity { get; set; }

	/// <summary>
	/// File name of the designer tab, such as About.cxaml.
	/// </summary>
	public string Document { get; private set; }

	/// <summary>
	/// Gets the current error state as returned from the previewer process.
	/// </summary>
	public PreviewExceptionDetails Error
	{
		get => _error;
		private set
		{
			if (!Equals(_error, value))
			{
				_error = value;
				if (_session != null)
				{
					_session.PauseFrames = value != null;
				}

				if ((value != null) && (Status != "Awaiting build"))
				{
					Status = "Paused";
					Activity = "Paused";
				}
				else if ((value == null) && (Status == "Paused"))
				{
					Status = "Showing";
				}

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
	public bool IsReady =>
		IsRunning && (_session != null ? _session.IsConnected : _connection != null);

	internal RemoteSession RemoteSession => _session;

	/// <summary>
	/// Gets a value indicating whether the previewer process is currently running.
	/// </summary>
	internal static PreviewerProcess[] Live()
	{
		lock (_live)
		{
			return _live.ToArray();
		}
	}

	/// <summary>
	/// Stops every previewer so those processes release files when Visual Studio closes.
	/// </summary>
	internal static void ShutdownAll()
	{
		var live = Live();
		for (var i = 0; i < live.Length; i++)
		{
			try
			{
				live[i].Stop();
			}
			catch (Exception ex)
			{
				Log.Debug(ex, "Previewer shutdown failed");
			}
		}
	}

	internal string TargetPath => _executablePath;

	internal int ProcessId
	{
		get
		{
			try
			{
				var process = _process;
				if ((process == null) || process.HasExited)
				{
					return 0;
				}

				return process.Id;
			}
			catch (InvalidOperationException)
			{
				return 0;
			}
		}
	}

	public bool IsRunning
	{
		get
		{
			try
			{
				var process = _process;
				if (process == null)
				{
					return false;
				}

				return !process.HasExited;
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
	public XamlPreviewPlatform Platform => _protocol.Platform;

	public double Scaling { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Stops the process and disposes of all resources.
	/// </summary>
	public void Dispose()
	{
		Interlocked.Exchange(ref _disposedProcess, 1);
		lock (_live)
		{
			_live.Remove(this);
		}

		StopWatchdog();
		Stop();
		CancelRun();
		if (_session != null)
		{
			_session.FramePainted -= OnRemoteFramePainted;
			_session.FrameReceived -= OnRemoteFrameReceived;
			_session.FrameIgnored -= OnRemoteFrameIgnored;
			_session.MessageReceived -= OnRemoteMessageReceived;
			_session.Connected -= OnRemoteConnected;
			_session.Faulted -= OnRemoteFaulted;
			_session.Dispose();
		}

		_messageGate.Dispose();
		try
		{
			_runCts?.Dispose();
		}
		catch
		{
			// Already disposed during Stop.
		}
	}

	/// <summary>
	/// Sends an input message to the process.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <returns> A task tracking the operation. </returns>
	public Task SendPointerMovedAsync(double x, double y, MouseEventArgs e)
	{
		return SendInputObjectAsync(_protocol.CreatePointerMoved(x, y, e));
	}

	public Task SendPointerPressedAsync(double x, double y, MouseButtonEventArgs e)
	{
		return SendInputObjectAsync(_protocol.CreatePointerPressed(x, y, e));
	}

	public Task SendPointerReleasedAsync(double x, double y, MouseButtonEventArgs e)
	{
		return SendInputObjectAsync(_protocol.CreatePointerReleased(x, y, e));
	}

	private Task SendInputObjectAsync(object message)
	{
		if (!IsReady)
		{
			return Task.CompletedTask;
		}

		return SendAsync(message);
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

		if (_session != null)
		{
			await _session.SetScalingAsync(scaling).ConfigureAwait(false);
			return;
		}

		if (IsReady)
		{
			await SendRenderInfoAsync().ConfigureAwait(false);
		}
	}

	internal void SetDocument(string document)
	{
		Document = document ?? string.Empty;
	}

	internal void SetStatus(string status)
	{
		Status = status ?? string.Empty;
		if (string.Equals(Status, "Awaiting build", StringComparison.Ordinal))
		{
			Activity = "Waiting on build";
			return;
		}

		if (string.Equals(Status, "Paused", StringComparison.Ordinal))
		{
			Activity = "Paused";
		}
	}

	/// <summary>
	/// Binds remote frame application to the designer dispatcher. Call from the UI thread
	/// before <see cref="StartAsync"/>. Frames post a single pass when they arrive.
	/// <see cref="Stop"/> drops any wait, so a suspended host does not keep waking the UI.
	/// </summary>
	public void AttachUiDispatcher(Dispatcher dispatcher)
	{
		_uiDispatcher = dispatcher;
		_session?.AttachDispatcher(dispatcher);
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
		if (Volatile.Read(ref _disposedProcess) != 0)
		{
			throw new OperationCanceledException("Previewer start was cancelled.");
		}

		Interlocked.Exchange(ref _stoppedByUser, 0);
		Status = "Starting";
		Activity = "Starting";

		if ((_listener != null) || (_session != null && _session.IsListening) || IsRunning)
		{
			Stop();
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
		int run;
		if (!TryBeginRun(out run))
		{
			throw new OperationCanceledException("Previewer start was cancelled.");
		}

		ReplaceRunCancellation();
		Error = null;

		var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		int port;
		if (_session != null)
		{
			var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
			if (dispatcher != null)
			{
				_session.AttachDispatcher(dispatcher);
			}

			_remoteConnected = tcs;
			port = _session.ListenLoopback();
		}
		else
		{
			port = FreeTcpPort();
			_listener = _protocol.Listen(
				IPAddress.Loopback,
				port,
				#pragma warning disable VSTHRD101
				t =>
				{
					ConnectionInitializedAsync(t).ContinueWith(task =>
					{
						if (task.IsFaulted)
						{
							_log.Error(task.Exception, "Error initializing connection");
							tcs.TrySetException(task.Exception.GetBaseException());
						}
						else
						{
							tcs.TrySetResult(null);
						}
					}, TaskScheduler.Default);
				});
			#pragma warning restore VSTHRD101
		}

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
			var hostDir = Path.GetDirectoryName(hostAppPath);
			var probeArgs = "";
			if (!string.IsNullOrEmpty(hostDir))
			{
				// The previewed executable often does not reference Presentation. Its deps
				// file then has no entry for the host's assemblies, and Main dies with
				// FileNotFound before the handshake. Probe the host output and the NuGet
				// cache, and add the host graph with project dlls marked as flat packages.
				probeArgs = $@" --additionalprobingpath ""{hostDir}""";
				var nuget = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
				if (string.IsNullOrWhiteSpace(nuget))
				{
					nuget = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						".nuget",
						"packages");
				}

				if (Directory.Exists(nuget))
				{
					probeArgs += $@" --additionalprobingpath ""{nuget}""";
				}

				var hostDeps = TryCreateHostDepsFile(hostAppPath);
				if (hostDeps != null)
				{
					probeArgs += $@" --additional-deps ""{hostDeps}""";
				}
			}

			args = $@"exec{probeArgs} --runtimeconfig ""{runtimeConfigPath}"" --depsfile ""{depsPath}"" ""{hostAppPath}"" --transport tcp-bson://127.0.0.1:{port}/ ""{_executablePath}""";
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

		_log.Debug("Starting previewer process for '{ExecutablePath}'", _executablePath);
		_log.Debug("> dotnet.exe {Args}", args);

		// Stop during startup increments the run id. A process launched after that
		// used to be stored anyway, so closing the tab left it alive until devenv exited.
		if (!IsCurrentRun(run))
		{
			AbandonLaunch(null);
			throw new OperationCanceledException("Previewer start was cancelled.");
		}

		var process = Process.Start(processInfo);
		if (process == null)
		{
			AbandonLaunch(null);
			throw new InvalidOperationException("Failed to start the previewer process.");
		}

		ExtensionProcessLifetime.Track(process);
		if (!TryPublishProcess(run, process))
		{
			_log.Debug("Previewer launch lost the race with Stop; killing pid {Pid}", process.Id);
			AbandonLaunch(process);
			throw new OperationCanceledException("Previewer start was cancelled.");
		}
		Interlocked.Exchange(ref _exitHandled, 0);
		var abortOnce = 0;
		process.EnableRaisingEvents = true;
		process.OutputDataReceived += OnProcessOutputReceived;
		process.ErrorDataReceived += OnProcessErrorReceived;
		process.Exited += Abort;
		process.Exited += OnProcessExited;
		try
		{
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
		}
		catch (InvalidOperationException)
		{
			// The process can exit inside Main before the readers are attached.
		}

		try
		{
			if (process.HasExited)
			{
				Abort(process, EventArgs.Empty);
			}
		}
		catch (InvalidOperationException)
		{
			Abort(process, EventArgs.Empty);
		}

		void Abort(object sender, EventArgs e)
		{
			if (Interlocked.Exchange(ref abortOnce, 1) == 1)
			{
				return;
			}

			var code = -1;
			try
			{
				code = process.ExitCode;
			}
			catch (InvalidOperationException)
			{
				// Stop disposed the process, or the handle was already released.
			}

			_log.Information("Process exited while waiting for connection to be initialized.");
			tcs.TrySetException(new ApplicationException(
				"The previewer process exited unexpectedly with code " + code + "."));
		}

		StartWatchdog();

		try
		{
			_log.Debug("Started previewer process for '{ExecutablePath}'. Waiting for connection to be initialized.", _executablePath);
			using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(RunToken))
			{
				timeoutCts.CancelAfter(HandshakeTimeout);
				var timeoutTask = Task.Delay(Timeout.Infinite, timeoutCts.Token);
				var completed = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
				if (completed != tcs.Task)
				{
					if (!tcs.Task.IsCompleted)
					{
						_log.Warning("Previewer handshake timed out after {Timeout}", HandshakeTimeout);
						tcs.TrySetException(new TimeoutException(
							"The previewer did not connect within " + HandshakeTimeout.TotalSeconds + " seconds."));
						try
						{
							Stop();
						}
						catch (Exception ex)
						{
							_log.Debug(ex, "Stop after handshake timeout");
						}
					}
				}
			}

			await tcs.Task.ConfigureAwait(false);
		}
		finally
		{
			try
			{
				process.Exited -= Abort;
			}
			catch (InvalidOperationException)
			{
				// The process was disposed after it exited.
			}
		}

		_log.Verbose("Finished PreviewerProcess.StartAsync()");
	}

	/// <summary>
	/// Unhooks the preview render pump on the calling thread when that thread is the
	/// UI thread. Does not kill the process or close the socket.
	/// The Cornerstone pump stays stopped until the next listen.
	/// </summary>
	public void ReleasePreviewSurface()
	{
		Interlocked.Exchange(ref _surfaceSuspended, 1);
		_session?.RequestStopPump();
	}

	/// <summary>
	/// Drops the render hook while the host is still running. A hidden tab or a
	/// source-only view uses this so frames are acknowledged and not applied.
	/// </summary>
	public void SuspendPreviewSurface()
	{
		Interlocked.Exchange(ref _surfaceSuspended, 1);
		_session?.SuspendPump();
	}

	/// <summary>
	/// Presents frames again after <see cref="SuspendPreviewSurface"/>.
	/// </summary>
	public void ResumePreviewSurface()
	{
		Interlocked.Exchange(ref _surfaceSuspended, 0);
		_session?.ResumePump();
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
	/// Stops the preview from the process list. The designer leaves it stopped
	/// instead of showing a crash banner.
	/// </summary>
	public void Kill()
	{
		Interlocked.Exchange(ref _stoppedByUser, 1);
		Stop();
	}

	public bool StoppedByUser => _stoppedByUser == 1;

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
		Process process;
		var alreadyStopping = false;
		lock (_lifetimeGate)
		{
			// Invalidates an in-flight StartAsync. That start kills the process it
			// launched instead of keeping an orphan until Visual Studio exits.
			_runId++;
			alreadyStopping = _stopping == 1;
			_stopping = 1;
			process = _process;
		}

		if (alreadyStopping)
		{
			// Another stop is already in flight; still try to abort a wedged write if the host is alive.
			TryKillProcess(process);
			if (waitForExit)
			{
				WaitForProcessExit(process, timeout);
			}

			return;
		}

		_log.Verbose("Started PreviewerProcess.Stop(waitForExit={Wait})", waitForExit);
		_log.Debug("Stopping previewer process");
		StopWatchdog();
		CancelRun();

		_listener?.Dispose();
		_listener = null;
		_session?.Stop();
		var pendingConnect = Interlocked.Exchange(ref _remoteConnected, null);
		pendingConnect?.TrySetException(new ApplicationException("The previewer was stopped."));

		var connection = _connection;
		if (connection != null)
		{
			_connection = null;
			_protocol.Unsubscribe(connection, ConnectionMessageReceived, ConnectionExceptionReceived);

			try
			{
				_protocol.DisposeConnection(connection);
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

		if (process != null)
		{
			if (waitForExit)
			{
				// OnProcessExited disposes the Process. WaitForExit throws
				// "No process is associated" if that happens while we are in it.
				Interlocked.Increment(ref _stopWaitsForExit);
			}

			try
			{
				TryKillProcess(process);

				if (waitForExit)
				{
					WaitForProcessExit(process, timeout);
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
			finally
			{
				if (waitForExit)
				{
					Interlocked.Decrement(ref _stopWaitsForExit);
				}
			}
		}

		_executablePath = null;
		_assemblyPath = null;

		_log.Verbose("Finished PreviewerProcess.Stop()");
	}

	private void TryKillProcess(Process process)
	{
		if (process == null)
		{
			return;
		}

		_log.Debug("Killing previewer process");
		ExtensionProcessLifetime.Kill(process);
	}

	/// <summary>
	/// Best-effort wait until <see cref="_process"/> is null or has exited (for concurrent Stop).
	/// </summary>
	private void WaitForProcessExit(Process process, TimeSpan timeout)
	{
		if ((process == null) || HasProcessExited(process))
		{
			return;
		}

		var ms = timeout <= TimeSpan.Zero
			? 5000
			: (int) Math.Min(timeout.TotalMilliseconds, int.MaxValue);
		try
		{
			process.WaitForExit(ms);
		}
		catch (InvalidOperationException)
		{
			// The handle was released because the process had already exited.
		}
	}

	private static bool HasProcessExited(Process process)
	{
		try
		{
			return process.HasExited;
		}
		catch (InvalidOperationException)
		{
			return true;
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
		if (!IsReady)
		{
			_log.Information("Preview XAML was not sent. The host is not ready.");
			return false;
		}

		var refreshedAt = DateTime.Now.ToString("h:mm:ss tt");
		Interlocked.Exchange(ref _framePainted, 0);
		Status = "Updating";
		Activity = "Sent " + refreshedAt;
		_log.Information("Preview XAML sent at {Time}. Waiting for the host.", refreshedAt);
		try
		{
			await SendAsync(_protocol.CreateUpdateXaml(xaml, _assemblyPath, PreviewTheme, PreviewThemeColor, PreviewThemeDensity)).ConfigureAwait(false);
			// Showing means a frame was painted. Sending the XAML is not that.
			if ((_error == null) && (Volatile.Read(ref _framePainted) == 0))
			{
				Status = "Updating";
				Activity = "Sent " + refreshedAt;
			}

			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Keep the designer alive on transport glitches — surface as markup pause
			// instead of letting an unhandled fault take down the session.
			_log.Error(ex, "Failed to send UpdateXamlMessage");
			Status = "Paused";
			Error = new PreviewExceptionDetails
			{
				Message = "Failed to update preview: " + ex.Message
			};
			return false;
		}
	}

	private void CleanupProcessState()
	{
		Process process;
		lock (_lifetimeGate)
		{
			process = _process;
			_process = null;
		}

		if (process == null)
		{
			return;
		}

		ExtensionProcessLifetime.Kill(process);

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
		catch (InvalidOperationException)
		{
			// Already disposed, or no process is associated.
		}
	}

	private void ConnectionExceptionReceived(object connection, Exception ex)
	{
		// Tab close / Stop() disposes the BSON socket (or kills the host). Avalonia's
		// reader is still in EndReceive, so a reset looks like a connection error.
		if (IsExpectedTransportShutdown(connection, ex))
		{
			_log.Debug(ex, "Previewer transport closed during stop");
			return;
		}

		_log.Error(ex, "Connection error");
	}

	private bool IsExpectedTransportShutdown(object connection, Exception ex)
	{
		if ((Volatile.Read(ref _stopping) == 0) && (connection == _connection))
		{
			return false;
		}

		return IsTransportReset(ex);
	}

	private static bool IsTransportReset(Exception ex)
	{
		for (var current = ex; current != null; current = current.InnerException)
		{
			if (current is ObjectDisposedException)
			{
				return true;
			}

			if (current is SocketException)
			{
				return true;
			}

			if (current is IOException)
			{
				return true;
			}
		}

		return false;
	}

	private async Task ConnectionInitializedAsync(object connection)
	{
		_log.Verbose("Started PreviewerProcess.ConnectionInitializedAsync()");
		_log.Debug("Connection initialized");

		if (!IsRunning)
		{
			_log.Verbose("ConnectionInitializedAsync detected process has stopped: aborting");
			return;
		}

		_connection = connection;
		_protocol.Subscribe(_connection, ConnectionMessageReceived, ConnectionExceptionReceived);

		await SendAsync(_protocol.CreatePixelFormats()).ConfigureAwait(false);

		// Always send render info after connect. SetScalingAsync alone is a no-op when
		// Scaling was already set before the connection became ready.
		if (Scaling <= 0)
		{
			Scaling = 1;
		}

		await SendRenderInfoAsync().ConfigureAwait(false);

		_log.Verbose("Finished PreviewerProcess.ConnectionInitializedAsync()");
	}

	private void ConnectionMessageReceived(object connection, object message)
	{
		// Coalesce frames: only the latest pending frame is kept so a slow UI thread
		// cannot build an unbounded backlog of pixel buffers.
		if (_protocol.TryGetFrame(message, out var frame))
		{
			PreviewFrameData dropped = null;
			lock (_frameGate)
			{
				dropped = _pendingFrame;
				_pendingFrame = frame;
			}

			// ACK dropped frames immediately so the host is not stalled waiting on them.
			if (dropped != null)
			{
				SendAsync(_protocol.CreateFrameAck(dropped.SequenceId)).FireAndForget();
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

	/// <summary>
	/// Writes a deps file for the designer host with the host assembly removed and
	/// project outputs marked so they load from the host directory.
	/// </summary>
	private static string TryCreateHostDepsFile(string hostAppPath)
	{
		var hostDir = Path.GetDirectoryName(hostAppPath);
		if (string.IsNullOrEmpty(hostDir))
		{
			return null;
		}

		var sourcePath = Path.Combine(hostDir, Path.GetFileNameWithoutExtension(hostAppPath) + ".deps.json");
		if (!File.Exists(sourcePath))
		{
			return null;
		}

		var sourceTime = File.GetLastWriteTimeUtc(sourcePath);
		var cacheDir = Path.Combine(Path.GetTempPath(), "CornerstoneDesigner");
		Directory.CreateDirectory(cacheDir);
		var cachePath = Path.Combine(cacheDir, "host-" + sourceTime.Ticks + ".deps.json");
		if (File.Exists(cachePath))
		{
			return cachePath;
		}

		var root = JObject.Parse(File.ReadAllText(sourcePath));
		var hostName = Path.GetFileNameWithoutExtension(hostAppPath) + "/";
		var targets = root["targets"] as JObject;
		if (targets != null)
		{
			foreach (var tfm in targets.Properties())
			{
				var graph = tfm.Value as JObject;
				if (graph == null)
				{
					continue;
				}

				var remove = new List<string>();
				foreach (var entry in graph.Properties())
				{
					if (entry.Name.StartsWith(hostName, StringComparison.Ordinal))
					{
						remove.Add(entry.Name);
					}
				}

				foreach (var name in remove)
				{
					graph.Remove(name);
				}
			}
		}

		var libraries = root["libraries"] as JObject;
		if (libraries != null)
		{
			var removeLibraries = new List<string>();
			foreach (var entry in libraries.Properties())
			{
				if (entry.Name.StartsWith(hostName, StringComparison.Ordinal))
				{
					removeLibraries.Add(entry.Name);
				}
			}

			foreach (var name in removeLibraries)
			{
				libraries.Remove(name);
			}

			foreach (var entry in libraries.Properties())
			{
				var library = entry.Value as JObject;
				if ((library == null) || ((string) library["type"] != "project"))
				{
					continue;
				}

				// Project assets are only loaded from the app directory. A package
				// with path "." is probed in --additionalprobingpath, which is the host output.
				library["type"] = "package";
				library["path"] = ".";
				library["serviceable"] = true;
			}
		}

		File.WriteAllText(cachePath, root.ToString(Formatting.None));
		return cachePath;
	}

	private static void EnsureExists(string path)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Could not find '{path}'.");
		}
	}

	private static bool Equals(PreviewExceptionDetails a, PreviewExceptionDetails b)
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
			case var _ when _protocol.TryGetFrame(message, out var frame):
				_log.Verbose(
					"<= FrameMessage SequenceId={SequenceId} {Width}x{Height} Stride={Stride} Format={Format}",
					frame.SequenceId,
					frame.Width,
					frame.Height,
					frame.Stride,
					frame.Format);
				break;
			default:
				_log.Verbose("<= {@Message}", message);
				break;
		}
	}

	private async Task OnFrameAsync(PreviewFrameData frame)
	{
		_log.Verbose("Started PreviewerProcess.OnFrameAsync()");
		LogIncomingMessage(frame);

		// ACK off the UI thread first so a stuck dispatcher cannot stall the host or
		// block devenv on a dead socket.
		await SendAsync(_protocol.CreateFrameAck(frame.SequenceId)).ConfigureAwait(false);

		if (Error != null)
		{
			_log.Verbose("Finished PreviewerProcess.OnFrameAsync() (frozen — invalid markup)");
			return;
		}

		if ((frame.Width <= 1) && (frame.Height <= 1) && (_bitmap != null))
		{
			NoteFrameIgnored(frame.Width, frame.Height);
			_log.Verbose("Finished PreviewerProcess.OnFrameAsync() (ignored degenerate frame)");
			return;
		}

		try
		{
			await ApplyFrameOnUiAsync(frame).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			_log.Verbose("Finished PreviewerProcess.OnFrameAsync() (cancelled)");
			return;
		}

		_log.Verbose("Finished PreviewerProcess.OnFrameAsync()");
	}

	private async Task ApplyFrameOnUiAsync(PreviewFrameData frame)
	{
		if (Volatile.Read(ref _surfaceSuspended) != 0)
		{
			return;
		}

		var dispatcher = _uiDispatcher;
		if ((dispatcher != null) && !dispatcher.CheckAccess())
		{
			// Background is below Input. SwitchToMainThreadAsync posts at Normal,
			// which sits above the keyboard and freezes the shell while frames arrive.
			#pragma warning disable VSTHRD001
			await dispatcher.InvokeAsync(
				() => ApplyFrameOnUi(frame),
				DispatcherPriority.Background,
				RunToken);
			#pragma warning restore VSTHRD001
			return;
		}

		if (dispatcher == null)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(RunToken);
		}

		ApplyFrameOnUi(frame);
	}

	private void ApplyFrameOnUi(PreviewFrameData frame)
	{
		if (Volatile.Read(ref _surfaceSuspended) != 0)
		{
			return;
		}

		var sizeChanged = (_bitmap == null) ||
			(_bitmap.PixelWidth != frame.Width) ||
			(_bitmap.PixelHeight != frame.Height);

		if (sizeChanged)
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
				new Int32Rect(0, 0, frame.Width, frame.Height),
				frame.Data,
				frame.Stride,
				0);
			NoteFramePainted(frame.Width, frame.Height);
		}

		if (sizeChanged)
		{
			FrameReceived?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task OnNonFrameMessageAsync(object message)
	{
		_log.Verbose("Started PreviewerProcess.OnNonFrameMessageAsync()");
		LogIncomingMessage(message);

		if (_protocol.TryGetXamlResult(message, out var update))
		{
			var exception = update.Exception;

			if ((exception == null) && !string.IsNullOrWhiteSpace(update.Error))
			{
				exception = new PreviewExceptionDetails { Message = update.Error };
			}

			var hadError = Error != null;
			Error = exception;

			if (exception != null)
			{
				_log.Debug(
					"Preview paused on invalid markup (line {Line}, col {Col}): {Message}",
					exception.LineNumber,
					exception.LinePosition,
					exception.Message);
				_log.Debug(new XamlException(exception.Message, null, exception.LineNumber ?? 0, exception.LinePosition ?? 0), "UpdateXamlResult error");
				if (!string.IsNullOrWhiteSpace(update.Error))
				{
					_log.Debug("UpdateXamlResult error details: {0}", update.Error);
				}
			}
			else
			{
				if (hadError)
				{
					_log.Debug("Preview resumed — markup is valid again");
				}

				if (Volatile.Read(ref _framePainted) == 0)
				{
					var acceptedAt = DateTime.Now.ToString("h:mm:ss tt");
					Status = "Updating";
					Activity = "Host accepted " + acceptedAt;
					_log.Information("Preview host accepted XAML at {Time}. No frame painted yet.", acceptedAt);
				}
				else
				{
					_log.Information("Preview host accepted XAML. A frame is already painted.");
				}
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
		HandleProcessExited(sender as Process);
	}

	private void HandleProcessExited(Process exited)
	{
		lock (_lifetimeGate)
		{
			// A replaced or not-yet-published host must not Stop the run that superseded it.
			if ((exited == null) || !ReferenceEquals(_process, exited))
			{
				return;
			}
		}

		if (Interlocked.Exchange(ref _exitHandled, 1) == 1)
		{
			return;
		}

		_log.Debug("Process exited");
		if (Status != "Awaiting build")
		{
			Status = "Closed";
			Activity = "Closed";
		}
		StopWatchdog();
		_remoteConnected?.TrySetException(
			new ApplicationException("The previewer process exited unexpectedly."));

		try
		{
			Stop();
		}
		catch (Exception ex)
		{
			_log.Debug(ex, "Stop after process exit");
		}

		if (Volatile.Read(ref _stopWaitsForExit) == 0)
		{
			try
			{
				CleanupProcessState();
			}
			catch (Exception ex)
			{
				_log.Debug(ex, "Cleanup after process exit");
			}
		}

		try
		{
			ProcessExited?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			_log.Debug(ex, "ProcessExited handler failed");
		}
	}

	private void StartWatchdog()
	{
		StopWatchdog();
		_watchdog = new Timer(OnWatchdog, null, WatchdogInterval, WatchdogInterval);
	}

	private void StopWatchdog()
	{
		var timer = Interlocked.Exchange(ref _watchdog, null);
		if (timer == null)
		{
			return;
		}

		try
		{
			timer.Dispose();
		}
		catch
		{
			// Ignore dispose races with the callback.
		}
	}

	private void OnWatchdog(object state)
	{
		var process = _process;
		if (process == null)
		{
			return;
		}

		try
		{
			if (!process.HasExited)
			{
				if (Volatile.Read(ref _sendInFlight) > 0)
				{
					var started = Interlocked.Read(ref _sendStartedUtcTicks);
					if ((started != 0) &&
						((DateTime.UtcNow.Ticks - started) > TransportSendTimeout.Ticks))
					{
						_log.Error("Previewer send hung; stopping host");
						try
						{
							Stop();
						}
						catch (Exception ex)
						{
							_log.Debug(ex, "Stop after hung send");
						}
					}
				}

				return;
			}
		}
		catch (InvalidOperationException)
		{
			// Handle is gone — treat as exited.
		}

		HandleProcessExited(process);
	}

	private void OnProcessOutputReceived(object sender, DataReceivedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.Data))
		{
			return;
		}

		if (e.Data.StartsWith("Preview host:", StringComparison.Ordinal))
		{
			_log.Information("{Data}", e.Data);
			return;
		}

		_log.Verbose("<= {Data}", e.Data);
	}

	private async Task ProcessNonFrameMessageAsync(object message)
	{
		await _messageGate.WaitAsync().ConfigureAwait(false);
		try
		{
			// The Cornerstone session never assigns _connection. Dropping here
			// discarded UpdateXamlResult for every preview.
			if ((_connection == null) && ((_session == null) || !_session.IsConnected))
			{
				_log.Information("Preview message {MessageType} dropped. No live connection.", message != null ? message.GetType().Name : "null");
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
				PreviewFrameData frame;
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
		_log.Verbose("=> Sending {MessageType}", message?.GetType().Name);

		// Arm the hung-send watchdog before Send so a blocking caller-thread write is still visible.
		var inFlight = Interlocked.Increment(ref _sendInFlight);
		if (inFlight == 1)
		{
			Interlocked.Exchange(ref _sendStartedUtcTicks, DateTime.UtcNow.Ticks);
		}

		try
		{
			Task send = null;
			if (_session != null)
			{
				send = _session.SendAsync(message);
			}
			else if (_connection != null)
			{
				send = _protocol.SendAsync(_connection, message);
			}

			if (send == null)
			{
				return;
			}

			await send.ConfigureAwait(false);
		}
		finally
		{
			Interlocked.Decrement(ref _sendInFlight);
		}
	}

	private CancellationToken RunToken
	{
		get
		{
			var cts = _runCts;
			return cts != null ? cts.Token : CancellationToken.None;
		}
	}

	private bool TryBeginRun(out int run)
	{
		lock (_lifetimeGate)
		{
			_runId++;
			run = _runId;
			if (_disposedProcess != 0)
			{
				return false;
			}

			_stopping = 0;
			return true;
		}
	}

	private bool IsCurrentRun(int run)
	{
		lock (_lifetimeGate)
		{
			return (_disposedProcess == 0) && (run == _runId);
		}
	}

	private bool TryPublishProcess(int run, Process process)
	{
		lock (_lifetimeGate)
		{
			if ((_disposedProcess != 0) || (run != _runId))
			{
				return false;
			}

			_process = process;
			return true;
		}
	}

	private void AbandonLaunch(Process process)
	{
		lock (_lifetimeGate)
		{
			if ((process != null) && ReferenceEquals(_process, process))
			{
				_process = null;
			}
		}

		try
		{
			_listener?.Dispose();
		}
		catch (Exception ex)
		{
			_log.Debug(ex, "Failed to dispose previewer listener");
		}

		_listener = null;
		try
		{
			_session?.Stop();
		}
		catch (Exception ex)
		{
			_log.Debug(ex, "Failed to stop previewer session");
		}

		var pendingConnect = Interlocked.Exchange(ref _remoteConnected, null);
		pendingConnect?.TrySetException(new OperationCanceledException("Previewer start was cancelled."));
		ExtensionProcessLifetime.Kill(process);
	}

	private void ReplaceRunCancellation()
	{
		var next = new CancellationTokenSource();
		var previous = Interlocked.Exchange(ref _runCts, next);
		if (previous == null)
		{
			return;
		}

		try
		{
			previous.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}

		try
		{
			previous.Dispose();
		}
		catch
		{
			// Ignore dispose races with in-flight hops.
		}
	}

	private void CancelRun()
	{
		try
		{
			_runCts?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
		catch (AggregateException)
		{
		}
	}

	private void OnRemoteConnected(object sender, EventArgs e)
	{
		_log.Debug("Remote session connected");
		_remoteConnected?.TrySetResult(null);
	}

	private void OnRemoteFaulted(object sender, Exception exception)
	{
		ConnectionExceptionReceived(_session, exception);
		var fault = exception ?? new IOException("Remote session faulted.");
		_remoteConnected?.TrySetException(fault);
	}

	private void OnRemoteFramePainted(object sender, EventArgs e)
	{
		var bitmap = _session != null ? _session.Bitmap : null;
		if (bitmap != null)
		{
			NoteFramePainted(bitmap.PixelWidth, bitmap.PixelHeight);
		}
	}

	private void OnRemoteFrameReceived(object sender, EventArgs e)
	{
		FrameReceived?.Invoke(this, EventArgs.Empty);
	}

	private void OnRemoteFrameIgnored(object sender, EventArgs e)
	{
		var session = _session;
		if (session == null)
		{
			return;
		}

		NoteFrameIgnored(session.IgnoredFrameWidth, session.IgnoredFrameHeight);
	}

	private void NoteFramePainted(int width, int height)
	{
		var first = Interlocked.Exchange(ref _framePainted, 1) == 0;
		var sizeChanged = (width != _lastFrameWidth) || (height != _lastFrameHeight);
		_lastFrameWidth = width;
		_lastFrameHeight = height;
		var paintedAt = DateTime.Now.ToString("h:mm:ss tt");
		Status = "Showing";
		Activity = "Frame " + width + "x" + height + " " + paintedAt;
		if (first || sizeChanged)
		{
			_log.Information("Preview frame painted {Width}x{Height} at {Time}.", width, height, paintedAt);
		}
	}

	private void NoteFrameIgnored(int width, int height)
	{
		var ignoredAt = DateTime.Now.ToString("h:mm:ss tt");
		if (Volatile.Read(ref _framePainted) == 0)
		{
			Status = "Updating";
		}

		Activity = "Ignored frame " + width + "x" + height + " " + ignoredAt;
		_log.Information(
			"Preview frame ignored ({Width}x{Height}) at {Time}. The surface stays empty until a real frame is painted.",
			width,
			height,
			ignoredAt);
	}

	private void OnRemoteMessageReceived(object sender, object message)
	{
		ProcessNonFrameMessageAsync(message).FireAndForget();
	}

	private Task SendRenderInfoAsync()
	{
		var scaling = Scaling > 0 ? Scaling : 1;
		return SendAsync(_protocol.CreateRenderInfo(96 * scaling, 96 * scaling));
	}

	private PixelFormat ToWpf(object format)
	{
		return _protocol.ToWpf(format);
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