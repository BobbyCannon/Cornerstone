#region References

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

#endregion

namespace Sample.Client.Maui;

public partial class AppShell
{
	#region Constructors

	public AppShell(MauiViewManager manager)
	{
		InitializeComponent();

		BindingContext = manager;
		ViewManager = manager;

		if (Application.Current != null)
		{
			Application.Current.UserAppTheme = AppTheme.Dark;
		}
	}

	#endregion

	#region Properties

	public MauiViewManager ViewManager { get; }

	#endregion
}