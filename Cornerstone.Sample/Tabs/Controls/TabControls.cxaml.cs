#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Sample.Tabs.Inputs;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControls : UserControl
{
	#region Constants

	public const string HeaderName = "Controls";

	#endregion

	#region Constructors

	public TabControls() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabControls(IRuntimeInformation runtimeInformation)
	{
		IsDockingAvailable = runtimeInformation.DevicePlatform == DevicePlatform.Windows;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool IsDockingAvailable { get; }

	#endregion

	#region Methods

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenButtons(object sender, RoutedEventArgs e)
	{
		Open(TabControlsButtons.HeaderName, new TabControlsButtons());
	}

	private void OpenCharts(object sender, RoutedEventArgs e)
	{
		Open(TabCharts.HeaderName, new TabCharts());
	}

	private void OpenDocking(object sender, RoutedEventArgs e)
	{
		Open(TabDockingManager.HeaderName, new TabDockingManager());
	}

	private void OpenGrids(object sender, RoutedEventArgs e)
	{
		Open(TabGrids.HeaderName, new TabGrids());
	}

	private void OpenInk(object sender, RoutedEventArgs e)
	{
		Open(TabInkCanvas.HeaderName, new TabInkCanvas());
	}

	private void OpenMarkdown(object sender, RoutedEventArgs e)
	{
		Open(TabMarkdownView.HeaderName, new TabMarkdownView());
	}

	private void OpenMenus(object sender, RoutedEventArgs e)
	{
		Open(TabControlsMenus.HeaderName, new TabControlsMenus());
	}

	private void OpenNumberBox(object sender, RoutedEventArgs e)
	{
		Open(TabInputsNumberBox.HeaderName, new TabInputsNumberBox());
	}

	private void OpenProgress(object sender, RoutedEventArgs e)
	{
		Open(TabControlsProgress.HeaderName, new TabControlsProgress());
	}

	private void OpenShortcut(object sender, RoutedEventArgs e)
	{
		Open(TabInputsShortcut.HeaderName, new TabInputsShortcut());
	}

	private void OpenShortcuts(object sender, RoutedEventArgs e)
	{
		Open(TabControlsShortcuts.HeaderName, new TabControlsShortcuts());
	}

	private void OpenTabs(object sender, RoutedEventArgs e)
	{
		Open(TabControlsTabs.HeaderName, new TabControlsTabs());
	}

	private void OpenTerminal(object sender, RoutedEventArgs e)
	{
		Open(TabTerminal.HeaderName, new TabTerminal());
	}

	private void OpenTextEditor(object sender, RoutedEventArgs e)
	{
		Open(TabTextEditor.HeaderName, new TabTextEditor());
	}

	private void OpenTokenFilter(object sender, RoutedEventArgs e)
	{
		Open(TabTokenTextFilter.HeaderName, new TabTokenTextFilter());
	}

	private void OpenTree(object sender, RoutedEventArgs e)
	{
		Open(TabTreeDataGrid.HeaderName, new TabTreeDataGrid());
	}

	#endregion
}