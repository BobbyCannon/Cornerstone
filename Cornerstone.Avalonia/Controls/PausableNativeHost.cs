#region References

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.Converters;
using Cornerstone.Avalonia.Resources;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Grid that hosts a native surface with optional pause: snapshot underlay, hide native host,
/// optional blur so Avalonia can paint over the region (native always draws above Avalonia).
/// </summary>
/// <remarks>
/// Subclasses own adapter lifetime and implement <see cref="CreateNativeControlCore" />,
/// <see cref="DestroyNativeControlCore" />, and <see cref="GetSurface" />.
/// Load-bearing pause rules (Windows HWND airspace):
/// no await between underlay apply and host hide; only toggle NativeControlHost.IsVisible
/// (HideWithSize / ShowInBounds); leave underlay mounted while live; blur on this Grid;
/// skip HandleResize while paused.
/// </remarks>
public abstract class PausableNativeHost : Grid, INativeHostPausable, IDisposable
{
	#region Fields

	public static readonly StyledProperty<double> BlurRadiusProperty =
		AvaloniaProperty.Register<PausableNativeHost, double>(nameof(BlurRadius), 30);

	public static readonly StyledProperty<bool> BlurWhenPausedProperty =
		AvaloniaProperty.Register<PausableNativeHost, bool>(nameof(BlurWhenPaused));

	public static readonly StyledProperty<bool> IsPausedProperty =
		AvaloniaProperty.Register<PausableNativeHost, bool>(nameof(IsPaused));

	public static readonly StyledProperty<bool> ResumeOnResizeProperty =
		AvaloniaProperty.Register<PausableNativeHost, bool>(nameof(ResumeOnResize), true);

	private Size _boundsWhenPaused;
	private readonly Border _fallbackBackground;
	private readonly NestedNativeHost _nativeHost;
	private TaskCompletionSource _nativeHostReadyCompletion = new();
	private int _pauseOperationId;
	private Bitmap _placeholderBitmap;
	private readonly Image _placeholderImage;
	private bool _suppressResizeResume;
	private int _underlayRefreshId;

	#endregion

	#region Constructors

