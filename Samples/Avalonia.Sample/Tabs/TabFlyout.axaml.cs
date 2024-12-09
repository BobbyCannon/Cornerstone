#region References

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabFlyout : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Flyout";

	#endregion

	#region Constructors

	public TabFlyout()
	{
		InitializeComponent();

		var afp = this.FindControl<Panel>("AttachedFlyoutPanel");
		if (afp != null)
		{
			afp.DoubleTapped += Afp_DoubleTapped;
		}
	}

	#endregion

	#region Methods

	private void Afp_DoubleTapped(object sender, RoutedEventArgs e)
	{
		if (sender is Panel p)
		{
			FlyoutBase.ShowAttachedFlyout(p);
		}
	}

	#endregion
}