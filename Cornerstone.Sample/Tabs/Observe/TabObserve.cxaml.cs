#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Observe;

[SourceReflection]
public partial class TabObserve : UserControl
{
	#region Constants

	public const string HeaderName = "Observe";

	#endregion

	#region Constructors

	public TabObserve()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenDateTime(object sender, RoutedEventArgs e)
	{
		Open(TabDateTime.HeaderName, new TabDateTime());
	}

	private void OpenDiagnostics(object sender, RoutedEventArgs e)
	{
		Open(TabDiagnostics.HeaderName, new TabDiagnostics());
	}

	private void OpenProfiler(object sender, RoutedEventArgs e)
	{
		Open(TabProfiler.HeaderName, new TabProfiler());
	}

	private void OpenRuntime(object sender, RoutedEventArgs e)
	{
		Open(TabRuntimeInformation.HeaderName, new TabRuntimeInformation());
	}

	#endregion
}
