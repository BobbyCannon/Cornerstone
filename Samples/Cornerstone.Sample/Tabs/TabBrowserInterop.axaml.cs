#region References

using System.Diagnostics;
using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabBrowserInterop : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Browser Interop";

	#endregion

	#region Constructors

	public TabBrowserInterop() : this(ViewDependencyProvider.Get<IBrowserInterop>(), null)
	{
	}

	[DependencyInjectionConstructor]
	public TabBrowserInterop(IBrowserInterop browserInterop, IDispatcher dispatcher) : base(dispatcher)
	{
		BrowserInterop = browserInterop;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IBrowserInterop BrowserInterop { get; }

	#endregion

	#region Methods

	private async void CheckPermissionOnClick(object sender, RoutedEventArgs e)
	{
		PermissionResult.Text = await BrowserInterop.CheckPermission(PermissionType.SelectedValue?.ToString());
	}

	private void CreateElementOnClick(object sender, RoutedEventArgs e)
	{
		var test = BrowserInterop.CreateElement(null, "div");
		Debug.WriteLine(test);
	}

	private void GetWindowLocationOnClick(object sender, RoutedEventArgs e)
	{
		WindowLocation.Text = BrowserInterop.GetWindowLocation();
	}

	private void SetWindowLocationOnClick(object sender, RoutedEventArgs e)
	{
		BrowserInterop.SetWindowLocation(WindowLocation.Text);
	}

	#endregion
}