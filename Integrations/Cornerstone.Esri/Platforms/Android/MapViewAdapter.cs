#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.Android;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Platforms.Android;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using EsriMapView = Esri.ArcGISRuntime.UI.Controls.MapView;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri.Platforms.Android;

[SourceReflection]
internal class MapViewAdapter : CornerstoneObject, IMapViewAdapter, IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly EsriMapView _mapView;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MapViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		var parentContext = (global::Android.Content.Context) AndroidHost.Activity
			?? global::Android.App.Application.Context;
		_mapView = new EsriMapView(parentContext);
		PlatformHandle = new AndroidViewControlHandle(_mapView);
	}

	#endregion

	#region Properties

	public GraphicsOverlayCollection GraphicsOverlays => _mapView.GraphicsOverlays;

	public bool IsNativeSurfaceVisible { get; private set; }

	public LocationDisplay LocationDisplay => _mapView.LocationDisplay;

	public Map Map
	{
		get => _mapView.Map;
		set => _mapView.Map = value;
	}

	public IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_mapView.Dispose();
	}

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return _mapView.GetCurrentViewpoint(viewpointType);
	}

	public void HandleResize(int width, int height, float scaling)
	{
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		return _mapView.IdentifyLayersAsync(new global::Android.Graphics.PointF((float) screenX, (float) screenY), tolerance, returnPopupsOnly);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_mapView.Visibility = visible
			? global::Android.Views.ViewStates.Visible
			: global::Android.Views.ViewStates.Invisible;
	}

	public void SetViewpoint(Viewpoint viewpoint)
	{
		_mapView.SetViewpoint(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint)
	{
		return _mapView.SetViewpointAsync(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration)
	{
		return _mapView.SetViewpointAsync(viewpoint, duration);
	}

	#endregion
}
