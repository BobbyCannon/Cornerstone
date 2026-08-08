#region References

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Platform;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Camera;

/// <summary>
/// Pausable native camera surface. Hosts platform PreviewView (Android) when the adapter
/// publishes a platform handle; stays collapsed for frame-based platforms so Avalonia Image
/// preview is not covered by an empty native child.
/// </summary>
public sealed class CameraNativeHost : PausableNativeHost
{
	#region Fields

	public static readonly StyledProperty<ICameraAdapter> CameraAdapterProperty =
		AvaloniaProperty.Register<CameraNativeHost, ICameraAdapter>(nameof(CameraAdapter));

	private ICameraAdapter _subscribedAdapter;

	#endregion

	#region Constructors

	public CameraNativeHost()
	{
		// Start hidden only when there is no platform surface yet (Windows frame-based Image path).
		// When the adapter already published a handle (Android PreviewView), stay ready to host it.
		NativeHost.IsVisible = false;
	}

	#endregion

	#region Properties

	public ICameraAdapter CameraAdapter
	{
		get => GetValue(CameraAdapterProperty);
		set => SetValue(CameraAdapterProperty, value);
	}

	/// <inheritdoc />
	protected override bool UseDefaultNativeChildWhenNull => false;

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		// Do not require Handle != IntPtr.Zero: AndroidViewControlHandle is matched by
		// HandleDescriptor + View, and may report Handle before the view is parented.
		return CameraAdapter?.PlatformHandle;
	}

	/// <inheritdoc />
	protected override IPausableNativeSurface GetSurface()
	{
		return CameraAdapter;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == CameraAdapterProperty)
		{
			SubscribeAdapter(change.GetOldValue<ICameraAdapter>(), change.GetNewValue<ICameraAdapter>());
			SyncNativeHostAvailability();
		}

		base.OnPropertyChanged(change);

		if (change.Property == IsPausedProperty)
		{
			// After resume, keep host collapsed when there is no platform surface (frame-based).
			SyncNativeHostAvailability();
		}
	}

	/// <inheritdoc />
	protected override bool ShouldDestroyNativeControl(IPlatformHandle control)
	{
		// PreviewView / host FrameLayout is owned by the adapter for the preview lifetime.
		if ((CameraAdapter?.PlatformHandle != null)
			&& ReferenceEquals(control, CameraAdapter.PlatformHandle))
		{
			return false;
		}

		return base.ShouldDestroyNativeControl(control);
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if ((e.PropertyName == nameof(ICameraAdapter.PlatformHandle))
			|| (e.PropertyName == nameof(ICameraAdapter.IsPreviewing)))
		{
			SyncNativeHostAvailability();
			if (!IsPaused && HasPlatformHandle())
			{
				RequestWarmUnderlay();
			}
		}
	}

	private bool HasPlatformHandle()
	{
		return CameraAdapter?.PlatformHandle != null;
	}

	private void SubscribeAdapter(ICameraAdapter previous, ICameraAdapter next)
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
		}
	}

	/// <summary>
	/// Show the nested native host only when the adapter published a real surface and we are not paused.
	/// Recreate when the handle instance changes so Avalonia's Android host attaches the PreviewView.
	/// </summary>
	private void SyncNativeHostAvailability()
	{
		if (IsPaused)
		{
			// Pause path owns NestedNativeHost.IsVisible (HideWithSize).
			return;
		}

		var show = HasPlatformHandle();
		if (!show)
		{
			NativeHost.IsVisible = false;
			return;
		}

		// Ensure CreateNativeControlCore runs with the current PlatformHandle (AndroidViewControlHandle).
		if (NativeHost.IsVisible)
		{
			NativeHost.IsVisible = false;
		}

		NativeHost.IsVisible = true;
	}

	#endregion
}