	protected PausableNativeHost()
	{
		_fallbackBackground = new Border
		{
			IsVisible = false,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		_placeholderImage = new Image
		{
			Stretch = Stretch.Fill,
			// Stay visible once a warm underlay exists so Avalonia can keep the texture ready
			// under the HWND; on pause we only remove the native host.
			IsVisible = false,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		// Underlay under the native host. Native HWND paints above Avalonia until hidden;
		// snapshot / Grid.Background remain so the first frame after hide is not empty.
		Children.Add(_fallbackBackground);
		Children.Add(_placeholderImage);

		_nativeHost = new NestedNativeHost(this)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Children.Add(_nativeHost);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gaussian blur radius applied when <see cref="BlurWhenPaused" /> is true. Default 30.
	/// </summary>
	public double BlurRadius
	{
		get => GetValue(BlurRadiusProperty);
		set => SetValue(BlurRadiusProperty, value);
	}

	/// <summary>
	/// When true and paused, applies <see cref="BlurEffect" /> to the full-resolution snapshot.
	/// </summary>
	public bool BlurWhenPaused
	{
		get => GetValue(BlurWhenPausedProperty);
		set => SetValue(BlurWhenPausedProperty, value);
	}

	/// <summary>
	/// Whether the native surface is currently painting.
	/// </summary>
	public bool IsNativeSurfaceVisible => GetSurface()?.IsNativeSurfaceVisible ?? true;

	/// <summary>
	/// Nested Avalonia <see cref="NativeControlHost" /> that owns the platform child handle.
	/// </summary>
	public NativeControlHost NativeHost => _nativeHost;

	/// <summary>
	/// When true, freezes the surface as a snapshot image and hides the native host so
	/// Avalonia controls can appear over this region. When false, restores the live surface.
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}

	/// <summary>
	/// When true (default), a significant size change while paused can restore the live surface.
	/// Auto-resume is currently disabled; property is retained for API compatibility.
	/// </summary>
	public bool ResumeOnResize
	{
		get => GetValue(ResumeOnResizeProperty);
		set => SetValue(ResumeOnResizeProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Captures the currently visible native surface as a PNG.
	/// </summary>
	public async Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		await WaitForNativeHost().ConfigureAwait(true);

		var surface = GetSurface();
		if (surface == null)
		{
			return NativeSurfaceSnapshot.Failed("Native surface is not available.");
		}

		return await surface.CaptureSnapshotAsync(options).ConfigureAwait(true);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Completes when Avalonia has created the native host at least once for this instance.
	/// </summary>
	public Task WaitForNativeHost()
	{
		return _nativeHostReadyCompletion.Task;
	}

	/// <summary>
	/// Creates or re-hosts the platform native control for Avalonia.
	/// Return null to use Avalonia's default child when <see cref="UseDefaultNativeChildWhenNull" /> is true.
	/// </summary>
	protected abstract IPlatformHandle CreateNativeControlCore(IPlatformHandle parent);

	/// <summary>
	/// Called when Avalonia tears down the native host attachment (e.g. visual-tree detach).
	/// </summary>
	protected virtual void DestroyNativeControlCore(IPlatformHandle control)
	{
	}

	/// <summary>
	/// When CreateNativeControlCore returns null, use Avalonia's default child (true for surfaces
	/// that attach to Avalonia's default HWND/view). Return false when an empty default child
	/// would cover a non-HWND preview path.
	/// </summary>
	protected virtual bool UseDefaultNativeChildWhenNull => true;

	/// <summary>
	/// Return false to keep an adapter-owned platform handle alive across host tear-down.
	/// </summary>
	protected virtual bool ShouldDestroyNativeControl(IPlatformHandle control)
	{
		return true;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		ClearPlaceholderBitmap();
	}

	/// <summary>
	/// Platform surface used for snapshot, visibility, and resize. May be null before first create.
	/// </summary>
	protected abstract IPausableNativeSurface GetSurface();

	/// <summary>
	/// Best-effort capture into the underlay while live so the next pause is snappy.
	/// </summary>
	public void RequestWarmUnderlay()
	{
		_ = RefreshUnderlayAsync(true);
	}

	/// <summary>
	/// Resets the native-host ready gate (e.g. after adapter release on detach).
	/// </summary>
	protected void ResetNativeHostReady()
	{
		_nativeHostReadyCompletion = new TaskCompletionSource();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == IsPausedProperty)
		{
			_ = ApplyPausedStateAsync(change.GetNewValue<bool>());
		}
		else if ((change.Property == BlurWhenPausedProperty) || (change.Property == BlurRadiusProperty))
		{
			if (IsPaused)
			{
				UpdatePlaceholderBlur();
			}
		}

		base.OnPropertyChanged(change);

		if (change.Property == BoundsProperty)
		{
			var newValue = change.GetNewValue<Rect>();
			var surface = GetSurface();

			// Do not resize/show the native HWND while paused — Avalonia + adapter would re-open airspace.
			if (!IsPaused && (surface?.IsNativeSurfaceVisible != false))
			{
				var scaling = (float) (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
				surface?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
			}

			HandleBoundsChanged(newValue.Size);
		}
	}

	private async Task ApplyPausedStateAsync(bool paused)
	{
		var operationId = ++_pauseOperationId;

		if (paused)
		{
			await PauseCoreAsync(operationId).ConfigureAwait(true);
		}
		else
		{
			ResumeCore();
		}
	}

	/// <summary>
	/// Installs freeze-frame pixels under the native host (Image + Grid.Background).
	/// Does not hide the native surface — caller decides when to remove the HWND.
	/// </summary>
	private void ApplyUnderlayBitmap(Bitmap next)
	{
		if (next == null)
		{
			return;
		}

		var previous = _placeholderBitmap;
		_placeholderBitmap = next;
		_placeholderImage.Source = next;
		_placeholderImage.Opacity = 1.0;
		_placeholderImage.IsVisible = true;
		_fallbackBackground.IsVisible = false;

		// Grid background paints with the control itself when the native host leaves the layout,
		// which is more reliable than a sibling Image alone for the first post-hide frame.
		Background = new ImageBrush
		{
			Source = next,
			Stretch = Stretch.Fill
		};

		if (!ReferenceEquals(previous, next))
		{
			previous?.Dispose();
		}

		UpdatePlaceholderBlur();
		InvalidateVisual();
		UpdateLayout();
	}

	private void ClearPlaceholderBitmap()
	{
		Effect = null;
		_placeholderImage.Effect = null;
		_placeholderImage.Source = null;
		_placeholderImage.IsVisible = false;
		Background = null;
		_placeholderBitmap?.Dispose();
		_placeholderBitmap = null;
	}

	/// <summary>
	/// Puts the native host back on top for live mode (HWND airspace above Avalonia).
	/// </summary>
	private void EnsureHostAbovePlaceholder()
	{
		if ((Children.Count > 0) && ReferenceEquals(Children[^1], _nativeHost))
		{
			return;
		}

		Children.Remove(_nativeHost);
		Children.Add(_nativeHost);
	}

	/// <summary>
	/// Puts the snapshot above the native host in Avalonia Z-order so when the HWND is
	/// hidden we do not flash an empty host rect that still covers the underlay sibling.
	/// </summary>
	private void EnsurePlaceholderAboveHost()
	{
		if ((Children.Count > 0) && ReferenceEquals(Children[^1], _placeholderImage))
		{
			return;
		}

		Children.Remove(_placeholderImage);
		Children.Add(_placeholderImage);
	}

	private void HandleBoundsChanged(Size newSize)
	{
		if (_suppressResizeResume || !IsPaused || !ResumeOnResize)
		{
			return;
		}

		const double epsilon = 1.0;
		if ((Math.Abs(newSize.Width - _boundsWhenPaused.Width) > epsilon)
			|| (Math.Abs(newSize.Height - _boundsWhenPaused.Height) > epsilon))
		{
			// Auto-resume on resize is currently disabled; ResumeOnResize retained for API compatibility.
			//IsPaused = false;
		}
	}

	/// <summary>
	/// Hides the native surface via Avalonia <see cref="NativeControlHost" /> visibility only.
	/// Snapshot must already be applied and stacked above the host.
	/// </summary>
	/// <remarks>
	/// On Windows, Avalonia attaches native views under a holder HWND and uses HideWithSize /
	/// ShowInBounds. Calling ShowWindow on the child ourselves races that and flashes an
	/// empty host. So we only toggle the nested host IsVisible (→ HideWithSize)
	/// and keep the adapter flag for HandleResize / warm-underlay gating.
	/// </remarks>
	private void HideNativeSurface()
	{
		EnsurePlaceholderAboveHost();
		UpdateLayout();

		// Logical pause flag (skip resize / warm capture). Presentation is host IsVisible.
		GetSurface()?.SetNativeSurfaceVisible(false);
		// HideWithSize — do not destroy attachment; avoids recreate flash on resume.
		_nativeHost.IsVisible = false;
		_boundsWhenPaused = Bounds.Size;
	}

	private void NotifyNativeHostCreated()
	{
		_nativeHostReadyCompletion.TrySetResult();

		// If IsPaused was set before the host was ready, apply once ready.
		if (IsPaused)
		{
			_ = ApplyPausedStateAsync(true);
		}
		else
		{
			// Warm underlay while live so pause can hide the HWND without waiting on capture.
			_ = RefreshUnderlayAsync(true);
		}
	}

	private async Task PauseCoreAsync(int operationId)
	{
		_suppressResizeResume = true;
		try
		{
			// Capture while native is still visible. Warm underlay (if any) is already under the
			// HWND so Avalonia has texture data; we still refresh for a current freeze frame.
			var snapshot = await CaptureSnapshotAsync(NativeSurfaceSnapshotOptions.Default()).ConfigureAwait(true);
			if (operationId != _pauseOperationId)
			{
				return;
			}

			if (snapshot is { Success: true, PngBytes: { Length: > 0 } })
			{
				var next = ImageConverters.BytesToBitmap(snapshot.PngBytes);
				ApplyUnderlayBitmap(next);
			}
			else if (_placeholderBitmap == null)
			{
				Effect = null;
				_placeholderImage.Effect = null;
				_placeholderImage.IsVisible = false;
				_fallbackBackground.Background = ResourceService.GetColorAsBrush("Background03");
				_fallbackBackground.IsVisible = true;
				Background = ResourceService.GetColorAsBrush("Background03");
			}
			// else keep last good underlay

			// Critical: no await between underlay apply and HWND hide (same UI turn).
			UpdatePlaceholderBlur();
			HideNativeSurface();
		}
		finally
		{
			_suppressResizeResume = false;
		}
	}

	/// <summary>
	/// Captures the live surface into the underlay without pausing. Keeps a ready freeze frame
	/// so the next <see cref="IsPaused" /> can hide the HWND without waiting on capture.
	/// </summary>
	private async Task RefreshUnderlayAsync(bool warmOnly)
	{
		if (IsPaused && warmOnly)
		{
			return;
		}

		var surface = GetSurface();
		if ((surface == null) || !surface.IsNativeSurfaceVisible)
		{
			return;
		}

		var refreshId = ++_underlayRefreshId;
		try
		{
			var snapshot = await CaptureSnapshotAsync(NativeSurfaceSnapshotOptions.Default()).ConfigureAwait(true);
			if (refreshId != _underlayRefreshId)
			{
				return;
			}

			// Do not overwrite a pause-critical path mid-flight with a stale warm frame after unpause race.
			if (IsPaused && warmOnly)
			{
				return;
			}

			if (snapshot is not { Success: true, PngBytes: { Length: > 0 } })
			{
				return;
			}

			var next = ImageConverters.BytesToBitmap(snapshot.PngBytes);
			ApplyUnderlayBitmap(next);
		}
		catch
		{
			// Warm underlay is best-effort.
		}
	}

	private void ResumeCore()
	{
		_pauseOperationId++;
		// Leave underlay mounted. Clear pause flag before showing the host so ShowInBounds
		// does not race a still-hidden child HWND (empty holder flash).
		Effect = null;
		_placeholderImage.Effect = null;
		_fallbackBackground.IsVisible = false;
		EnsureHostAbovePlaceholder();
		GetSurface()?.SetNativeSurfaceVisible(true);
		_nativeHost.IsVisible = true;

		// Re-apply size after the native host is shown again.
		if (Bounds is { Width: > 0, Height: > 0 })
		{
			var scaling = (float) (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
			GetSurface()?.HandleResize((int) (Bounds.Width * scaling), (int) (Bounds.Height * scaling), scaling);
		}

		// Refresh warm underlay for the next pause (after the live surface is back).
		_ = RefreshUnderlayAsync(true);
	}

	private void UpdatePlaceholderBlur()
	{
		// Blur only while paused; keep a sharp warm underlay while live.
		// Apply Effect on this control (Grid) so both the Image child and the Grid.Background
		// ImageBrush blur together. Blurring only the Image leaves a sharp Background that
		// bleeds through soft blur edges.
		if (IsPaused && BlurWhenPaused && (BlurRadius > 0))
		{
			Effect = new BlurEffect { Radius = BlurRadius };
			_placeholderImage.Effect = null;
		}
		else
		{
			Effect = null;
			_placeholderImage.Effect = null;
		}
	}

	#endregion

	#region Classes

	/// <summary>
	/// Thin native host so the owner can own both the snapshot image and the native surface.
	/// </summary>
	private sealed class NestedNativeHost : NativeControlHost
	{
		#region Fields

		private readonly PausableNativeHost _owner;

		#endregion

		#region Constructors

		public NestedNativeHost(PausableNativeHost owner)
		{
			_owner = owner;
		}

		#endregion

		#region Methods

		protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
		{
			// Owner may return a platform surface or null.
			var handle = _owner.CreateNativeControlCore(parent);
			if (handle != null)
			{
				_owner.NotifyNativeHostCreated();
				return handle;
			}

			// Surfaces that attach to Avalonia's default child HWND/view (WebView, media player).
			// Camera (UseDefaultNativeChildWhenNull = false) must not get an empty default child —
			// that would block PreviewView and CameraX would time out waiting for a surface.
			if (_owner.UseDefaultNativeChildWhenNull)
			{
				handle = base.CreateNativeControlCore(parent);
				_owner.NotifyNativeHostCreated();
				return handle;
			}

			// No published handle yet: still need a temporary child for Avalonia's contract.
			// Camera keeps NestedNativeHost.IsVisible = false until PlatformHandle is set.
			handle = base.CreateNativeControlCore(parent);
			_owner.NotifyNativeHostCreated();
			return handle;
		}

		protected override void DestroyNativeControlCore(IPlatformHandle control)
		{
			_owner.DestroyNativeControlCore(control);
			if (_owner.ShouldDestroyNativeControl(control))
			{
				base.DestroyNativeControlCore(control);
			}
		}

		#endregion
	}

	#endregion
}