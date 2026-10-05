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
/// Pass-through map surface. GIS types are Esri's; this does not wrap Map, layers, or geometry.
/// </summary>
public interface IMapView : INotifyPropertyChanged
{
	#region Properties

	GraphicsOverlayCollection GraphicsOverlays { get; }

	LocationDisplay LocationDisplay { get; }

	Map Map { get; set; }

	#endregion

	#region Methods

	Viewpoint GetCurrentViewpoint(ViewpointType viewpointType);

	Task<IReadOnlyList<IdentifyLayerResult>> IdentifyLayersAsync(double screenX, double screenY, double tolerance, bool returnPopupsOnly);

	void SetViewpoint(Viewpoint viewpoint);

	Task<bool> SetViewpointAsync(Viewpoint viewpoint);

	Task<bool> SetViewpointAsync(Viewpoint viewpoint, TimeSpan duration);

	#endregion
}
