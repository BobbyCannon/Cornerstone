#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Sample.ViewModels;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabThemes : CornerstoneUserControl<MainViewModel>
{
	#region Constants

	public const string HeaderName = "Themes";

	#endregion

	#region Constructors

	public TabThemes() : this(DesignModeDependencyProvider.Get<MainViewModel>(), null)
	{
	}

	public TabThemes(MainViewModel viewModel, IDispatcher dispatcher) : base(viewModel, dispatcher)
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void ThemeOnClick(object sender, RoutedEventArgs e)
	{
		if (sender is RadioButton { DataContext: ThemeColor themeColor })
		{
			ViewModel.ApplicationSettings.ThemeColor = themeColor;
		}
	}

	#endregion
}