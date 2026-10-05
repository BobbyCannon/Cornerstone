#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;

#endregion

namespace Cornerstone.Navigator.Views;

public partial class MainView : UserControl
{
	#region Constructors

	public MainView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (DataContext is AppViewModel viewModel
			&& (viewModel.WebView != null)
			&& !WebViewHost.Children.Contains(viewModel.WebView))
		{
			WebViewHost.Children.Add(viewModel.WebView);
		}

		base.OnLoaded(e);
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		if (DataContext is AppViewModel viewModel
			&& (viewModel.WebView != null)
			&& WebViewHost.Children.Contains(viewModel.WebView))
		{
			WebViewHost.Children.Remove(viewModel.WebView);
		}

		base.OnUnloaded(e);
	}

	private void AddressOnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter)
		{
			return;
		}

		if (DataContext is AppViewModel viewModel)
		{
			viewModel.Navigate();
		}
	}

	private void FavoriteScrimOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (DataContext is AppViewModel viewModel)
		{
			viewModel.CloseFavoriteEditorCommand.Execute(null);
		}
	}

	#endregion
}