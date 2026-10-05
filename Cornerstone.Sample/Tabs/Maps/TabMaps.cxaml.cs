#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

#endregion

namespace Cornerstone.Sample.Tabs.Maps;

[SourceReflection]
public partial class TabMaps : UserControl
{
	#region Constants

	public const string HeaderName = "Maps";

	#endregion

	#region Constructors

	public TabMaps() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMaps(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		EsriDescription = IsEsriAvailable
			? "Native ArcGIS MapView island per platform."
			: "Native ArcGIS islands are not available in the browser. Use Mapsui, or run Desktop / Android / iOS.";
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public string EsriDescription { get; }

	public bool IsEsriAvailable => RuntimeInformation.DevicePlatform != DevicePlatform.Browser;

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	private static Control CreateUnavailablePage()
	{
		return new TextBlock
		{
			Margin = new Thickness(16),
			TextWrapping = TextWrapping.Wrap,
			Text = "Esri MapView and SceneView are native islands (Windows, Android, iOS). They cannot run in net10-browser. Use Mapsui on this host, or run Sample.Desktop, Sample.Android, or Sample.iOS."
		};
	}

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenEsri(object sender, RoutedEventArgs e)
	{
		if (!IsEsriAvailable)
		{
			Open(TabMapsEsri.HeaderName, CreateUnavailablePage());
			return;
		}

		try
		{
			Open(TabMapsEsri.HeaderName, new TabMapsEsri());
		}
		catch (Exception exception)
		{
			var message = exception.GetBaseException().Message;
			Open(TabMapsEsri.HeaderName, new TextBlock
			{
				Margin = new Thickness(16),
				TextWrapping = TextWrapping.Wrap,
				Text = message
			});
		}
	}

	private void OpenMapsui(object sender, RoutedEventArgs e)
	{
		Open(TabMapsMapsui.HeaderName, new TabMapsMapsui());
	}

	#endregion
}