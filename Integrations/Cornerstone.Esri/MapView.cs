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
/// Cross-platform Esri MapView island. Native child is platform Esri MapView; GIS types stay Esri.
/// </summary>
public class MapView : NativeSurfaceHost, IMapView
{
	#region Fields

	public static readonly StyledProperty<IMapViewAdapter> MapViewAdapterProperty =
		PresentationProperty.Register<MapView, IMapViewAdapter>(nameof(MapViewAdapter));

	public static readonly StyledProperty<Map> MapProperty =
		PresentationProperty.Register<MapView, Map>(nameof(Map));

	private IMapViewAdapter _subscribedAdapter;

	#endregion

	#region Constructors

	public MapView()
	{
	}

	#endregion

	#region Properties

	public GraphicsOverlayCollection GraphicsOverlays => EnsureAdapter()?.GraphicsOverlays;

	public LocationDisplay LocationDisplay => EnsureAdapter()?.LocationDisplay;

	public Map Map
	{
		get => GetValue(MapProperty);
		set => SetValue(MapProperty, value);
	}

	public IMapViewAdapter MapViewAdapter
	{
		get => GetValue(MapViewAdapterProperty);
		set => SetValue(MapViewAdapterProperty, value);
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
			if (MapViewAdapter is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		base.Dispose(disposing);
	}

	protected override INativeSurface GetSurface()
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
		if (change.Property == MapViewAdapterProperty)
		{
			SubscribeAdapter(change.GetOldValue<IMapViewAdapter>(), change.GetNewValue<IMapViewAdapter>());
			PushMapToAdapter();
		}
		else if (change.Property == MapProperty)
		{
			PushMapToAdapter();
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

	private IMapViewAdapter EnsureAdapter()
	{
		if (MapViewAdapter != null)
		{
			return MapViewAdapter;
		}

		IMapViewAdapter adapter;
		try
		{
			adapter = AppBootstrap.GetInstance<IMapViewAdapter>();
		}
		catch
		{
			adapter = new MapViewAdapterStub();
		}

		MapViewAdapter = adapter;
		return adapter;
	}

	private void PushMapToAdapter()
	{
		var adapter = MapViewAdapter;
		if (adapter == null)
		{
			return;
		}

		adapter.Map = Map;
	}

	private void SubscribeAdapter(IMapViewAdapter previous, IMapViewAdapter next)
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
			next.Map = Map;
		}
	}

	private void AdapterOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
	}

	#endregion
}
