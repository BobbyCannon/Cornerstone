#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.Theming;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Grid that hosts a native surface with optional pause: snapshot underlay, hide native host,
/// optional blur so Cornerstone can paint over the region (native always draws above Cornerstone).
/// </summary>
/// <remarks>
/// Subclasses own adapter lifetime and implement <see cref="CreateNativeControlCore" />,
/// <see cref="DestroyNativeControlCore" />, <see cref="AfterDestroyNativeControlCore" />,
/// and <see cref="GetSurface" />.
/// Load-bearing pause rules (Windows HWND airspace):
/// capture and apply underlay under the still-visible HWND, wait for a render pass, then
/// hide; only toggle NativeControlHost.IsVisible (HideWithSize / ShowInBounds); blur the
/// underlay Grid (never this host) after hide; drop the last freeze-frame on resume and
/// recapture a warm underlay while live; skip HandleResize while paused.
/// </remarks>
public abstract class PausableNativeHost : Grid, INativeHostPausable, IDisposable
{
	#region Fields

	public static readonly StyledProperty<double> BlurRadiusProperty =
		PresentationProperty.Register<PausableNativeHost, double>(nameof(BlurRadius), 30);

	public static readonly StyledProperty<bool> BlurWhenPausedProperty =
		PresentationProperty.Register<PausableNativeHost, bool>(nameof(BlurWhenPaused), true);

	public static readonly StyledProperty<bool> IsPausedProperty =
		PresentationProperty.Register<PausableNativeHost, bool>(nameof(IsPaused));

	public static readonly StyledProperty<bool> ResumeOnResizeProperty =
		PresentationProperty.Register<PausableNativeHost, bool>(nameof(ResumeOnResize), true);

	private Size _boundsWhenPaused;
	private readonly SemaphoreSlim _captureGate = new(1, 1);
	private readonly Border _fallbackBackground;
	private readonly NestedNativeHost _nativeHost;
	private TaskCompletionSource _nativeHostReadyCompletion = new();
	private Task _pauseApplyTask = Task.CompletedTask;
	private int _pauseOperationId;
	private Bitmap _placeholderBitmap;
	private readonly Image _placeholderImage;
	private bool _suppressResizeResume;
	private readonly Grid _underlay;
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

