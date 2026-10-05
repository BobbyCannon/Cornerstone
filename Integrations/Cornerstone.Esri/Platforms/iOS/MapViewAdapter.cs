#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGraphics;
using Cornerstone.Presentation.iOS;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using UIKit;
using EsriMapView = Esri.ArcGISRuntime.UI.Controls.MapView;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri.Platforms.iOS;

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
		_mapView = new EsriMapView
		{
			AutoresizingMask = UIViewAutoresizing.None
		};
		// UIViewControlHandle keeps the existing managed MapView. A raw PlatformHandle
		// makes NativeControlHost wrap the same native object again (ViewHolder) and
		// logs "already has a managed reference".
		PlatformHandle = new UIViewControlHandle(_mapView);
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

	public Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		return MapViewSnapshot.CaptureAsync(_mapView, options);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_mapView.RemoveFromSuperview();
		_mapView.Dispose();
	}

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return _mapView.GetCurrentViewpoint(viewpointType);
	}

	public void HandleResize(int width, int height, float scaling)
	{
		// Position and size are owned by NativeControlHost.ShowInBounds.
		// Do not set Frame (0,0) — that jumps a window-sibling island to the origin.
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		return _mapView.IdentifyLayersAsync(new CGPoint(screenX, screenY), tolerance, returnPopupsOnly);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_mapView.Hidden = !visible;
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
