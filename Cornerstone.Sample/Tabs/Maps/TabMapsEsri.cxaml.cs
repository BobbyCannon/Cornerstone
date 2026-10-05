#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Location;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone.State;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Maps;

[SourceReflection]
public partial class TabMapsEsri : UserControl
{
	#region Constants

	public const string HeaderName = "Esri";

	private const string ArcGisOnlineServices = "https://server.arcgisonline.com/ArcGIS/rest/services/";
	private const double CurrentLocationScale = 36112;
	private const string OverlayPlaces = "SamplePlaces";
	private const string OverlayRoads = "SampleRoads";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly ILocationProvider _locationProvider;

	#endregion

	#region Constructors

	public TabMapsEsri() : this(
		AppBootstrap.GetInstance<AppSettings>(),
		AppBootstrap.GetInstance<IDateTimeProvider>(),
		AppBootstrap.GetInstance<ILocationProvider>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMapsEsri(AppSettings settings, IDateTimeProvider dateTimeProvider, ILocationProvider locationProvider)
	{
		Settings = settings;
		_dateTimeProvider = dateTimeProvider;
		_locationProvider = locationProvider;
		Basemaps =
		[
			new EsriSampleBasemap("topo", "67372ff42cd145319639a99152b15bc3", false),
			new EsriSampleBasemap("topo-3d", "0560e29930dc4d5ebeb58c635c0909c9", true),
			new EsriSampleBasemap("streets", "55ebf90799fa4a3fa57562700a68c405", false),
			new EsriSampleBasemap("streets-3d", "1754f80e7e6644e28ce0c4d35066e392", true),
			new EsriSampleBasemap("satellite", () => new Basemap(new ArcGISTiledLayer(ServiceUri("World_Imagery")))),
			new EsriSampleBasemap("hybrid", "28f49811a6974659988fd279de5ce39f", false),
			new EsriSampleBasemap("navigation", "c50de463235e4161b206d000587af18b", false),
			new EsriSampleBasemap("navigation-3d", "00a5f468dda941d7bf0b51c144aae3f0", true),
			new EsriSampleBasemap("osm", () => new Basemap(new OpenStreetMapLayer())),
			new EsriSampleBasemap("osm-3d", "1c071fcf8ff2448599b0547116e2de55", true),
			new EsriSampleBasemap("gray", "979c6cc89af9449cbeb5342a439c6a76", false),
			new EsriSampleBasemap("gray-3d", "35cdf329d663403b99df27d6ca5fa38d", true),
			new EsriSampleBasemap("dark-gray", "358ec1e175ea41c3bf5c68f0da11ae2b", false),
			new EsriSampleBasemap("dark-gray-3d", "a8b7322a5fe94002bb0f5e0eeb0c5c18", true),
			new EsriSampleBasemap("oceans", "67ab7f7c535c4687b6518e6d2343e8a2", false),
			new EsriSampleBasemap("outdoor", "2e8a3ccdfd6d42a995b79812b3b0ebc6", false)
		];
		SelectedBasemap = Basemaps[0];
		ShowRoads = false;
		ShowPlaces = false;
		IsNavigatingToLocation = false;
		NavigationStatus = string.Empty;
		DataContext = this;
		InitializeComponent();
		ApplyApiKey();
		Loaded += OnLoaded;
	}

	#endregion

	#region Properties

	public IReadOnlyList<EsriSampleBasemap> Basemaps { get; }

	[Notify]
	public partial bool IsNavigatingToLocation { get; set; }

	[Notify]
	public partial string NavigationStatus { get; set; }

	[Notify]
	public partial EsriSampleBasemap SelectedBasemap { get; set; }

	public AppSettings Settings { get; }

	[Notify]
	public partial bool ShowPlaces { get; set; }

	[Notify]
	public partial bool ShowRoads { get; set; }

	#endregion

	#region Methods

	private void ApiKeyLostFocus(object sender, RoutedEventArgs e)
	{
		ApplyApiKey();
		_ = ApplyLayerAsync();
	}

	private void ApplyApiKey()
	{
		var key = Settings?.EsriApiKey;
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		ArcGISRuntimeEnvironment.ApiKey = key.Trim();
	}

	private async Task ApplyLayerAsync()
	{
		if ((MapHost == null) || (SceneHost == null) || (SelectedBasemap == null))
		{
			return;
		}

		var isScene = SelectedBasemap.IsScene;
		MapHost.IsVisible = !isScene;
		SceneHost.IsVisible = isScene;
		MapHost.IsPaused = isScene;
		SceneHost.IsPaused = !isScene;

		if (isScene)
		{
			SceneHost.Scene = await SelectedBasemap.CreateSceneAsync().ConfigureAwait(true);
			ApplyOverlays(SceneHost.Scene);
			return;
		}

		if (SelectedBasemap.IsWebMap)
		{
			MapHost.Map = await SelectedBasemap.CreateMapAsync().ConfigureAwait(true);
			ApplyOverlays(MapHost.Map);
			return;
		}

		if (MapHost.Map == null)
		{
			MapHost.Map = new Map();
		}

		MapHost.Map.Basemap = SelectedBasemap.CreateBasemap();
		ApplyOverlays(MapHost.Map);
	}

	private void ApplyOverlays(GeoModel model)
	{
		if (model == null)
		{
			return;
		}

		RemoveOverlay(model, OverlayRoads);
		RemoveOverlay(model, OverlayPlaces);
		if (ShowRoads)
		{
			model.OperationalLayers.Add(TiledLayer("Reference/World_Transportation", OverlayRoads));
		}

		if (ShowPlaces)
		{
			model.OperationalLayers.Add(TiledLayer("Reference/World_Boundaries_and_Places", OverlayPlaces));
		}
	}

	private void BasemapSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_ = ApplyLayerAsync();
	}

