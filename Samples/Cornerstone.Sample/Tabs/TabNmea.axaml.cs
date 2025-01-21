#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Location;
using Cornerstone.Presentation;
using Cornerstone.Protocols.Nmea;
using Cornerstone.Protocols.Nmea.Messages;
using Cornerstone.Runtime;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Manipulations;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabNmea : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "NMEA";

	#endregion

	#region Fields

	private readonly AddInfo _addInfo;
	private readonly BusPointProvider _bus;
	private readonly UdpClient _client;
	private readonly IPEndPoint _clientBroadcast;
	private static readonly SymbolStyle _disableStyle;
	private readonly WritableLayer _editLayer;
	private int _lineNumber;
	private static readonly SymbolStyle _selectedStyle;
	private readonly BackgroundWorker _worker;

	#endregion

	#region Constructors

	public TabNmea()
	{
		_addInfo = new AddInfo();
		_editLayer = CreateEditLayer();
		_lineNumber = 1;
		_worker = new BackgroundWorker();
		_worker.DoWork += WorkerOnDoWork;
		_worker.ProgressChanged += WorkerOnProgressChanged;
		_worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
		_worker.WorkerSupportsCancellation = true;
		_worker.WorkerReportsProgress = true;

		_bus = new BusPointProvider();

		_client = new UdpClient();
		_client.ExclusiveAddressUse = false;
		_client.EnableBroadcast = true;
		_clientBroadcast = new IPEndPoint(IPAddress.Broadcast, 22040);

		Lines = new SpeedyList<SelectionOption<GeometryFeature>>(GetInstance<IDispatcher>());
		LocationProvider = GetInstance<ILocationProvider>();

		DataContext = this;
		InitializeComponent();
	}

	static TabNmea()
	{
		_selectedStyle = new() { Fill = null, Outline = new Pen(Color.Red, 3), Line = new Pen(Color.Red, 3) };
		_disableStyle = new() { Enabled = false };
	}

	#endregion

	#region Properties

	public bool EnableAddingLine { get; set; }

	public bool IsRunning => _worker.IsBusy;

	public SpeedyList<SelectionOption<GeometryFeature>> Lines { get; }

	public ILocationProvider LocationProvider { get; }

	public int Progress { get; private set; }

	public SelectionOption<GeometryFeature> SelectedLine { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(EnableAddingLine) when !EnableAddingLine:
			{
				EndLineEditing(false);
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override async void OnLoaded(RoutedEventArgs e)
	{
		var myLocation = new MyLocationLayer(MapControl.Map);
		MapControl.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
		MapControl.Map.Layers.Add(myLocation);
		MapControl.Map.Layers.Add(_editLayer);
		MapControl.Map.Layers.Add(new AnimatedPointLayer(_bus)
		{
			Name = "Replay",
			Style = new LabelStyle
			{
				BackColor = new Brush(Color.Black),
				ForeColor = Color.White,
				Text = "Bus"
			}
		});
		MapControl.Map.Navigator.RotationLock = false;
		MapControl.Map.Navigator.ZoomToLevel(MapControl.Map.Navigator.Resolutions.Count - 2);

		var currentLocation = await LocationProvider.GetCurrentLocationAsync();
		if (currentLocation != null)
		{
			var point = SphericalMercator.FromLonLat(
				currentLocation.HorizontalLocation.Longitude,
				currentLocation.HorizontalLocation.Latitude
			);
			myLocation.UpdateMyLocation(new MPoint(point.x, point.y));
		}

		base.OnLoaded(e);
	}

	private static WritableLayer CreateEditLayer()
	{
		return new()
		{
			Name = "EditLayer",
			Style = CreateEditLayerStyle(),
			IsMapInfoLayer = true
		};
	}

	private static VectorStyle CreateEditLayerBasicStyle()
	{
		return new()
		{
			Fill = new Brush(new Color(124, 22, 111, 180)),
			Line = new Pen(new Color(124, 22, 111, 180), 3),
			Outline = new Pen(new Color(124, 22, 111, 180), 3)
		};
	}

	private static StyleCollection CreateEditLayerStyle()
	{
		return new()
		{
			Styles =
			{
				CreateEditLayerBasicStyle(),
				CreateSelectedStyle(),
				CreateStyleToShowTheVertices()
			}
		};
	}

	private static ThemeStyle CreateSelectedStyle()
	{
		return new(f => (bool?) f["Selected"] == true ? _selectedStyle : _disableStyle);
	}

	private static SymbolStyle CreateStyleToShowTheVertices()
	{
		return new() { SymbolScale = 0.5 };
	}

	private void EndLineEditing(bool keepLastPoint)
	{
		if ((_addInfo.Vertices.Count > 2) && (_addInfo.Feature != null))
		{
			if (!keepLastPoint)
			{
				// Remove the last vertex, because it is the hover vertex
				_addInfo.Vertices.RemoveAt(_addInfo.Vertices.Count - 1);
			}

			_addInfo.Feature.Geometry = new LineString([.. _addInfo.Vertices]);

			Lines.Add(new SelectionOption<GeometryFeature>(_addInfo.Feature, $"Line {_lineNumber++}"));
			SelectedLine = Lines.Last();
		}

		_addInfo.Feature = null;
		_addInfo.Vertex = null;

		EnableAddingLine = false;
	}

	private void MapControlOnPointerMoved(object sender, PointerEventArgs e)
	{
		if (_addInfo.Vertex == null)
		{
			return;
		}

		var pointerPoint = e.GetCurrentPoint(sender as Visual);
		var screenPosition = new ScreenPosition(pointerPoint.Position.X, pointerPoint.Position.Y);
		var info = MapControl.GetMapInfo(screenPosition);

		_addInfo.Vertex.SetXY(info.WorldPosition);
		_addInfo.Feature?.Modified();
		_editLayer.DataHasChanged();
	}

	private void MapControlOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (!EnableAddingLine)
		{
			return;
		}

		var pointerPoint = e.GetCurrentPoint(sender as Visual);
		var screenPosition = new ScreenPosition(pointerPoint.Position.X, pointerPoint.Position.Y);
		var worldPosition = MapControl.Map.Navigator.Viewport.ScreenToWorld(screenPosition).ToCoordinate();

		if (_addInfo.Feature == null)
		{
			var firstPoint = worldPosition.Copy();
			// Add a second point right away. The second one will be the 'hover' vertex
			var secondPoint = worldPosition.Copy();

			_addInfo.Vertex = secondPoint;
			_addInfo.Feature = new GeometryFeature { Geometry = new LineString([firstPoint, secondPoint]) };
			_addInfo.Vertices = _addInfo.Feature.Geometry.MainCoordinates();

			_editLayer.Add(_addInfo.Feature);
			_editLayer.DataHasChanged();
		}
		else if ((e.ClickCount > 1) || ((e.KeyModifiers & KeyModifiers.Shift) != 0))
		{
			EndLineEditing(true);
		}
		else
		{
			// Set the final position of the 'hover' vertex (that was already part of the geometry)
			_addInfo.Vertex.SetXY(worldPosition);
			_addInfo.Vertex = worldPosition.Copy(); // and create a new hover vertex
			_addInfo.Vertices.Add(_addInfo.Vertex);
			_addInfo.Feature.Geometry = new LineString([.. _addInfo.Vertices]);
			_addInfo.Feature.Modified();
			_editLayer.DataHasChanged();
		}
	}

	private void PlayButtonOnClick(object sender, RoutedEventArgs e)
	{
		_worker.RunWorkerAsync(SelectedLine.Id.Clone());

		OnPropertyChanged(nameof(IsRunning));
	}

	private void RemoveButtonOnClick(object sender, RoutedEventArgs e)
	{
		_editLayer.TryRemove(SelectedLine.Id);
		Lines.Remove(SelectedLine);
		SelectedLine = Lines.FirstOrDefault();
	}

	private void StopButtonOnClick(object sender, RoutedEventArgs e)
	{
		_worker.CancelAsync();
	}

	private static void WorkerOnDoWork(object sender, DoWorkEventArgs e)
	{
		var worker = sender as BackgroundWorker;
		var geometry = e.Argument as GeometryFeature;
		if ((geometry == null) || (worker == null))
		{
			return;
		}

		var line = geometry.Geometry as LineString;
		if (line == null)
		{
			return;
		}

		var index = 0;

		while (!worker.CancellationPending
				&& (index < line.Coordinates.Length))
		{
			worker.ReportProgress(-1, line.Coordinates[index++]);

			var percent = (int) (((double) index / line.Coordinates.Length) * 100);
			worker.ReportProgress(percent);

			Thread.Sleep(1000);
		}
	}

	private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		if (e.ProgressPercentage == -1)
		{
			// Represents a move
			var coordinate = (Coordinate) e.UserState;
			_bus.UpdateLocation(coordinate);

			var location = SphericalMercator.ToLonLat(coordinate.X, coordinate.Y);
			var gga = new GgaMessage
			{
				Prefix = NmeaMessagePrefix.GlobalNavigationSatelliteSystem,
				Time = double.Parse(DateTimeProvider.RealTime.UtcNow.ToString("hhmmss.ff")),
				Latitude = NmeaLocation.FromLatitude((decimal) location.lat),
				Longitude = NmeaLocation.FromLatitude((decimal) location.lon),
				FixQuality = "1",
				NumberOfSatellites = 5,
				HorizontalDilutionOfPrecision = 0.1,
				Altitude = 0,
				AltitudeUnit = "",
				HeightOfGeoid = 0,
				HeightOfGeoidUnit = "",
				SecondsSinceLastUpdateDgps = "",
				StationIdNumberDgps = ""
			};

			gga.Checksum = NmeaMessage.CalculateChecksum(gga.ToString());

			var message = gga.ToString();
			var buffer = Encoding.UTF8.GetBytes(message);

			_client.Send(buffer, buffer.Length, _clientBroadcast);
			return;
		}

		if (e.ProgressPercentage is >= 0 and <= 100)
		{
			// Represents a percent change.
			Progress = e.ProgressPercentage;
		}
	}

	private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		OnPropertyChanged(nameof(IsRunning));
	}

	#endregion

	#region Classes

	public class AddInfo
	{
		#region Properties

		public GeometryFeature Feature { get; set; }
		public Coordinate Vertex { get; set; }
		public IList<Coordinate> Vertices { get; set; }

		#endregion

		#region Methods

		public void Reset()
		{
			Feature = null;
			Vertices = null;
			Vertex = null;
		}

		#endregion
	}

	#endregion
}

public class BusPointProvider : MemoryProvider,
	IDynamic, IDisposable
{
	#region Fields

	private readonly Coordinate _coordinate;

	#endregion

	#region Constructors

	public BusPointProvider()
	{
		_coordinate = new Coordinate();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void DataHasChanged()
	{
		DataChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	public void Dispose()
	{
	}

	/// <inheritdoc />
	public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
	{
		var busFeature = new PointFeature(_coordinate.X, _coordinate.Y) { ["ID"] = "bus" };
		return Task.FromResult((IEnumerable<IFeature>) [busFeature]);
	}

	public void UpdateLocation(Coordinate coordinate)
	{
		_coordinate.X = coordinate.X;
		_coordinate.Y = coordinate.Y;

		DataHasChanged();
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler DataChanged;

	#endregion
}