			// Stay visible once a warm underlay exists so Cornerstone can keep the texture ready
			// under the HWND; on pause we only remove the native host.
			IsVisible = false,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};

		// Underlay is a sibling Grid, not this control. BlurEffect on PausableNativeHost
		// rasterizes the visual that owns NestedNativeHost; TransformToVisual then fails
		// and WebView2 can initialize at 0x0 and never paint after unpause.
		_underlay = new Grid
		{
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_underlay.Children.Add(_fallbackBackground);
		_underlay.Children.Add(_placeholderImage);

		// Underlay under the native host. Native HWND paints above Cornerstone until hidden;
		// snapshot / Grid.Background remain so the first frame after hide is not empty.
		Children.Add(_underlay);

		_nativeHost = new NestedNativeHost(this)
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		#if !BROWSER
		Children.Add(_nativeHost);
		#endif
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
	/// True when paused, the native host is hidden, and a real snapshot (not a solid fill) is showing.
	/// </summary>
	public bool IsPauseSurfaceReady => IsPaused && !_nativeHost.IsVisible && (_placeholderBitmap != null);

	/// <summary>
	/// When true, freezes the surface as a snapshot image and hides the native host so
	/// Cornerstone controls can appear over this region. When false, restores the live surface.
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}

	/// <summary>
	/// Nested Cornerstone <see cref="NativeControlHost" /> that owns the platform child handle.
	/// </summary>
	public NativeControlHost NativeHost => _nativeHost;

	/// <summary>
	/// When true (default), a significant size change while paused can restore the live surface.
	/// Auto-resume is currently disabled; property is retained for API compatibility.
	/// </summary>
	public bool ResumeOnResize
	{
		get => GetValue(ResumeOnResizeProperty);
		set => SetValue(ResumeOnResizeProperty, value);
	}

	/// <summary>
	/// When CreateNativeControlCore returns null, use Cornerstone's default child (true for surfaces
	/// that attach to Cornerstone's default HWND/view). Return false when an empty default child
	/// would cover a non-HWND preview path.
	/// </summary>
	protected virtual bool UseDefaultNativeChildWhenNull => true;

	/// <summary>
	/// True when Cornerstone NativeControlHost is in the visual tree. Browser (WASM) omits it
	/// because that path locked the UI; overlay surfaces attach from OnAttachedToVisualTree instead.
	/// </summary>
	private bool IsNestedHostInTree => _nativeHost.Parent != null;

	#endregion

	#region Methods

	/// <summary>
	/// Captures the currently visible native surface as a PNG.
	/// </summary>
	public async Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		await _captureGate.WaitAsync().ConfigureAwait(true);
		try
		{
			await WaitForNativeHost().ConfigureAwait(true);

			var surface = GetSurface();
			if (surface == null)
			{
				return NativeSurfaceSnapshot.Failed("Native surface is not available.");
			}

			return await surface.CaptureSnapshotAsync(options).ConfigureAwait(true);
		}
		finally
		{
			_captureGate.Release();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Runs pause apply again while already paused (fresh capture). Use after the
	/// underlying content has settled (e.g. map camera idle).
	/// </summary>
	public Task ReapplyPausedStateAsync()
	{
		if (!IsPaused)
		{
			IsPaused = true;
			return WaitForPauseAppliedAsync();
		}

		_pauseApplyTask = BeginApplyPausedState(true);
		return _pauseApplyTask;
	}

	/// <summary>
	/// Best-effort capture into the underlay while live so the next pause is snappy.
	/// </summary>
	public void RequestWarmUnderlay()
	{
		_ = RefreshUnderlayAsync(true);
	}

	/// <summary>
	/// Completes when Cornerstone has created the native host at least once for this instance.
	/// </summary>
	public Task WaitForNativeHost()
	{
		return _nativeHostReadyCompletion.Task;
	}

	/// <summary>
	/// Completes when the latest <see cref="IsPaused" /> apply (snapshot + hide, or resume) finishes.
	/// </summary>
	public Task WaitForPauseAppliedAsync()
	{
		return _pauseApplyTask ?? Task.CompletedTask;
	}

	/// <summary>
	/// Called after Cornerstone has torn down the native host attachment when
	/// <see cref="ShouldDestroyNativeControl" /> is true. Use this to destroy platform
	/// peers that must be unparented first (for example Android WebView.Destroy).
	/// </summary>
	protected virtual void AfterDestroyNativeControlCore(IPlatformHandle control)
	{
	}

	/// <summary>
	/// Creates or re-hosts the platform native control for Cornerstone.Presentation.
	/// Return null to use Cornerstone's default child when <see cref="UseDefaultNativeChildWhenNull" /> is true.
	/// </summary>
	protected abstract IPlatformHandle CreateNativeControlCore(IPlatformHandle parent);

	/// <summary>
	/// Called when Cornerstone tears down the native host attachment (e.g. visual-tree detach).
	/// </summary>
	protected virtual void DestroyNativeControlCore(IPlatformHandle control)
	{
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		ClearPlaceholderBitmap();
		_captureGate.Dispose();
	}

	/// <summary>
	/// Platform surface used for snapshot, visibility, and resize. May be null before first create.
	/// </summary>
	protected abstract IPausableNativeSurface GetSurface();

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		if (IsNestedHostInTree)
		{
			return;
		}

		// Defer so tab switch / layout can finish first (WASM UI thread).
		Dispatcher.UIThread.Post(() =>
		{
			if (!this.IsAttachedToVisualTree() || IsNestedHostInTree)
			{
				return;
			}

			try
			{
				CreateNativeControlCore(null);
				NotifyNativeHostCreated();
			}
			catch (Exception ex)
			{
				Console.WriteLine("PausableNativeHost overlay attach failed: " + ex);
			}
		}, DispatcherPriority.Background);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == IsPausedProperty)
		{
			_pauseApplyTask = BeginApplyPausedState(change.GetNewValue<bool>());
		}
		else if ((change.Property == BlurWhenPausedProperty) || (change.Property == BlurRadiusProperty))
		{
			if (IsPaused)
			{
				UpdatePlaceholderBlur();
			}
		}

		base.OnPropertyChanged(change);

		if ((change.Property == IsVisibleProperty) && !IsNestedHostInTree && !IsPaused)
		{
			GetSurface()?.SetNativeSurfaceVisible(IsVisible);
		}

		if (change.Property == BoundsProperty)
		{
			var newValue = change.GetNewValue<Rect>();
			var surface = GetSurface();

			// Do not resize/show the native HWND while paused — Cornerstone + adapter would re-open airspace.
			if (!IsPaused && (surface?.IsNativeSurfaceVisible != false))
			{
				var scaling = (float) (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
				surface?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
			}

			HandleBoundsChanged(newValue.Size);
		}
	}

	/// <summary>
	/// Resets the native-host ready gate (e.g. after adapter release on detach).
	/// </summary>
	protected void ResetNativeHostReady()
	{
		_nativeHostReadyCompletion = new TaskCompletionSource();
	}

	/// <summary>
	/// Return false to keep an adapter-owned platform handle alive across host tear-down.
	/// </summary>
	protected virtual bool ShouldDestroyNativeControl(IPlatformHandle control)
	{
		return true;
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

	private void ApplySolidFallbackUnderlay()
	{
		Effect = null;
		_underlay.Effect = null;
		_placeholderImage.Effect = null;
		_placeholderImage.IsVisible = false;
		var fallback = ThemeBrushes.Get(this, "Background03");

		_fallbackBackground.Background = fallback;
		_fallbackBackground.IsVisible = true;
		Background = fallback;
	}

	/// <summary>
	/// Installs freeze-frame pixels under the native host (Image + Grid.Background).
	/// Does not hide the native surface — caller decides when to remove the HWND.
	/// Does not apply pause blur; call <see cref="UpdatePlaceholderBlur" /> before hide.
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

		InvalidateVisual();
		UpdateLayout();
	}

	private Task BeginApplyPausedState(bool paused)
	{
		if (Dispatcher.UIThread.CheckAccess())
		{
			return ApplyPausedStateAsync(paused);
		}

		// Overlay code may set IsPaused off the UI thread; host hide/blur must run on it.
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Dispatcher.UIThread.Post(async () =>
		{
			try
			{
				await ApplyPausedStateAsync(paused).ConfigureAwait(true);
				completion.TrySetResult();
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
			}
		});
		return completion.Task;
	}

	private void ClearPlaceholderBitmap()
	{
		Effect = null;
		_underlay.Effect = null;
		_placeholderImage.Effect = null;
		_placeholderImage.Source = null;
		_placeholderImage.IsVisible = false;
		Background = null;
		_placeholderBitmap?.Dispose();
		_placeholderBitmap = null;
	}

	/// <summary>
	/// Puts the native host back on top for live mode (HWND airspace above Cornerstone).
	/// </summary>
	private void EnsureHostAbovePlaceholder()
	{
		if (!IsNestedHostInTree)
		{
			return;
		}

		if ((Children.Count > 0) && ReferenceEquals(Children[^1], _nativeHost))
		{
			return;
		}

		Children.Remove(_nativeHost);
		Children.Add(_nativeHost);
	}

	/// <summary>
	/// Puts the snapshot above the native host in Cornerstone Z-order so when the HWND is
	/// hidden we do not flash an empty host rect that still covers the underlay sibling.
	/// </summary>
	private void EnsurePlaceholderAboveHost()
	{
		if ((Children.Count > 0) && ReferenceEquals(Children[^1], _underlay))
		{
			return;
		}

		Children.Remove(_underlay);
		Children.Add(_underlay);
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
	/// Hides the native surface via Cornerstone <see cref="NativeControlHost" /> visibility only.
	/// Snapshot must already be applied and stacked above the host.
	/// </summary>
	/// <remarks>
	/// On Windows, Cornerstone attaches native views under a holder HWND and uses HideWithSize /
	/// ShowInBounds. Calling ShowWindow on the child ourselves races that and flashes an
	/// empty host. So we only toggle the nested host IsVisible (→ HideWithSize)
	/// and keep the adapter flag for HandleResize / warm-underlay gating.
	/// </remarks>
	private void HideNativeSurface()
	{
		if (IsNestedHostInTree)
		{
			EnsurePlaceholderAboveHost();
			UpdateLayout();
		}

		GetSurface()?.SetNativeSurfaceVisible(false);
		if (IsNestedHostInTree)
		{
			// HideWithSize — do not destroy attachment; avoids recreate flash on resume.
			_nativeHost.IsVisible = false;
		}

		_boundsWhenPaused = Bounds.Size;
	}

	private void NotifyNativeHostCreated()
	{
		_nativeHostReadyCompletion.TrySetResult();

		// If IsPaused was set before the host was ready, apply once ready.
		if (IsPaused)
		{
			_pauseApplyTask = BeginApplyPausedState(true);
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
			if (!IsNestedHostInTree)
			{
				_ = operationId;
				ApplySolidFallbackUnderlay();
				GetSurface()?.SetNativeSurfaceVisible(false);
				_boundsWhenPaused = Bounds.Size;
				UpdatePlaceholderBlur();
				return;
			}

			// Invalidate warm-underlay applies so a capture that started before this
			// pause cannot overwrite the freeze-frame we are about to take. Do not
			// time out the pause capture: WebView2 allows one CapturePreview at a
			// time, and a warm capture holding the gate used to make us hide on a
			// solid fill (blank first pause).
			_underlayRefreshId++;

			// Reapply after a failed hide (or a hide without pixels) needs the HWND
			// back so CapturePreview has a surface.
			if (!_nativeHost.IsVisible && (_placeholderBitmap == null))
			{
				EnsureHostAbovePlaceholder();
				GetSurface()?.SetNativeSurfaceVisible(true);
				_nativeHost.IsVisible = true;
				await WaitForUnderlayPaintedAsync().ConfigureAwait(true);
				if (operationId != _pauseOperationId)
				{
					return;
				}
			}

			NativeSurfaceSnapshot snapshot = null;
			for (var attempt = 0; attempt < 3; attempt++)
			{
				if (operationId != _pauseOperationId)
				{
					return;
				}

				snapshot = await TryCaptureSnapshotForPauseAsync().ConfigureAwait(true);
				if (snapshot is { Success: true, PngBytes: { Length: > 0 } })
				{
					break;
				}
			}

			if (operationId != _pauseOperationId)
			{
				return;
			}

			if (snapshot is { Success: true, PngBytes: { Length: > 0 } })
			{
				var next = ImageConverters.BytesToBitmap(snapshot.PngBytes);
				ApplyUnderlayBitmap(next);
			}

			// Never hide the HWND without a real snapshot. A solid fallback looks
			// like a blank map and cannot be used as an ink underlay.
			if (_placeholderBitmap == null)
			{
				return;
			}

			// Blur while the HWND still covers the underlay, then wait a paint so the
			// first frame after hide is already blurred (not a sharp leftover freeze-frame).
			UpdatePlaceholderBlur();
			await WaitForUnderlayPaintedAsync().ConfigureAwait(true);
			if (operationId != _pauseOperationId)
			{
				return;
			}

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

		// Show the host first so ShowInBounds does not race a still-hidden child HWND.
		// Then drop the last pause freeze-frame; leaving it unblurred made the next pause
		// flash that stale sharp snapshot the instant the HWND hid.
		Effect = null;
		_underlay.Effect = null;
		_placeholderImage.Effect = null;
		_fallbackBackground.IsVisible = false;
		EnsureHostAbovePlaceholder();
		GetSurface()?.SetNativeSurfaceVisible(IsNestedHostInTree || IsVisible);
		if (IsNestedHostInTree)
		{
			_nativeHost.IsVisible = true;
			_nativeHost.TryUpdateNativeControlPosition();
		}

		// Re-apply size after the native host is shown again.
		if (Bounds is { Width: > 0, Height: > 0 })
		{
			var scaling = (float) (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
			GetSurface()?.HandleResize((int) (Bounds.Width * scaling), (int) (Bounds.Height * scaling), scaling);
		}

		ClearPlaceholderBitmap();

		// Fresh warm underlay for the next pause (after the live surface is back).
		_ = RefreshUnderlayAsync(true);
	}

	/// <summary>
	/// Fresh freeze-frame before hide. Waits for an in-flight CapturePreview (the
	/// capture gate) instead of failing open into a blank underlay.
	/// </summary>
	private async Task<NativeSurfaceSnapshot> TryCaptureSnapshotForPauseAsync()
	{
		try
		{
			var capture = CaptureSnapshotAsync(NativeSurfaceSnapshotOptions.Default());
			var completed = await Task.WhenAny(capture, Task.Delay(4000)).ConfigureAwait(true);
			if (completed != capture)
			{
				return NativeSurfaceSnapshot.Failed("Snapshot timed out.");
			}

			return await capture.ConfigureAwait(true);
		}
		catch (Exception)
		{
			return NativeSurfaceSnapshot.Failed("Snapshot failed.");
		}
	}

	private void UpdatePlaceholderBlur()
	{
		// Blur only while paused; keep a sharp warm underlay while live.
		// Apply Effect on the underlay Grid (not this host) so NestedNativeHost keeps a
		// valid TransformToVisual. ImageBrush on this.Background is sharp; the Image
		// sibling is stacked above it while paused.
		Effect = null;
		if (IsPaused && BlurWhenPaused && (BlurRadius > 0))
		{
			_underlay.Effect = new BlurEffect { Radius = BlurRadius };
			_placeholderImage.Effect = null;
		}
		else
		{
			_underlay.Effect = null;
			_placeholderImage.Effect = null;
		}
	}

	/// <summary>
	/// Yields through layout and render so a newly assigned underlay bitmap is composited
	/// under the native host before the HWND is hidden.
	/// </summary>
	private static async Task WaitForUnderlayPaintedAsync()
	{
		await Dispatcher.UIThread.InvokeAsync((Action) (() => { }), DispatcherPriority.Render).GetTask().ConfigureAwait(true);
		await Dispatcher.UIThread.InvokeAsync((Action) (() => { }), DispatcherPriority.Render).GetTask().ConfigureAwait(true);
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

			// Surfaces that attach to Cornerstone's default child HWND/view (WebView, media player).
			// Camera (UseDefaultNativeChildWhenNull = false) must not get an empty default child —
			// that would block PreviewView and CameraX would time out waiting for a surface.
			if (_owner.UseDefaultNativeChildWhenNull)
			{
				handle = base.CreateNativeControlCore(parent);
				_owner.NotifyNativeHostCreated();
				return handle;
			}

			// No published handle yet: still need a temporary child for Cornerstone's contract.
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
				_owner.AfterDestroyNativeControlCore(control);
			}
		}

		#endregion
	}

	#endregion
}