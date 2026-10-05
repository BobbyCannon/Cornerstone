#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.AirPlay;
using Cornerstone.RemoteLink.AirPlay.Models.Mirroring;
using Cornerstone.RemoteLink.Android.Protocol;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.Android;

/// <summary>
/// Lists devices through adb.exe, injects keys/taps, and starts remotelink-server.jar
/// when the Gradle build is present (push, reverse, Hello handshake).
/// </summary>
[SourceReflection]
[DependencyInjected(TypeLifetime.Singleton)]
public sealed class AndroidMirrorService : IDisposable
{
	#region Constants

	public const string DefaultWindowsAdbPath = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe";
	public const string DeviceJarPath = "/data/local/tmp/remotelink-server.jar";
	public const string ServerJarFileName = "remotelink-server.jar";
	private const int MaxDiagnosticLines = 250;

	#endregion

	#region Fields

	private string _adbPath;
	private TcpClient _client;
	private int _encodeHeight;
	private int _encodeWidth;
	private AirPlayH264Decoder _decoder;
	private readonly List<string> _diagnosticLines = [];
	private readonly IDispatcher _dispatcher;
	private int _disposed;
	private bool _h264Live;
	private int _ignoredMessages;
	private int _logFrameBucket;
	private int _videoPackets;
	private TcpListener _listener;
	private byte[] _previewPixels;
	private string _pushedJarKey;
	private CancellationTokenSource _receiveToken;
	private Process _serverProcess;
	private readonly StringBuilder _serverLog = new();
	private readonly IDateTimeProvider _time;
	private int _uiQueued;

	#endregion

	#region Constructors

	public AndroidMirrorService(IDispatcher dispatcher)
		: this(dispatcher, DateTimeProvider.RealTime)
	{
	}

