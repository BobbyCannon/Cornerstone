#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

public partial class MediaPlayerControl : CornerstoneUserControl
{
	#region Fields

	public static readonly DirectProperty<MediaPlayerControl, string> MediaUrlProperty;

	private readonly IMediaPlayerAdapter _mediaPlayerAdapter;
	private string _mediaUrl;
	private readonly NativeControlHost _nativeHost;

	#endregion

	#region Constructors

	public MediaPlayerControl()
	{
		InitializeComponent();

		_nativeHost = this.FindControl<NativeControlHost>("NativeHost");
		_mediaPlayerAdapter = GetInstance<IMediaPlayerAdapter>();
	}

	static MediaPlayerControl()
	{
		MediaUrlProperty = AvaloniaProperty.RegisterDirect<MediaPlayerControl, string>(nameof(MediaUrl), o => o.MediaUrl, (o, v) => o.MediaUrl = v);
	}

	#endregion

	#region Properties

	public string MediaUrl
	{
		get => _mediaUrl;
		set
		{
			SetAndRaise(MediaUrlProperty, ref _mediaUrl, value);
			if (!string.Equals(value, _mediaUrl))
			{
				PlayVideo(value);
			}
		}
	}

	#endregion

	#region Methods

	public void PauseVideo()
	{
		_mediaPlayerAdapter.Pause();
	}

	public void PlayVideo(string url)
	{
		_mediaPlayerAdapter.Play(url);
	}

	public void StopVideo()
	{
		_mediaPlayerAdapter.Stop();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_mediaPlayerAdapter.Initialize(_nativeHost);
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		_mediaPlayerAdapter.Dispose();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}

	#endregion
}