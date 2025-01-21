#region References

using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabToolTip : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "ToolTip";

	#endregion

	#region Constructors

	public TabToolTip()
	{
		DataContext = this;
		InitializeComponent();
	}

	#endregion
}