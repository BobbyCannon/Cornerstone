#region References

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabWebView : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "WebView";

	#endregion

	#region Constructors

	public TabWebView() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public TabWebView(AppViewModel viewModel)
	{
		Uri = "https://github.com/BobbyCannon/Cornerstone";
		HtmlContent = "<html><body style='font-family:sans-serif;padding:1rem'><h1>WebView</h1><p>HTML content mode</p></body></html>";
		ViewModel = viewModel;

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string HtmlContent { get; set; }

	[Notify]
	public partial string Uri { get; set; }

	public AppViewModel ViewModel { get; }

	#endregion

	#region Methods

	[RelayCommand]
	public void Refresh()
	{
		WebView.Navigate(Uri);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
		base.OnAttachedToVisualTree(e);
		WebView.Navigate(Uri);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
		base.OnDetachedFromVisualTree(e);
	}

	private void ResumeOnClick(object sender, RoutedEventArgs e)
	{
		WebView.IsPaused = false;
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.NavigationMenuIsOpen):
			{
				WebView.IsPaused = ViewModel.NavigationMenuIsOpen
					&& ViewModel.NavigationMenuDisplayMode
						is SplitViewDisplayMode.Overlay
						or SplitViewDisplayMode.CompactOverlay;
				break;
			}
		}
	}

	#endregion
}