	[DependencyInjectionConstructor]
	public AndroidMirrorService(IDispatcher dispatcher, IDateTimeProvider time)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		_time = time ?? DateTimeProvider.RealTime;
		DiagnosticLog = "";
		StatusText = "Idle. Refresh to list USB or wireless-debug devices.";
	}

	#endregion

	#region Properties

	public event EventHandler Changed;

	public string DiagnosticLog { get; private set; }

	public bool HasLiveControl
	{
		get
		{
			var client = _client;
			return (client != null) && client.Connected;
		}
	}

	public bool IsConnected { get; private set; }

	public int PreviewHeight { get; private set; }

	public byte[] PreviewPixels { get; private set; }

	public int PreviewWidth { get; private set; }

	public int ScreenHeight { get; private set; }

	public int ScreenWidth { get; private set; }

	public string SelectedSerial { get; private set; }

	public string StatusText { get; private set; }

	#endregion

	#region Methods

	public async Task DisconnectAsync()
	{
		IsConnected = false;
		StopReceive();
		CloseSocket();
		KillServerProcess();
		_decoder?.Reset();
		_h264Live = false;
		_encodeWidth = 0;
		_encodeHeight = 0;
		PreviewPixels = null;
		PreviewWidth = 0;
		PreviewHeight = 0;
		var serial = SelectedSerial;
		var adb = _adbPath;
		SelectedSerial = null;
		ScreenWidth = 0;
		ScreenHeight = 0;
		_ = serial;
		_ = adb;
		StatusText = "Disconnected.";
		Log("Disconnected.");
		RaiseChanged();
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		DisconnectAsync().GetAwaiter().GetResult();
		_decoder?.Dispose();
		_decoder = null;
	}

	public string FindServerJar()
	{
		var baseDirectory = AppContext.BaseDirectory;
		var nextToApp = Path.Combine(baseDirectory, ServerJarFileName);
		if (File.Exists(nextToApp))
		{
			return nextToApp;
		}

		var repoJar = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "Cornerstone.RemoteLink.AndroidServer", "build", "libs", ServerJarFileName));
		if (File.Exists(repoJar))
		{
			return repoJar;
		}

		return null;
	}

	public async Task InjectKeyAsync(string adbPath, string serial, int keyCode, CancellationToken cancellationToken)
	{
		if (await TrySendControlAsync(RemoteLinkProtocol.ControlType.Key, keyCode, 0, cancellationToken).ConfigureAwait(false))
		{
			return;
		}

		var executable = ResolveAdbPath(adbPath);
		await RunAdbAsync(executable, $"-s {Quote(serial)} shell input keyevent {keyCode.ToString(CultureInfo.InvariantCulture)}", cancellationToken).ConfigureAwait(false);
	}

	public async Task InjectTextAsync(string adbPath, string serial, string text, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (await TrySendControlTextAsync(text, cancellationToken).ConfigureAwait(false))
		{
			return;
		}

		var executable = ResolveAdbPath(adbPath);
		var start = 0;
		for (var i = 0; i <= text.Length; i++)
		{
			var c = i < text.Length ? text[i] : '\n';
			if ((c != '\n') && (c != '\r'))
			{
				continue;
			}

			if (i > start)
			{
				var chunk = EscapeAdbInputText(text.Substring(start, i - start));
				await RunAdbAsync(executable, $"-s {Quote(serial)} shell input text {chunk}", cancellationToken).ConfigureAwait(false);
			}

			if (i < text.Length)
			{
				await RunAdbAsync(executable, $"-s {Quote(serial)} shell input keyevent 66", cancellationToken).ConfigureAwait(false);
			}

			start = i + 1;
			if ((c == '\r') && (start < text.Length) && (text[start] == '\n'))
			{
				start++;
				i++;
			}
		}
	}

	public async Task InjectSwipeAsync(string adbPath, string serial, int x1, int y1, int x2, int y2, int durationMs, CancellationToken cancellationToken)
	{
		if (await TrySendControlAsync(RemoteLinkProtocol.ControlType.TouchDown, x1, y1, cancellationToken).ConfigureAwait(false))
		{
			await TrySendControlAsync(RemoteLinkProtocol.ControlType.TouchMove, x2, y2, cancellationToken).ConfigureAwait(false);
			await TrySendControlAsync(RemoteLinkProtocol.ControlType.TouchUp, x2, y2, cancellationToken).ConfigureAwait(false);
			return;
		}

		var executable = ResolveAdbPath(adbPath);
		await RunAdbAsync(executable, $"-s {Quote(serial)} shell input swipe {x1} {y1} {x2} {y2} {durationMs}", cancellationToken).ConfigureAwait(false);
	}

	public async Task InjectTapAsync(string adbPath, string serial, int x, int y, CancellationToken cancellationToken)
	{
		if (await TrySendControlAsync(RemoteLinkProtocol.ControlType.TouchDown, x, y, cancellationToken).ConfigureAwait(false))
		{
			await TrySendControlAsync(RemoteLinkProtocol.ControlType.TouchUp, x, y, cancellationToken).ConfigureAwait(false);
			return;
		}

		var executable = ResolveAdbPath(adbPath);
		await RunAdbAsync(executable, $"-s {Quote(serial)} shell input tap {x} {y}", cancellationToken).ConfigureAwait(false);
	}

	public async Task<IReadOnlyList<AndroidDeviceItem>> RefreshDevicesAsync(string adbPath, CancellationToken cancellationToken)
	{
		var executable = ResolveAdbPath(adbPath);
		var output = await RunAdbAsync(executable, "devices -l", cancellationToken).ConfigureAwait(false);
		var devices = AndroidAdbText.ParseDevices(output);
		StatusText = devices.Count == 0
			? $"No devices from \"{executable}\". Enable USB debugging and use one ADB server (Visual Studio and scrcpy each ship adb.exe)."
			: $"Found {devices.Count} device(s). Protocol v{RemoteLinkProtocol.Version}.";
		return devices;
	}

	public string ResolveAdbPath(string adbPath)
	{
		if (!string.IsNullOrWhiteSpace(adbPath))
		{
			return adbPath.Trim();
		}

		return TryGetDefaultAdbPath() ?? "adb";
	}

	public static string TryGetDefaultAdbPath()
	{
		return File.Exists(DefaultWindowsAdbPath) ? DefaultWindowsAdbPath : null;
	}

	public async Task SendControlAsync(RemoteLinkProtocol.ControlType control, int x, int y, CancellationToken cancellationToken)
	{
		await TrySendControlAsync(control, x, y, cancellationToken).ConfigureAwait(false);
	}

	public async Task<string> TryStartSessionAsync(string adbPath, string serial, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(serial))
		{
			IsConnected = false;
			StatusText = "Select a device.";
			return StatusText;
		}

		await DisconnectAsync().ConfigureAwait(false);
		ClearDiagnosticLog();
		Log($"Connect serial={serial}");
		_adbPath = ResolveAdbPath(adbPath);
		SelectedSerial = serial;
		Log($"adb={_adbPath}");

		var sizeOutput = await RunAdbAsync(_adbPath, $"-s {Quote(serial)} shell wm size", cancellationToken).ConfigureAwait(false);
		if (AndroidAdbText.TryParseWmSize(sizeOutput, out var width, out var height))
		{
			ScreenWidth = width;
			ScreenHeight = height;
			Log($"wm size {width}x{height}");
		}
		else
		{
			Log("wm size not parsed: " + Truncate(sizeOutput, 200));
		}

		var jar = FindServerJar();
		if (jar == null)
		{
			Log($"jar not found (looked next to exe and {ServerJarFileName} under AndroidServer/build/libs). Screenshot fallback.");
			IsConnected = true;
			StatusText = ScreenWidth > 0
				? $"Screenshot preview {ScreenWidth}x{ScreenHeight}. Build remotelink-server.jar for H.264."
				: "Screenshot preview. Build remotelink-server.jar for H.264.";
			StartPreview(false);
			RaiseChanged();
			return StatusText;
		}

		var jarInfo = new FileInfo(jar);
		Log($"jar={jar} bytes={jarInfo.Length} written={jarInfo.LastWriteTimeUtc:u} dex={JarContainsDex(jar)}");

		try
		{
			var flags = await StartServerSessionAsync(jar, serial, cancellationToken).ConfigureAwait(false);
			IsConnected = true;
			var h264 = (flags & RemoteLinkProtocol.FlagVideo) != 0;
			Log($"Hello flags={flags} video={(h264 ? "yes" : "no")} encode={_encodeWidth}x{_encodeHeight}");
			StatusText = h264
				? $"Connecting video {ScreenWidth}x{ScreenHeight}…"
				: $"Connected {ScreenWidth}x{ScreenHeight}. Screenshot preview (encoder did not start).";
			StartPreview(h264);
			RaiseChanged();
			return StatusText;
		}
		catch (OperationCanceledException)
		{
			Log("Connect cancelled.");
			StopReceive();
			CloseSocket();
			KillServerProcess();
			IsConnected = false;
			StatusText = "Connect cancelled.";
			RaiseChanged();
			throw;
		}
		catch (Exception ex)
		{
			Log($"Connect failed {ex.GetType().Name}: {ex.Message}");
			StopReceive();
			CloseSocket();
			KillServerProcess();
			IsConnected = true;
			StatusText = $"Screenshot preview (server failed: {ex.Message}).";
			StartPreview(false);
			RaiseChanged();
			return StatusText;
		}
	}

	private static string Quote(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "\"\"";
		}

		if (value.Contains(' ', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
		{
			return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
		}

		return value;
	}

	private static async Task<string> RunAdbAsync(string executable, string arguments, CancellationToken cancellationToken, bool throwOnError = true)
	{
		using var process = StartAdb(executable, arguments, true);
		var stdout = new StringBuilder();
		var stderr = new StringBuilder();
		process.OutputDataReceived += (_, e) =>
		{
			if (e.Data != null)
			{
				stdout.AppendLine(e.Data);
			}
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (e.Data != null)
			{
				stderr.AppendLine(e.Data);
			}
		};
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
		if (throwOnError && (process.ExitCode != 0))
		{
			var error = stderr.ToString().Trim();
			if (error.Length == 0)
			{
				error = stdout.ToString().Trim();
			}

			throw new InvalidOperationException(string.IsNullOrEmpty(error)
				? $"adb exited {process.ExitCode}."
				: error);
		}

		return stdout.ToString();
	}

	private static Process StartAdb(string executable, string arguments, bool redirect)
	{
		var start = new ProcessStartInfo
		{
			FileName = executable,
			Arguments = arguments,
			RedirectStandardOutput = redirect,
			RedirectStandardError = redirect,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		try
		{
			var process = Process.Start(start);
			if (process == null)
			{
				throw new InvalidOperationException("Failed to start adb.");
			}

			return process;
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			throw new InvalidOperationException($"Could not start adb at \"{executable}\". Set the ADB path on this tab. {ex.Message}", ex);
		}
	}

	private void CloseSocket()
	{
		try
		{
			_client?.Close();
		}
		catch
		{
			// Ignore.
		}

		_client = null;
		try
		{
			_listener?.Stop();
		}
		catch
		{
			// Ignore.
		}

		_listener = null;
	}

	private void KillServerProcess()
	{
		try
		{
			if ((_serverProcess != null) && !_serverProcess.HasExited)
			{
				_serverProcess.Kill(true);
			}
		}
		catch
		{
			// Ignore.
		}

		_serverProcess?.Dispose();
		_serverProcess = null;
	}

	private void FlushUi()
	{
		Interlocked.Exchange(ref _uiQueued, 0);
		var decoder = _decoder;
		if (_h264Live && (decoder != null) && decoder.CopyPixels(ref _previewPixels, out var width, out var height))
		{
			PreviewPixels = _previewPixels;
			PreviewWidth = width;
			PreviewHeight = height;
			var error = decoder.LastError;
			StatusText = string.IsNullOrEmpty(error)
				? $"Mirroring {width}x{height} H.264."
				: error;
			var bucket = decoder.DecodedFrames / 60;
			if ((bucket > 0) && (bucket != _logFrameBucket))
			{
				_logFrameBucket = bucket;
				Log($"video packets={_videoPackets} decoded={decoder.DecodedFrames} preview={width}x{height}"
					+ (string.IsNullOrEmpty(error) ? "" : " error=" + error));
			}
		}

		Changed?.Invoke(this, EventArgs.Empty);
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
		QueueUi();
	}

	private async Task<bool> TrySendControlAsync(RemoteLinkProtocol.ControlType control, int x, int y, CancellationToken cancellationToken)
	{
		var client = _client;
		if ((client == null) || !client.Connected)
		{
			return false;
		}

		var bytes = RemoteLinkFraming.EncodeControl(control, x, y);
		await client.GetStream().WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
		return true;
	}

	private async Task<bool> TrySendControlTextAsync(string text, CancellationToken cancellationToken)
	{
		var client = _client;
		if ((client == null) || !client.Connected)
		{
			return false;
		}

		var bytes = RemoteLinkFraming.EncodeControlText(text);
		await client.GetStream().WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
		return true;
	}

	private static string EscapeAdbInputText(string value)
	{
		var builder = new StringBuilder(value.Length);
		foreach (var c in value)
		{
			if (c == ' ')
			{
				builder.Append("%s");
			}
			else if ((c == '%') || (c == '"') || (c == '\'') || (c == '\\') || (c == '&') || (c == '<') || (c == '>') || (c == ';') || (c == '(') || (c == ')') || (c == '|'))
			{
				builder.Append('\\').Append(c);
			}
			else
			{
				builder.Append(c);
			}
		}

		return builder.ToString();
	}

	private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
	{
		var client = _client;
		if (client == null)
		{
			return;
		}

		_decoder ??= new AirPlayH264Decoder();
		var stream = client.GetStream();
		while (!cancellationToken.IsCancellationRequested)
		{
			RemoteLinkMessage message;
			try
			{
				message = await RemoteLinkFraming.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				Log("Video receive cancelled.");
				return;
			}
			catch (Exception ex)
			{
				Log($"Video receive ended {ex.GetType().Name}: {ex.Message}");
				return;
			}

			if (message.Type != RemoteLinkProtocol.MessageType.VideoPacket)
			{
				if (_ignoredMessages < 5)
				{
					_ignoredMessages++;
					Log($"Ignoring message type {message.Type} length={message.Payload?.Length ?? 0}");
				}

				continue;
			}

			if (!_h264Live)
			{
				Log($"First video packet bytes={message.Payload?.Length ?? 0}");
			}

			_h264Live = true;
			var payload = message.Payload;
			if ((payload == null) || (payload.Length < 5))
			{
				Log("Video packet too short, skipped.");
				continue;
			}

			_videoPackets++;
			_decoder.Submit(new H264Data
			{
				Data = payload,
				Length = payload.Length,
				Width = _encodeWidth > 0 ? _encodeWidth : ScreenWidth,
				Height = _encodeHeight > 0 ? _encodeHeight : ScreenHeight,
				FrameType = 1
			});
			QueueUi();
		}
	}

	private static async Task<byte[]> RunAdbBinaryAsync(string executable, string arguments, CancellationToken cancellationToken)
	{
		var start = new ProcessStartInfo
		{
			FileName = executable,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using var process = Process.Start(start);
		if (process == null)
		{
			throw new InvalidOperationException("Failed to start adb.");
		}

		await using var output = new MemoryStream();
		var stdout = process.StandardOutput.BaseStream.CopyToAsync(output, cancellationToken);
		var stderr = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
		await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
		await stdout.ConfigureAwait(false);
		await stderr.ConfigureAwait(false);
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"adb exited {process.ExitCode}.");
		}

		return output.ToArray();
	}

	private async Task ScreenshotLoopAsync(CancellationToken cancellationToken)
	{
		var serial = SelectedSerial;
		var adb = _adbPath;
		if (string.IsNullOrWhiteSpace(serial) || string.IsNullOrWhiteSpace(adb))
		{
			return;
		}

		while (!cancellationToken.IsCancellationRequested && IsConnected)
		{
			if (_h264Live)
			{
				return;
			}

			try
			{
				var png = await RunAdbBinaryAsync(adb, $"-s {Quote(serial)} exec-out screencap -p", cancellationToken).ConfigureAwait(false);
				if (_h264Live)
				{
					return;
				}

				if (AndroidScreenshotPng.TryDecode(png, out var bgra, out var width, out var height))
				{
					PreviewPixels = bgra;
					PreviewWidth = width;
					PreviewHeight = height;
					if (ScreenWidth <= 0)
					{
						ScreenWidth = width;
					}

					if (ScreenHeight <= 0)
					{
						ScreenHeight = height;
					}

					StatusText = $"Screenshot preview {width}x{height}. Taps use adb.";
					if (_videoPackets == 0)
					{
						Log($"Screenshot frame {width}x{height} bytes={png.Length}");
						_videoPackets = -1;
					}

					RaiseChanged();
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch
			{
				// Keep polling; a single failed screencap is not fatal.
			}

			try
			{
				await Task.Delay(800, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
	}

	private async Task PushJarIfNeededAsync(string jar, string serial, CancellationToken cancellationToken)
	{
		var info = new FileInfo(jar);
		var key = serial + "|" + info.FullName + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks;
		if (_pushedJarKey == key)
		{
			Log("jar already pushed for this device and file stamp.");
			return;
		}

		Log($"adb push {jar} -> {DeviceJarPath}");
		await RunAdbAsync(_adbPath, $"-s {Quote(serial)} push {Quote(jar)} {DeviceJarPath}", cancellationToken).ConfigureAwait(false);
		_pushedJarKey = key;
	}

	private void StartPreview(bool h264)
	{
		StopReceive();
		_h264Live = false;
		_videoPackets = 0;
		_logFrameBucket = 0;
		_ignoredMessages = 0;
		_receiveToken = new CancellationTokenSource();
		if (h264)
		{
			Log("Starting H.264 receive loop.");
			_ = ReceiveLoopAsync(_receiveToken.Token);
			_ = ScreenshotFallbackAsync(_receiveToken.Token);
			return;
		}

		Log("Starting screenshot preview loop.");
		_ = ScreenshotLoopAsync(_receiveToken.Token);
	}

	private async Task ScreenshotFallbackAsync(CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (_h264Live || !IsConnected)
		{
			Log(_h264Live
				? "Screenshot fallback skipped; H.264 is live."
				: "Screenshot fallback skipped; not connected.");
			return;
		}

		Log("H.264 did not produce a frame in 5s; screenshot fallback.");
		await ScreenshotLoopAsync(cancellationToken).ConfigureAwait(false);
	}

	private string DescribeServerFailure()
	{
		string log;
		lock (_serverLog)
		{
			log = _serverLog.ToString().Trim();
		}

		if (log.Length > 800)
		{
			log = log.Substring(log.Length - 800);
		}

		var exit = "";
		try
		{
			if ((_serverProcess != null) && _serverProcess.HasExited)
			{
				exit = $"app_process exited {_serverProcess.ExitCode}. ";
			}
		}
		catch
		{
			// Ignore.
		}

		if (log.Length == 0)
		{
			return exit + "No output from app_process. Check USB debugging and that remotelink-server.jar was pushed.";
		}

		return exit + log;
	}

	private void AppendServerLog(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return;
		}

		lock (_serverLog)
		{
			_serverLog.AppendLine(line);
		}

		Log("device: " + line);
	}

	private void ClearDiagnosticLog()
	{
		lock (_diagnosticLines)
		{
			_diagnosticLines.Clear();
			DiagnosticLog = "";
		}
	}

	private void Log(string message)
	{
		var line = _time.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + "  " + message;
		lock (_diagnosticLines)
		{
			_diagnosticLines.Add(line);
			while (_diagnosticLines.Count > MaxDiagnosticLines)
			{
				_diagnosticLines.RemoveAt(0);
			}

			DiagnosticLog = string.Join(Environment.NewLine, _diagnosticLines);
		}

		Debug.WriteLine("RemoteLink: " + line);
		QueueUi();
	}

	private static bool JarContainsDex(string jar)
	{
		try
		{
			using var zip = ZipFile.OpenRead(jar);
			return zip.GetEntry("classes.dex") != null;
		}
		catch
		{
			return false;
		}
	}

	private async Task AppendLogcatAsync(string serial, CancellationToken cancellationToken)
	{
		try
		{
			var output = await RunAdbAsync(_adbPath, $"-s {Quote(serial)} logcat -d -t 60 AndroidRuntime:E art:E *:S", cancellationToken, false).ConfigureAwait(false);
			var trimmed = Truncate(output?.Trim(), 1200);
			if (string.IsNullOrEmpty(trimmed))
			{
				Log("logcat: (no AndroidRuntime/art errors)");
				return;
			}

			foreach (var line in trimmed.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
			{
				Log("logcat: " + line);
			}
		}
		catch (Exception ex)
		{
			Log("logcat failed: " + ex.Message);
		}
	}

	private static string Truncate(string value, int max)
	{
		if (string.IsNullOrEmpty(value) || (value.Length <= max))
		{
			return value ?? "";
		}

		return value.Substring(0, max) + "…";
	}

	private async Task<ushort> StartServerSessionAsync(string jar, string serial, CancellationToken cancellationToken)
	{
		lock (_serverLog)
		{
			_serverLog.Clear();
		}

		CloseSocket();
		KillServerProcess();
		var port = RemoteLinkProtocol.VideoPort;
		_listener = new TcpListener(IPAddress.Loopback, port);
		_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		_listener.Start();
		Log($"Listening on 127.0.0.1:{port}");
		await RunAdbAsync(_adbPath, $"-s {Quote(serial)} shell pkill -f cornerstone.remotelink.server.RemoteLinkServer", cancellationToken, false).ConfigureAwait(false);
		await RunAdbAsync(_adbPath, $"-s {Quote(serial)} reverse --remove tcp:{port}", cancellationToken, false).ConfigureAwait(false);
		Log($"adb reverse tcp:{port} tcp:{port}");
		await RunAdbAsync(_adbPath, $"-s {Quote(serial)} reverse tcp:{port} tcp:{port}", cancellationToken).ConfigureAwait(false);
		await PushJarIfNeededAsync(jar, serial, cancellationToken).ConfigureAwait(false);
		var sizeArgs = ScreenWidth > 0 && ScreenHeight > 0
			? $" {ScreenWidth} {ScreenHeight}"
			: " 0 0";
		var shell = $"-s {Quote(serial)} shell CLASSPATH={DeviceJarPath} app_process / cornerstone.remotelink.server.RemoteLinkServer{sizeArgs}";
		Log("app_process " + shell);
		var acceptTask = _listener.AcceptTcpClientAsync();
		_serverProcess = StartAdb(_adbPath, shell, true);
		_serverProcess.OutputDataReceived += (_, e) => AppendServerLog(e.Data);
		_serverProcess.ErrorDataReceived += (_, e) => AppendServerLog(e.Data);
		_serverProcess.BeginOutputReadLine();
		_serverProcess.BeginErrorReadLine();
		var exitTask = _serverProcess.WaitForExitAsync(cancellationToken);
		try
		{
			var finished = await Task.WhenAny(acceptTask, exitTask).WaitAsync(TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
			if (finished == exitTask)
			{
				await Task.Delay(150, CancellationToken.None).ConfigureAwait(false);
				await AppendLogcatAsync(serial, cancellationToken).ConfigureAwait(false);
				var detail = DescribeServerFailure();
				Log("app_process exited before TCP connect. " + detail);
				throw new InvalidOperationException(detail);
			}

			_client = await acceptTask.ConfigureAwait(false);
			Log("TCP accepted from device.");
		}
		catch (TimeoutException ex)
		{
			var detail = DescribeServerFailure();
			Log("TCP accept timed out. " + detail);
			throw new TimeoutException("Device did not connect within 20 seconds. " + detail, ex);
		}

		_client.NoDelay = true;
		RemoteLinkMessage hello;
		try
		{
			hello = await RemoteLinkFraming.ReadAsync(_client.GetStream(), cancellationToken)
				.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
				.ConfigureAwait(false);
		}
		catch (TimeoutException ex)
		{
			Log("Hello wait timed out. " + DescribeServerFailure());
			throw new TimeoutException("Device connected but did not send Hello. " + DescribeServerFailure(), ex);
		}

		if (hello.Type != RemoteLinkProtocol.MessageType.Hello)
		{
			Log($"Expected Hello, got {hello.Type} length={hello.Payload?.Length ?? 0}");
			throw new InvalidDataException("Device did not send Hello.");
		}

		ushort flags = 0;
		if (RemoteLinkFraming.TryReadHello(hello.Payload, out var width, out var height, out flags) && (width > 0) && (height > 0))
		{
			_encodeWidth = width;
			_encodeHeight = height;
			if (ScreenWidth <= 0)
			{
				ScreenWidth = width;
			}

			if (ScreenHeight <= 0)
			{
				ScreenHeight = height;
			}

			var alignedWidth = ScreenWidth;
			var alignedHeight = ScreenHeight;
			AndroidAdbText.AlignOrientation(_encodeWidth, _encodeHeight, ref alignedWidth, ref alignedHeight);
			ScreenWidth = alignedWidth;
			ScreenHeight = alignedHeight;
		}

		return flags;
	}

	private void StopReceive()
	{
		try
		{
			_receiveToken?.Cancel();
		}
		catch
		{
			// Ignore.
		}

		_receiveToken?.Dispose();
		_receiveToken = null;
	}

	#endregion
}
