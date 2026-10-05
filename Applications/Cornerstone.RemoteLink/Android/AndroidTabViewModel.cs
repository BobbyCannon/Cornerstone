#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Theme;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.Android.Protocol;
using Cornerstone.RemoteLink.Keystone.State;

#endregion

namespace Cornerstone.RemoteLink.Android;

[SourceReflection]
[Notifiable(["*"])]
public partial class AndroidTabViewModel : ViewModel, IShellTab
{
	#region Constants

	private const int KeyAppSwitch = 187;
	private const int KeyBack = 4;
	private const int KeyDelete = 67;
	private const int KeyDpadDown = 20;
	private const int KeyDpadLeft = 21;
	private const int KeyDpadRight = 22;
	private const int KeyDpadUp = 19;
	private const int KeyEnter = 66;
	private const int KeyEscape = 111;
	private const int KeyForwardDelete = 112;
	private const int KeyHome = 3;
	private const int KeyMoveEnd = 123;
	private const int KeyMoveHome = 122;
	private const int KeyPageDown = 93;
	private const int KeyPageUp = 92;
	private const int KeyTab = 61;

	#endregion

	#region Fields

	private bool _didRefreshOnSelect;
	private CancellationTokenSource _operation;
	private bool _persistSettings;
	private DateTimeOffset _pointerStartTime;
	private double _pointerStartX;
	private double _pointerStartY;
	private readonly ClipboardService _clipboard;
	private readonly AndroidMirrorService _service;
	private readonly AppSettings _settings;

	#endregion

	#region Constructors

	public AndroidTabViewModel(AndroidMirrorService service, AppSettings settings, ClipboardService clipboard, IDispatcher dispatcher)
	{
		_service = service ?? throw new ArgumentNullException(nameof(service));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
		DisplayName = "Android";
		Devices = new PresentationList<AndroidDeviceItem>(dispatcher);
		StatusText = _service.StatusText;
		ProtocolSummary = $"RemoteLink protocol v{RemoteLinkProtocol.Version}  video {RemoteLinkProtocol.VideoPort}  control {RemoteLinkProtocol.ControlPort}";
	}

	#endregion

	#region Properties

	[Notify]
	public partial string AdbPath { get; set; }

	[Notify]
	public partial string DiagnosticLog { get; set; }

	public PresentationList<AndroidDeviceItem> Devices { get; }

	public string DisplayName { get; }

	[Notify]
	public partial string ErrorMessage { get; set; }

	[Notify]
	public partial int FrameVersion { get; set; }

	[Notify]
	public partial bool IsBusy { get; set; }

	[Notify]
	public partial bool IsConnected { get; set; }

	[Notify]
	public partial int PreviewHeight { get; set; }

	[Notify]
	public partial byte[] PreviewPixels { get; set; }

	[Notify]
	public partial int PreviewWidth { get; set; }

	public string ProtocolSummary { get; }

	[Notify]
	public partial bool ShowCaptureHint { get; set; }

	[Notify]
	public partial bool ShowDiagnosticLog { get; set; }

	[Notify]
	public partial int ScreenHeight { get; set; }

	[Notify]
	public partial int ScreenWidth { get; set; }

	[Notify]
	public partial AndroidDeviceItem SelectedDevice { get; set; }

	[Notify]
	public partial string StatusText { get; set; }

	#endregion

	#region Methods

	public bool CanCopyDiagnosticLog()
	{
		return !string.IsNullOrWhiteSpace(DiagnosticLog);
	}

	public bool CanConnect()
	{
		return !IsBusy && !IsConnected && (SelectedDevice != null) && (SelectedDevice.State == "device");
	}

	public bool CanDisconnect()
	{
		return IsConnected || IsBusy;
	}

	public bool CanRefresh()
	{
		return !IsBusy;
	}

	public bool CanSendNavigation()
	{
		return !IsBusy && (SelectedDevice != null) && (SelectedDevice.State == "device");
	}

