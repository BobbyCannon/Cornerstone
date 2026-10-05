#region References

using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.MediaPlayer;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Pausable native surface for media playback.
/// Windows/iOS use Cornerstone's default child (HWND / attach target).
/// Android publishes AndroidViewControlHandle (TextureView host) via the adapter.
/// </summary>
public sealed class MediaPlayerNativeHost : PausableNativeHost
{
	#region Fields

	private BaseMediaPlayerAdapter _adapter;

	#endregion

	#region Methods

	/// <summary>
	/// Binds the playback adapter used for snapshot, visibility, resize, and platform surface.
	/// </summary>
	public void SetAdapter(BaseMediaPlayerAdapter adapter)
	{
		_adapter = adapter;
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
		// Android: TextureView host. Windows: null → default HWND child for MFPlay.
		var handle = _adapter?.PlatformHandle;
		if (handle != null)
		{
			return handle;
		}

		return null;
	}

	/// <inheritdoc />
	protected override IPausableNativeSurface GetSurface()
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

		return base.ShouldDestroyNativeControl(control);
	}

	#endregion
}
