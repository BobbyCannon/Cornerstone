#region References

using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

/// <summary>
/// Pausable native surface for media playback.
/// Windows/iOS use Avalonia's default child (HWND / attach target).
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
	/// Recreates the native child when the adapter publishes a platform surface (Android).
	/// </summary>
	public void RefreshPlatformSurface()
	{
		if (_adapter?.PlatformHandle == null)
		{
			return;
		}

		if (NativeHost.IsVisible)
		{
			NativeHost.IsVisible = false;
		}

		NativeHost.IsVisible = true;
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
