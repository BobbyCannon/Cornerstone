#region References

using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.MediaPlayer;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Native surface for media playback.
/// Windows/iOS use Cornerstone's default child (HWND / attach target).
/// Android publishes AndroidViewControlHandle (TextureView host) via the adapter.
/// </summary>
public sealed class MediaPlayerNativeHost : NativeSurfaceHost
{
	#region Fields

	private BaseMediaPlayerAdapter _adapter;

	#endregion

	#region Methods

	/// <summary>
	/// Binds the playback adapter used for visibility, resize, and the platform surface.
	/// </summary>
	public void SetAdapter(BaseMediaPlayerAdapter adapter)
	{
		_adapter = adapter;
		// Template children attach before OnApplyTemplate, so the subsurface may already
		// exist with no adapter. Recreate it now that the adapter can take the parent.
		if (NativeHost.IsAttachedToVisualTree)
		{
			NativeHost.RecreateNativeControl();
		}

		RefreshPlatformSurface();
	}

	/// <summary>
	/// Hosts the adapter surface. Android publishes the TextureView after the native host
	/// has already attached an empty default child, so the child has to be replaced.
	/// </summary>
	public void RefreshPlatformSurface()
	{
		var handle = _adapter?.PlatformHandle;
		if (handle == null)
		{
			return;
		}

		NativeHost.HostPlatformHandle(handle);
	}

	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		_adapter?.BindHostHandle(parent);
		// Returning null makes the nested host create a second subsurface and destroy
		// this one. The visible island would stay the orange placeholder.
		if (parent is INativePixelBufferHost)
		{
			return parent;
		}

		var handle = _adapter?.PlatformHandle;
		if (handle != null)
		{
			return handle;
		}

		return null;
	}

	/// <inheritdoc />
	protected override INativeSurface GetSurface()
	{
		return _adapter;
	}

	/// <inheritdoc />
	protected override bool ShouldDestroyNativeControl(IPlatformHandle control)
	{
		// Adapter owns the Android TextureView host for the player lifetime.
		if ((_adapter?.PlatformHandle != null)
			&& ReferenceEquals(control, _adapter.PlatformHandle))
		{
			return false;
		}

		// Wayland: the attachment owns the subsurface. The adapter only borrows it.
		if (control is INativePixelBufferHost)
		{
			return false;
		}

		return base.ShouldDestroyNativeControl(control);
	}

	#endregion
}
