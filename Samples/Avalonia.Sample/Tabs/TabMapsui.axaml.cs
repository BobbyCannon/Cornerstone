#region References

using Avalonia.Interactivity;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Location;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
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
		LocationProvider = GetService<ILocationProvider>();
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ILocationProvider LocationProvider { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override async void OnLoaded(RoutedEventArgs e)
	{
		var myLocation = new MyLocationLayer(MapControl.Map);
		MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		MapControl.Map.Layers.Add(myLocation);
		MapControl.Map.Navigator.RotationLock = false;
		MapControl.Map.Navigator.ZoomToLevel(MapControl.Map.Navigator.Resolutions.Count - 2);

		var currentLocation = await LocationProvider.GetCurrentLocationAsync();
		var point = SphericalMercator.FromLonLat(
			currentLocation.HorizontalLocation.Longitude,
			currentLocation.HorizontalLocation.Latitude
		);
		myLocation.UpdateMyLocation(new MPoint(point.x, point.y));

		base.OnLoaded(e);
	}

	#endregion
}