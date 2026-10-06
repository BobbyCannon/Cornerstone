#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Grid that hosts one native surface under Cornerstone composition.
/// </summary>
/// <remarks>
/// Subclasses own adapter lifetime and implement <see cref="CreateNativeControlCore" />,
/// <see cref="DestroyNativeControlCore" />, <see cref="AfterDestroyNativeControlCore" />,
/// and <see cref="GetSurface" />.
/// </remarks>
public abstract class NativeSurfaceHost : Grid, IDisposable
{
	#region Fields

	private readonly NestedNativeHost _nativeHost;
	private TaskCompletionSource _nativeHostReadyCompletion;

	#endregion

	#region Constructors

	protected NativeSurfaceHost()
	{
		_nativeHostReadyCompletion = new TaskCompletionSource();
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
	/// Whether the native surface is currently painting.
	/// </summary>
	public bool IsNativeSurfaceVisible => GetSurface()?.IsNativeSurfaceVisible ?? true;

	/// <summary>
	/// Nested Cornerstone <see cref="NativeControlHost" /> that owns the platform child handle.
	/// </summary>
	public NativeControlHost NativeHost => _nativeHost;

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

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Completes when Cornerstone has created the native host at least once for this instance.
	/// </summary>
	public Task WaitForNativeHost()
	{
		return _nativeHostReadyCompletion.Task;
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
	}

	/// <summary>
	/// Platform surface used for visibility and resize. May be null before first create.
	/// </summary>
	protected abstract INativeSurface GetSurface();

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
				Console.WriteLine("NativeSurfaceHost overlay attach failed: " + ex);
			}
		}, DispatcherPriority.Background);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == IsVisibleProperty) && !IsNestedHostInTree)
		{
			GetSurface()?.SetNativeSurfaceVisible(IsVisible);
		}

		if (change.Property == BoundsProperty)
		{
			var newValue = change.GetNewValue<Rect>();
			var surface = GetSurface();
			if (surface?.IsNativeSurfaceVisible != false)
			{
				var scaling = (float) (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
				surface?.HandleResize((int) (newValue.Width * scaling), (int) (newValue.Height * scaling), scaling);
			}
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

	private void NotifyNativeHostCreated()
	{
		_nativeHostReadyCompletion.TrySetResult();
	}

	#endregion

	#region Classes

	/// <summary>
	/// Thin native host so the owner can supply the platform child.
	/// </summary>
	private sealed class NestedNativeHost : NativeControlHost
	{
		#region Fields

		private readonly NativeSurfaceHost _owner;

		#endregion

		#region Constructors

		public NestedNativeHost(NativeSurfaceHost owner)
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
