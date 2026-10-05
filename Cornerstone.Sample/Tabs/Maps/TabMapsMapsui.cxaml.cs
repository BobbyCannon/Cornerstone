#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Mapsui.Layers;
using Mapsui.Tiling;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Maps;

[SourceReflection]
public partial class TabMapsMapsui : UserControl
{
	#region Constants

	public const string HeaderName = "Mapsui";

	#endregion

	#region Constructors

	public TabMapsMapsui()
	{
		DataContext = this;
		InitializeComponent();
		Loaded += OnLoaded;
	}

	#endregion

	#region Methods

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Loaded -= OnLoaded;
		if (Enumerable.Any<ILayer>(MapHost.Map.Layers, layer => layer.Name == "OpenStreetMap"))
		{
			return;
		}

		MapHost.Map.Layers.Add(OpenStreetMap.CreateTileLayer("Cornerstone.Sample"));
		MapHost.Map.Navigator.ZoomToPanBounds();
	}

	#endregion
}