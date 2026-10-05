#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

[SourceReflection]
public partial class TabData : UserControl
{
	#region Constants

	public const string HeaderName = "Data";

	#endregion

	#region Constructors

	public TabData()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenDebounce(object sender, RoutedEventArgs e)
	{
		Open(TabDebounceAndThrottle.HeaderName, new TabDebounceAndThrottle());
	}

	private void OpenSecurityCardReader(object sender, RoutedEventArgs e)
	{
		Open(TabSecurityCardReader.HeaderName, new TabSecurityCardReader());
	}

	private void OpenSpeedyPack(object sender, RoutedEventArgs e)
	{
		Open(TabSpeedyPack.HeaderName, new TabSpeedyPack());
	}

	private void OpenSync(object sender, RoutedEventArgs e)
	{
		Open("Loopback", new TabSync());
	}

	private void OpenSyncMesh(object sender, RoutedEventArgs e)
	{
		Open(TabSyncMesh.HeaderName, new TabSyncMesh());
	}

	#endregion
}
