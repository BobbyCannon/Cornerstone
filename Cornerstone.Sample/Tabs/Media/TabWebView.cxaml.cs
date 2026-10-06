#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Media;

[SourceReflection]
public partial class TabWebView : UserControl
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
		base.OnAttachedToVisualTree(e);
		WebView.Navigate(Uri);
	}

	#endregion
}