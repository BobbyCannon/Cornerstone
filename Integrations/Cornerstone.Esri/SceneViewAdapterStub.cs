#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri;

public class SceneViewAdapterStub : ISceneViewAdapter
{
	#region Constructors

	public SceneViewAdapterStub()
	{
		IsNativeSurfaceVisible = true;
		PlatformHandle = null;
	}

	#endregion

	#region Properties

	public Camera Camera { get; set; }

	public GraphicsOverlayCollection GraphicsOverlays => null;

	public bool IsNativeSurfaceVisible { get; private set; }

	public IPlatformHandle PlatformHandle { get; }

	public Scene Scene { get; set; }

	#endregion

	#region Methods

	public Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		return Task.FromResult(NativeSurfaceSnapshot.Failed("SceneView adapter stub does not support snapshots."));
	}

	public Viewpoint GetCurrentViewpoint(ViewpointType viewpointType)
	{
		return null;
	}

	public void HandleResize(int width, int height, float scaling)
	{
	}

	public Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly)
	{
		return Task.FromResult((IReadOnlyList<IdentifyLayerResult>) []);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
	}

	public void SetViewpoint(Viewpoint viewpoint)
	{
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint)
	{
		return Task.FromResult(false);
	}

	public Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration)
	{
		return Task.FromResult(false);
	}

	protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		PropertyChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}
