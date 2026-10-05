#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.RemoteLink.AirPlay;

#endregion

namespace Cornerstone.RemoteLink.AirPlay;

[SourceReflection]
[Notifiable(["*"])]
public partial class AirPlayTabViewModel : ViewModel, IShellTab
{
	#region Fields

	private bool _clientAdded;
	private readonly AirPlayMirrorService _service;

	#endregion

	#region Constructors

	public AirPlayTabViewModel(AirPlayMirrorService service, IDispatcher dispatcher)
	{
		_service = service ?? throw new ArgumentNullException(nameof(service));
		DisplayName = "AirPlay";
		CopyFromService();
	}

	#endregion

	#region Properties

	public string DisplayName { get; }

	[Notify]
	public partial int FrameVersion { get; set; }

	[Notify]
	public partial bool IsConnected { get; set; }

	[Notify]
	public partial int PreviewHeight { get; set; }

	[Notify]
	public partial byte[] PreviewPixels { get; set; }

	[Notify]
	public partial int PreviewWidth { get; set; }

	[Notify]
	public partial string StatusText { get; set; }

	#endregion

	#region Methods

	public override void StartLifecycle()
	{
		if (!_clientAdded)
		{
			_service.Changed += ServiceOnChanged;
			_service.AddClient();
			_clientAdded = true;
		}

		CopyFromService();
		base.StartLifecycle();
	}

	public bool CanDisconnect()
	{
		return IsConnected;
	}

	[RelayCommand(CanExecuteMethod = nameof(CanDisconnect))]
	public void Disconnect()
	{
		_service.Disconnect();
	}

	public override void UninitializeLifecycle()
	{
		if (_clientAdded)
		{
			_service.Changed -= ServiceOnChanged;
			_service.RemoveClient();
			_clientAdded = false;
		}

		base.UninitializeLifecycle();
	}

	private void CopyFromService()
	{
		StatusText = _service.StatusText;
		IsConnected = _service.IsConnected;
		PreviewPixels = _service.PreviewPixels;
		PreviewWidth = _service.PreviewWidth;
		PreviewHeight = _service.PreviewHeight;
		FrameVersion++;
		(DisconnectCommand as RelayCommand)?.Refresh();
	}

	private void ServiceOnChanged(object sender, EventArgs e)
	{
		CopyFromService();
	}

	#endregion
}
