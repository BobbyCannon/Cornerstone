#region References

using System;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Location;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabLocationProvider : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "LocationProvider";

	#endregion

	#region Fields

	private MyLocationLayer _myLocationLayer;

	#endregion

	#region Constructors

	public TabLocationProvider()
	{
		LocationProvider = GetInstance<ILocationProvider>();

		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public ILocationProvider LocationProvider { get; }

	#endregion

	#region Methods

	public void GetCurrentLocationOnClick(object sender, RoutedEventArgs e)
	{
		RefreshLocation();
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		LocationProvider.Updated += LocationProviderOnUpdated;

		_myLocationLayer = new MyLocationLayer(MapControl.Map);
		MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		MapControl.Map.Layers.Add(_myLocationLayer);
		MapControl.Map.Navigator.RotationLock = false;
		MapControl.Map.Navigator.ZoomToLevel(MapControl.Map.Navigator.Resolutions.Count - 2);

		RefreshLocation();

		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		LocationProvider.Updated -= LocationProviderOnUpdated;
		base.OnUnloaded(e);
	}

	private void LocationProviderOnUpdated(object sender, IUpdateable e)
	{
		var location = (Location.Location) e;
		var point = SphericalMercator.FromLonLat(
			location.HorizontalLocation.Longitude,
			location.HorizontalLocation.Latitude
		);
		_myLocationLayer.UpdateMyLocation(new MPoint(point.x, point.y));
	}

	private async void RefreshLocation()
	{
		await LocationProvider.GetCurrentLocationAsync(TimeSpan.FromSeconds(1));
	}

	#endregion
}