	private static async Task<PortalItem> CreatePortalItem(string itemId)
	{
		var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(true);
		return await PortalItem.CreateAsync(portal, itemId).ConfigureAwait(true);
	}

	private static async Task<Map> CreateWebMap(string itemId)
	{
		return new Map(await CreatePortalItem(itemId).ConfigureAwait(true));
	}

	private static async Task<Scene> CreateWebScene(string itemId)
	{
		return new Scene(await CreatePortalItem(itemId).ConfigureAwait(true));
	}

	private async Task<MapPoint> GetDeviceLocationAsync()
	{
		try
		{
			var source = new SystemLocationDataSource();
			MapPoint position = null;

			void OnLocationChanged(object sender, global::Esri.ArcGISRuntime.Location.Location location)
			{
				if (location?.Position != null)
				{
					position = location.Position;
				}
			}

			source.LocationChanged += OnLocationChanged;
			await source.StartAsync().ConfigureAwait(true);
			var deadline = _dateTimeProvider.UtcNow.AddSeconds(15);
			while ((position == null) && (_dateTimeProvider.UtcNow < deadline))
			{
				await Task.Delay(200).ConfigureAwait(true);
			}

			source.LocationChanged -= OnLocationChanged;
			await source.StopAsync().ConfigureAwait(true);
			if (position != null)
			{
				return position;
			}
		}
		catch
		{
		}

		var fallback = await _locationProvider.GetCurrentLocationAsync().ConfigureAwait(true);
		if (fallback?.Horizontal?.HasValue != true)
		{
			return null;
		}

		return new MapPoint(fallback.Horizontal.Longitude, fallback.Horizontal.Latitude, SpatialReferences.Wgs84);
	}

	private async void GoToCurrentLocation(object sender, RoutedEventArgs e)
	{
		if (IsNavigatingToLocation)
		{
			return;
		}

		IsNavigatingToLocation = true;
		try
		{
			NavigationStatus = "Finding your location…";
			var point = await GetDeviceLocationAsync().ConfigureAwait(true);
			if (point == null)
			{
				NavigationStatus = "Could not find your location.";
				await Task.Delay(1500).ConfigureAwait(true);
				return;
			}

			NavigationStatus = "Flying you there…";
			var viewpoint = new Viewpoint(point, CurrentLocationScale);
			if (SelectedBasemap?.IsScene == true)
			{
				await SceneHost.SetViewpointAsync(viewpoint).ConfigureAwait(true);
				return;
			}

			await MapHost.SetViewpointAsync(viewpoint).ConfigureAwait(true);
		}
		finally
		{
			IsNavigatingToLocation = false;
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Loaded -= OnLoaded;
		_ = ApplyLayerAsync();
	}

	private void OverlayToggled(object sender, RoutedEventArgs e)
	{
		if (SelectedBasemap?.IsScene == true)
		{
			ApplyOverlays(SceneHost?.Scene);
			return;
		}

		ApplyOverlays(MapHost?.Map);
	}

	private static void RemoveOverlay(GeoModel model, string overlayId)
	{
		for (var index = model.OperationalLayers.Count - 1; index >= 0; index--)
		{
			if (model.OperationalLayers[index].Id == overlayId)
			{
				model.OperationalLayers.RemoveAt(index);
			}
		}
	}

	private static Uri ServiceUri(string servicePath)
	{
		return new Uri(ArcGisOnlineServices + servicePath + "/MapServer");
	}

	private static ArcGISTiledLayer TiledLayer(string servicePath, string overlayId)
	{
		return new ArcGISTiledLayer(ServiceUri(servicePath))
		{
			Id = overlayId
		};
	}

	#endregion

	#region Classes

	public sealed class EsriSampleBasemap
	{
		#region Fields

		private readonly Func<Basemap> _createBasemap;
		private readonly string _mapItemId;
		private readonly string _sceneItemId;

		#endregion

		#region Constructors

		public EsriSampleBasemap(string name, Func<Basemap> createBasemap)
		{
			Name = name;
			_createBasemap = createBasemap;
			_mapItemId = null;
			_sceneItemId = null;
		}

		public EsriSampleBasemap(string name, string portalItemId, bool isScene)
		{
			Name = name;
			_createBasemap = null;
			_mapItemId = isScene ? null : portalItemId;
			_sceneItemId = isScene ? portalItemId : null;
		}

		#endregion

		#region Properties

		public bool IsScene => _sceneItemId != null;

		public bool IsWebMap => _mapItemId != null;

		public string Name { get; }

		#endregion

		#region Methods

		public Basemap CreateBasemap()
		{
			return _createBasemap();
		}

		public Task<Map> CreateMapAsync()
		{
			return CreateWebMap(_mapItemId);
		}

		public Task<Scene> CreateSceneAsync()
		{
			return CreateWebScene(_sceneItemId);
		}

		public override string ToString()
		{
			return Name;
		}

		#endregion
	}

	#endregion
}