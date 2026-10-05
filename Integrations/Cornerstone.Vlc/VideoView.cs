#region References

using System;
using System.ComponentModel;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Runtime;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

#endregion

namespace Cornerstone.Vlc;

/// <summary>
/// Cross-platform LibVLC video island. The native child is the platform LibVLCSharp VideoView.
/// </summary>
public class VideoView : PausableNativeHost, IVideoView
{
	#region Fields

	public static readonly StyledProperty<Uri> SourceProperty =
		PresentationProperty.Register<VideoView, Uri>(nameof(Source));

	public static readonly StyledProperty<IVideoViewAdapter> VideoViewAdapterProperty =
		PresentationProperty.Register<VideoView, IVideoViewAdapter>(nameof(VideoViewAdapter));

	private IVideoViewAdapter _subscribedAdapter;

	#endregion

	#region Constructors

	public VideoView()
	{
	}

	#endregion

	#region Properties

	public LibVlcMediaPlayer MediaPlayer => EnsureAdapter()?.MediaPlayer;

	public Uri Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public IVideoViewAdapter VideoViewAdapter
	{
		get => GetValue(VideoViewAdapterProperty);
		set => SetValue(VideoViewAdapterProperty, value);
	}

	#endregion

	#region Methods

	public void Pause()
	{
		EnsureAdapter()?.Pause();
	}

	public void Play()
	{
		EnsureAdapter()?.Play();
	}

	public void Stop()
	{
		EnsureAdapter()?.Stop();
	}

	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		var adapter = EnsureAdapter();
		return adapter?.AttachToHost(parent, this);
	}

	protected override void DestroyNativeControlCore(IPlatformHandle control)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			SubscribeAdapter(_subscribedAdapter, null);
			if (VideoViewAdapter is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		base.Dispose(disposing);
	}

	protected override IPausableNativeSurface GetSurface()
	{
		return EnsureAdapter();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		EnsureAdapter()?.DetachFromHost();
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == VideoViewAdapterProperty)
		{
			SubscribeAdapter(change.GetOldValue<IVideoViewAdapter>(), change.GetNewValue<IVideoViewAdapter>());
			PushSourceToAdapter();
		}
		else if (change.Property == SourceProperty)
		{
			PushSourceToAdapter();
		}

		base.OnPropertyChanged(change);
	}

	protected override bool ShouldDestroyNativeControl(IPlatformHandle control)
	{
		var adapter = EnsureAdapter();
		if ((adapter?.PlatformHandle != null) && ReferenceEquals(control, adapter.PlatformHandle))
		{
			return false;
		}

		return base.ShouldDestroyNativeControl(control);
	}

	private IVideoViewAdapter EnsureAdapter()
	{
		if (VideoViewAdapter != null)
		{
			return VideoViewAdapter;
		}

		IVideoViewAdapter adapter;
		try
		{
			adapter = AppBootstrap.GetInstance<IVideoViewAdapter>();
		}
		catch
		{
			adapter = new VideoViewAdapterStub();
		}

		VideoViewAdapter = adapter;
		return adapter;
	}

	private void PushSourceToAdapter()
	{
		var adapter = VideoViewAdapter;
		if (adapter == null)
		{
			return;
		}

		adapter.Source = Source;
	}

	private void SubscribeAdapter(IVideoViewAdapter previous, IVideoViewAdapter next)
	{
		if (ReferenceEquals(previous, next))
		{
			return;
		}

		if (_subscribedAdapter != null)
		{
			_subscribedAdapter.PropertyChanged -= AdapterOnPropertyChanged;
			_subscribedAdapter = null;
		}

		if (next != null)
		{
			next.PropertyChanged += AdapterOnPropertyChanged;
			_subscribedAdapter = next;
			next.Source = Source;
		}
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
	}

	#endregion
}
