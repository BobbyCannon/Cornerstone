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
using EsriSceneView = Esri.ArcGISRuntime.UI.Controls.SceneView;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri.Platforms.iOS;

[SourceReflection]
internal class SceneViewAdapter : CornerstoneObject, ISceneViewAdapter, IDisposable
{
	#region Fields

	private bool _disposed;
	private readonly EsriSceneView _sceneView;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public SceneViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		_sceneView = new EsriSceneView
		{
			AutoresizingMask = UIViewAutoresizing.None
		};
		PlatformHandle = new UIViewControlHandle(_sceneView);
	}

	#endregion

	#region Properties

	public Camera Camera
	{
		get => _sceneView.Camera;
		set
		{
			if (value != null)
			{
				_sceneView.SetViewpointCamera(value);
			}
		}
	}

	public GraphicsOverlayCollection GraphicsOverlays => _sceneView.GraphicsOverlays;

	public bool IsNativeSurfaceVisible { get; private set; }

	public IPlatformHandle PlatformHandle { get; }

	public Scene Scene
	{
		get => _sceneView.Scene;
		set => _sceneView.Scene = value;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_sceneView.RemoveFromSuperview();
		_sceneView.Dispose();
	}

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return _sceneView.GetCurrentViewpoint(viewpointType);
	}

	public void HandleResize(int width, int height, float scaling)
	{
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		return _sceneView.IdentifyLayersAsync(new CGPoint(screenX, screenY), tolerance, returnPopupsOnly);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_sceneView.Hidden = !visible;
	}

	public void SetViewpoint(Viewpoint viewpoint)
	{
		_sceneView.SetViewpoint(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint)
	{
		return _sceneView.SetViewpointAsync(viewpoint);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration)
	{
		return _sceneView.SetViewpointAsync(viewpoint, duration);
	}

	#endregion
}