	public bool CanTypeClipboard()
	{
		return CanSendNavigation();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanCopyDiagnosticLog))]
	public async Task CopyDiagnosticLogAsync()
	{
		if (string.IsNullOrEmpty(DiagnosticLog))
		{
			return;
		}

		await _clipboard.SetTextAsync(DiagnosticLog).ConfigureAwait(true);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanConnect))]
	public async Task ConnectAsync()
	{
		await RunOperationAsync(async token =>
		{
			StatusText = await _service.TryStartSessionAsync(AdbPath, SelectedDevice?.Serial, token).ConfigureAwait(true);
			CopySession();
		}).ConfigureAwait(true);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanDisconnect))]
	public async Task DisconnectAsync()
	{
		_operation?.Cancel();
		await _service.DisconnectAsync().ConfigureAwait(true);
		CopySession();
		ErrorMessage = null;
		RefreshCommands();
	}

	public Task RefreshOnFirstSelectAsync()
	{
		if (_didRefreshOnSelect || IsBusy)
		{
			return Task.CompletedTask;
		}

		_didRefreshOnSelect = true;
		return RefreshDevicesAsync();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanRefresh))]
	public async Task RefreshDevicesAsync()
	{
		await RunOperationAsync(async token =>
		{
			var devices = await _service.RefreshDevicesAsync(AdbPath, token).ConfigureAwait(true);
			Devices.Clear();
			foreach (var device in devices)
			{
				Devices.Add(device);
			}

			if ((SelectedDevice == null) && (Devices.Count > 0))
			{
				SelectedDevice = Devices[0];
			}

			StatusText = _service.StatusText;
			ErrorMessage = null;
		}).ConfigureAwait(true);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanSendNavigation))]
	public Task SendAppSwitch()
	{
		return SendKeyAsync(KeyAppSwitch, RemoteLinkProtocol.ControlType.AppSwitch);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanSendNavigation))]
	public Task SendBack()
	{
		return SendKeyAsync(KeyBack, RemoteLinkProtocol.ControlType.Back);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanSendNavigation))]
	public Task SendHome()
	{
		return SendKeyAsync(KeyHome, RemoteLinkProtocol.ControlType.Home);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanTypeClipboard))]
	public async Task TypeClipboardAsync()
	{
		if (SelectedDevice == null)
		{
			return;
		}

		try
		{
			var text = await _clipboard.GetTextAsync().ConfigureAwait(true);
			if (string.IsNullOrEmpty(text))
			{
				ErrorMessage = "Clipboard is empty or could not be read.";
				return;
			}

			await _service.InjectTextAsync(AdbPath, SelectedDevice.Serial, text, CancellationToken.None).ConfigureAwait(true);
			ErrorMessage = null;
			StatusText = $"Typed {text.Length} characters.";
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	public Task SendKeyboardKeyAsync(int androidKeyCode)
	{
		return SendKeyAsync(androidKeyCode, RemoteLinkProtocol.ControlType.Key);
	}

	public static bool TryMapSpecialKey(string keyName, out int androidKeyCode)
	{
		androidKeyCode = keyName switch
		{
			"Enter" or "Return" => KeyEnter,
			"Back" or "Backspace" => KeyDelete,
			"Delete" => KeyForwardDelete,
			"Tab" => KeyTab,
			"Escape" => KeyEscape,
			"Up" => KeyDpadUp,
			"Down" => KeyDpadDown,
			"Left" => KeyDpadLeft,
			"Right" => KeyDpadRight,
			"Home" => KeyMoveHome,
			"End" => KeyMoveEnd,
			"PageUp" => KeyPageUp,
			"PageDown" => KeyPageDown,
			_ => 0
		};
		return androidKeyCode != 0;
	}

	public async Task SendTextAsync(string text)
	{
		if (string.IsNullOrEmpty(text) || (SelectedDevice == null))
		{
			return;
		}

		try
		{
			await _service.InjectTextAsync(AdbPath, SelectedDevice.Serial, text, CancellationToken.None).ConfigureAwait(true);
			ErrorMessage = null;
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	public Task SendPointerMoveAsync(double x, double y)
	{
		if (!_service.HasLiveControl || (SelectedDevice == null) || (ScreenWidth <= 0) || (ScreenHeight <= 0))
		{
			return Task.CompletedTask;
		}

		var displayWidth = ScreenWidth;
		var displayHeight = ScreenHeight;
		AndroidAdbText.AlignOrientation(PreviewWidth, PreviewHeight, ref displayWidth, ref displayHeight);
		var px = ClampPixel(x, displayWidth);
		var py = ClampPixel(y, displayHeight);
		return _service.SendControlAsync(RemoteLinkProtocol.ControlType.TouchMove, px, py, CancellationToken.None);
	}

	public async Task SendPointerAsync(double startX, double startY, double endX, double endY, TimeSpan duration, bool isRelease)
	{
		if (!IsConnected || (SelectedDevice == null) || (ScreenWidth <= 0) || (ScreenHeight <= 0))
		{
			return;
		}

		var displayWidth = ScreenWidth;
		var displayHeight = ScreenHeight;
		AndroidAdbText.AlignOrientation(PreviewWidth, PreviewHeight, ref displayWidth, ref displayHeight);
		var x1 = ClampPixel(startX, displayWidth);
		var y1 = ClampPixel(startY, displayHeight);
		var x2 = ClampPixel(endX, displayWidth);
		var y2 = ClampPixel(endY, displayHeight);
		var serial = SelectedDevice.Serial;
		try
		{
			if (_service.HasLiveControl)
			{
				var control = isRelease
					? RemoteLinkProtocol.ControlType.TouchUp
					: RemoteLinkProtocol.ControlType.TouchDown;
				if (isRelease && ((Math.Abs(x2 - x1) > 2) || (Math.Abs(y2 - y1) > 2)))
				{
					await _service.SendControlAsync(RemoteLinkProtocol.ControlType.TouchMove, x2, y2, CancellationToken.None).ConfigureAwait(true);
				}

				await _service.SendControlAsync(control, isRelease ? x2 : x1, isRelease ? y2 : y1, CancellationToken.None).ConfigureAwait(true);
				return;
			}

			if (!isRelease)
			{
				return;
			}

			if ((duration.TotalMilliseconds > 200) || (Math.Abs(x2 - x1) > 8) || (Math.Abs(y2 - y1) > 8))
			{
				var ms = Math.Max(50, (int) duration.TotalMilliseconds);
				await _service.InjectSwipeAsync(AdbPath, serial, x1, y1, x2, y2, ms, CancellationToken.None).ConfigureAwait(true);
			}
			else
			{
				await _service.InjectTapAsync(AdbPath, serial, x2, y2, CancellationToken.None).ConfigureAwait(true);
			}
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	public void StorePointerStart(double normalizedX, double normalizedY)
	{
		_pointerStartX = normalizedX;
		_pointerStartY = normalizedY;
		_pointerStartTime = DateTimeOffset.UtcNow;
	}

	public DateTimeOffset PointerStartTime => _pointerStartTime;

	public double PointerStartX => _pointerStartX;

	public double PointerStartY => _pointerStartY;

	public override void StartLifecycle()
	{
		AdbPath = _settings.AdbPath ?? string.Empty;
		_persistSettings = true;
		if (string.IsNullOrWhiteSpace(AdbPath))
		{
			var discovered = AndroidMirrorService.TryGetDefaultAdbPath();
			if (discovered != null)
			{
				AdbPath = discovered;
			}
		}

		_service.Changed += ServiceOnChanged;
		CopySession();
		base.StartLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		_service.Changed -= ServiceOnChanged;
		_operation?.Cancel();
		_service.DisconnectAsync().GetAwaiter().GetResult();
		CopySession();
		base.UninitializeLifecycle();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if ((propertyName == nameof(AdbPath))
			|| (propertyName == nameof(IsBusy))
			|| (propertyName == nameof(IsConnected))
			|| (propertyName == nameof(SelectedDevice))
			|| (propertyName == nameof(DiagnosticLog)))
		{
			RefreshCommands();
		}

		if (_persistSettings && (propertyName == nameof(AdbPath)))
		{
			_settings.AdbPath = AdbPath;
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	private static int ClampPixel(double normalized, int size)
	{
		if (size <= 0)
		{
			return 0;
		}

		var pixel = (int) Math.Round(normalized * size);
		if (pixel < 0)
		{
			return 0;
		}

		if (pixel >= size)
		{
			return size - 1;
		}

		return pixel;
	}

	private void CopySession()
	{
		IsConnected = _service.IsConnected;
		ScreenWidth = _service.ScreenWidth;
		ScreenHeight = _service.ScreenHeight;
		StatusText = _service.StatusText;
		DiagnosticLog = _service.DiagnosticLog;
		PreviewPixels = _service.PreviewPixels;
		PreviewWidth = _service.PreviewWidth;
		PreviewHeight = _service.PreviewHeight;
		ShowCaptureHint = IsConnected && (PreviewWidth <= 0);
		if (PreviewPixels != null)
		{
			FrameVersion++;
		}
	}

	private void ServiceOnChanged(object sender, EventArgs e)
	{
		CopySession();
	}

	private void RefreshCommands()
	{
		(ConnectAsyncCommand as RelayCommand)?.Refresh();
		(DisconnectAsyncCommand as RelayCommand)?.Refresh();
		(RefreshDevicesAsyncCommand as RelayCommand)?.Refresh();
		(SendBackCommand as RelayCommand)?.Refresh();
		(SendHomeCommand as RelayCommand)?.Refresh();
		(SendAppSwitchCommand as RelayCommand)?.Refresh();
		(CopyDiagnosticLogAsyncCommand as RelayCommand)?.Refresh();
		(TypeClipboardAsyncCommand as RelayCommand)?.Refresh();
	}

	private async Task RunOperationAsync(Func<CancellationToken, Task> work)
	{
		_operation?.Cancel();
		_operation?.Dispose();
		_operation = new CancellationTokenSource();
		IsBusy = true;
		ErrorMessage = null;
		RefreshCommands();
		try
		{
			await work(_operation.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			ErrorMessage = "Cancelled.";
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			StatusText = ex.Message;
		}
		finally
		{
			IsBusy = false;
			RefreshCommands();
		}
	}

	private async Task SendKeyAsync(int keyCode, RemoteLinkProtocol.ControlType control)
	{
		if (SelectedDevice == null)
		{
			return;
		}

		try
		{
			if (_service.HasLiveControl)
			{
				var x = control == RemoteLinkProtocol.ControlType.Key ? keyCode : 0;
				await _service.SendControlAsync(control, x, 0, CancellationToken.None).ConfigureAwait(true);
			}
			else
			{
				await _service.InjectKeyAsync(AdbPath, SelectedDevice.Serial, keyCode, CancellationToken.None).ConfigureAwait(true);
			}
			ErrorMessage = null;
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	#endregion
}
