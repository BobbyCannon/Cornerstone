#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;

#endregion

namespace Cornerstone.Esri;

/// <summary>
/// Pass-through 3D scene surface. GIS types are Esri's; this does not wrap Scene, layers, or camera.
/// </summary>
public interface ISceneView : INotifyPropertyChanged
{
	#region Properties

	Camera Camera { get; set; }

	GraphicsOverlayCollection GraphicsOverlays { get; }

	Scene Scene { get; set; }

	#endregion

	#region Methods

	Viewpoint GetCurrentViewpoint(ViewpointType viewpointType);

	Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly);

	void SetViewpoint(Viewpoint viewpoint);

	Task<bool> SetViewpointAsync(Viewpoint viewpoint);

	Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration);

	#endregion
}
