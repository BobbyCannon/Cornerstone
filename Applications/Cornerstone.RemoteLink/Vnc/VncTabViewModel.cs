#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Theme;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.Keystone.State;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.SecurityTypes;
using Cornerstone.RemoteLink.Vnc.Client.Security;
using Cornerstone.RemoteLink.Vnc.Controls;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.Vnc;

[SourceReflection]
[Notifiable(["*"])]
public partial class VncTabViewModel : ViewModel, IShellTab, IAuthenticationHandler
{
	#region Fields

	private readonly ClipboardService _clipboardService;
	private CancellationTokenSource _connectCancellation;
	private readonly VncConnectionService _connectionService;
	private readonly VncFramebuffer _framebuffer = new();
	private bool _persistSettings;
	private readonly AppSettings _settings;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public VncTabViewModel(
		VncConnectionService connectionService,
		ClipboardService clipboardService,
		AppSettings settings)
	{
		_connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
		_clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		DisplayName = "VNC";
		Host = "127.0.0.1";
		Port = 5900;
		Password = string.Empty;
	}

	#endregion

	#region Properties

	[Notify]
	public partial string ConnectionStatus { get; set; }

	public string DisplayName { get; }

	[Notify]
	public partial string ErrorMessage { get; set; }

	[Notify]
	public partial string Host { get; set; }

	[Notify]
	public partial bool IsBusy { get; set; }

	[Notify]
	public partial bool IsConnected { get; set; }

	[Notify]
	public partial string Password { get; set; }

	[Notify]
	public partial int Port { get; set; }

	[Notify]
	public partial RfbConnection RfbConnection { get; set; }

	#endregion

	#region Methods

	public bool CanConnect()
	{
		return !IsBusy && !IsConnected && IsHostValid() && (Port is >= 0 and <= 65535);
	}

	public bool CanDisconnect()
	{
		return IsConnected || IsBusy;
	}

	public bool CanSetClipboard()
	{
		return IsConnected && (RfbConnection != null);
	}

	[RelayCommand(CanExecuteMethod = nameof(CanConnect))]
	public async Task ConnectAsync()
	{
		_connectCancellation?.Cancel();
		_connectCancellation?.Dispose();
		_connectCancellation = new CancellationTokenSource();
		IsBusy = true;
		ErrorMessage = null;
		RefreshCommands();
		try
		{
			RfbConnection = await _connectionService.ConnectAsync(Host?.Trim(), Port, this, _framebuffer, _connectCancellation.Token).ConfigureAwait(true);
			IsConnected = true;
			ConnectionStatus = RfbConnection?.DesktopName;
			Password = string.Empty;
		}
		catch (OperationCanceledException)
		{
			ErrorMessage = "Connect cancelled.";
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			RfbConnection = null;
			IsConnected = false;
		}
		finally
		{
			IsBusy = false;
			RefreshCommands();
		}
	}

	[RelayCommand(CanExecuteMethod = nameof(CanDisconnect))]
	public async Task DisconnectAsync()
	{
		_connectCancellation?.Cancel();
		var connection = RfbConnection;
		RfbConnection = null;
		IsConnected = false;
		ConnectionStatus = null;
		_framebuffer.Clear();
		RefreshCommands();
		if (connection == null)
		{
			return;
		}

		try
		{
			await connection.CloseAsync().ConfigureAwait(true);
		}
		catch
		{
			// Closing a dropped session is best-effort.
		}
		finally
		{
			connection.Dispose();
		}
	}

	public Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
		where TInput : class, IAuthenticationInput
	{
		if (typeof(TInput) == typeof(PasswordAuthenticationInput))
		{
			var input = (TInput)(object)new PasswordAuthenticationInput(Password ?? string.Empty);
			return Task.FromResult(input);
		}

		throw new InvalidOperationException("The authentication input request is not supported.");
	}

	[RelayCommand(CanExecuteMethod = nameof(CanSetClipboard))]
	public async Task SetClipboardAsync()
	{
		var connection = RfbConnection;
		if (connection == null)
		{
			ErrorMessage = "Not connected.";
			return;
		}

		try
		{
			var text = await _clipboardService.GetTextAsync().ConfigureAwait(true);
			if (string.IsNullOrEmpty(text))
			{
				ErrorMessage = "Clipboard is empty or could not be read.";
				return;
			}

			await connection.TypeTextAsync(text).ConfigureAwait(true);
			ErrorMessage = null;
			ConnectionStatus = $"Typed {text.Length} characters.";
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
		}
	}

	public override void StartLifecycle()
	{
		Host = string.IsNullOrWhiteSpace(_settings.VncHost) ? "127.0.0.1" : _settings.VncHost;
		Port = _settings.VncPort is >= 0 and <= 65535 ? _settings.VncPort : 5900;
		_persistSettings = true;
		base.StartLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		_connectCancellation?.Cancel();
		var connection = RfbConnection;
		RfbConnection = null;
		IsConnected = false;
		connection?.Dispose();
		_framebuffer.Dispose();
		base.UninitializeLifecycle();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		if ((propertyName == nameof(Host)) || (propertyName == nameof(Port)) || (propertyName == nameof(IsBusy)) || (propertyName == nameof(IsConnected)) || (propertyName == nameof(RfbConnection)))
		{
			RefreshCommands();
		}

		if (_persistSettings)
		{
			if (propertyName == nameof(Host))
			{
				_settings.VncHost = Host;
			}
			else if (propertyName == nameof(Port))
			{
				_settings.VncPort = Port;
			}
		}

		base.OnPropertyChanged(propertyName, oldValue, newValue);
	}

	private bool IsHostValid()
	{
		return Uri.CheckHostName(Host?.Trim() ?? string.Empty) != UriHostNameType.Unknown;
	}

	private void RefreshCommands()
	{
		(ConnectAsyncCommand as RelayCommand)?.Refresh();
		(DisconnectAsyncCommand as RelayCommand)?.Refresh();
		(SetClipboardAsyncCommand as RelayCommand)?.Refresh();
	}

	#endregion
}