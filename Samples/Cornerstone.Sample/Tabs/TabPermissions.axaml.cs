#region References

using Avalonia.Interactivity;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabPermissions : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Permissions";

	#endregion

	#region Constructors

	public TabPermissions() : this(ViewDependencyProvider.Get<IPermissions>(), null)
	{
	}

	[DependencyInjectionConstructor]
	public TabPermissions(IPermissions permissions, IDispatcher dispatcher) : base(dispatcher)
	{
		Permissions = permissions;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IPermissions Permissions { get; }

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		Permissions.RefreshAsync();
		base.OnLoaded(e);
	}

	private void RefreshOnClick(object sender, RoutedEventArgs e)
	{
		Permissions.RefreshAsync();
	}

	#endregion
}