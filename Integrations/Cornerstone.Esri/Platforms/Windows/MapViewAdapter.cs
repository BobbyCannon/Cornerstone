#region References

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using EsriMapView = Esri.ArcGISRuntime.UI.Controls.MapView;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri.Platforms.Windows;

[SourceReflection]
[SupportedOSPlatform("Windows")]
internal class MapViewAdapter : CornerstoneObject, IMapViewAdapter, IDisposable
{
	#region Constants

	private const int SwHide = 0;
	private const int SwShow = 5;
	private const uint SwpHideWindow = 0x0080;
	private const uint SwpNoMove = 0x0002;
	private const uint SwpNoSize = 0x0001;
	private const uint SwpNoZOrder = 0x0004;
	private const uint SwpShowWindow = 0x0040;

	#endregion

	#region Fields

	private bool _disposed;
	private readonly ElementHost _elementHost;
	private readonly EsriMapView _mapView;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public MapViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		_mapView = new EsriMapView();
		_elementHost = new ElementHost
		{
			Child = _mapView,
			Dock = System.Windows.Forms.DockStyle.Fill
		};
		PlatformHandle = new PlatformHandle(_elementHost.Handle, "HWND");
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
		_elementHost.Child = null;
		_elementHost.Dispose();
	}

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return _mapView.GetCurrentViewpoint(viewpointType);
	}

	public void HandleResize(int width, int height, float scaling)
	{
		if (!IsNativeSurfaceVisible)
		{
			return;
		}

		SetWindowPos(_elementHost.Handle, IntPtr.Zero, 0, 0, Math.Max(0, width), Math.Max(0, height), SwpNoZOrder);
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		return _mapView.IdentifyLayersAsync(new System.Windows.Point(screenX, screenY), tolerance, returnPopupsOnly);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_elementHost.Visible = visible;
		var hwnd = _elementHost.Handle;
		if (hwnd == IntPtr.Zero)
		{
			return;
		}

		if (visible)
		{
			ShowWindow(hwnd, SwShow);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpShowWindow);
		}
		else
		{
			ShowWindow(hwnd, SwHide);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoZOrder | SwpHideWindow);
		}
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

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	#endregion
}
