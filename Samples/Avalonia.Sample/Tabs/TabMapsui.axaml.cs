#region References

using Cornerstone.Avalonia.Controls;
using Mapsui.Tiling;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabMapsui : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Mapsui";

	#endregion

	#region Constructors

	public TabMapsui()
	{
		InitializeComponent();

		MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		MapControl.Map.Navigator.RotationLock = false;
	}

	#endregion
}