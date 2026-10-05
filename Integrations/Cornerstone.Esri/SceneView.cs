#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Runtime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri;

/// <summary>
/// Cross-platform Esri SceneView island. Native child is platform Esri SceneView; GIS types stay Esri.
/// </summary>
public class SceneView : PausableNativeHost, ISceneView
{
	#region Fields

	public static readonly StyledProperty<Camera> CameraProperty =
		PresentationProperty.Register<SceneView, Camera>(nameof(Camera));

	public static readonly StyledProperty<Scene> SceneProperty =
		PresentationProperty.Register<SceneView, Scene>(nameof(Scene));

	public static readonly StyledProperty<ISceneViewAdapter> SceneViewAdapterProperty =
		PresentationProperty.Register<SceneView, ISceneViewAdapter>(nameof(SceneViewAdapter));

	private ISceneViewAdapter _subscribedAdapter;

	#endregion

	#region Constructors

	public SceneView()
	{
	}

	#endregion

	#region Properties

	public Camera Camera
	{
		get => GetValue(CameraProperty);
		set => SetValue(CameraProperty, value);
	}

	public GraphicsOverlayCollection GraphicsOverlays => EnsureAdapter()?.GraphicsOverlays;

	public Scene Scene
	{
		get => GetValue(SceneProperty);
		set => SetValue(SceneProperty, value);
	}

	public ISceneViewAdapter SceneViewAdapter
	{
		get => GetValue(SceneViewAdapterProperty);
		set => SetValue(SceneViewAdapterProperty, value);
	}

	#endregion

	#region Methods

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return EnsureAdapter()?.GetCurrentViewpoint(viewpointType);
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		var adapter = EnsureAdapter();
		if (adapter == null)
		{
			return Task.FromResult((IReadOnlyList<IdentifyLayerResult>) []);
		}

		return adapter.IdentifyLayersAsync(screenX, screenY, tolerance, returnPopupsOnly);
	}

	public void SetViewpoint(Viewpoint viewpoint)
	{
		EnsureAdapter()?.SetViewpoint(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint)
	{
		var adapter = EnsureAdapter();
		if (adapter == null)
		{
			return Task.FromResult(false);
		}

		return adapter.SetViewpointAsync(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration)
	{
		var adapter = EnsureAdapter();
		if (adapter == null)
		{
			return Task.FromResult(false);
		}

		return adapter.SetViewpointAsync(viewpoint, duration);
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
			if (SceneViewAdapter is IDisposable disposable)
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
		if (change.Property == SceneViewAdapterProperty)
		{
			SubscribeAdapter(change.GetOldValue<ISceneViewAdapter>(), change.GetNewValue<ISceneViewAdapter>());
			PushToAdapter();
		}
		else if ((change.Property == SceneProperty) || (change.Property == CameraProperty))
		{
			PushToAdapter();
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

	private ISceneViewAdapter EnsureAdapter()
	{
		if (SceneViewAdapter != null)
		{
			return SceneViewAdapter;
		}

		ISceneViewAdapter adapter;
		try
		{
			adapter = AppBootstrap.GetInstance<ISceneViewAdapter>();
		}
		catch
		{
			adapter = new SceneViewAdapterStub();
		}

		SceneViewAdapter = adapter;
		return adapter;
	}

	private void PushToAdapter()
	{
		var adapter = SceneViewAdapter;
		if (adapter == null)
		{
			return;
		}

		adapter.Scene = Scene;
		if (Camera != null)
		{
			adapter.Camera = Camera;
		}
	}

	private void SubscribeAdapter(ISceneViewAdapter previous, ISceneViewAdapter next)
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
			next.Scene = Scene;
			if (Camera != null)
			{
				next.Camera = Camera;
			}
		}
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
	}

	#endregion
}
