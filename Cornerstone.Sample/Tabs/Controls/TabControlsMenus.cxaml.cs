#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControlsMenus : UserControl
{
	#region Constants

	public const string HeaderName = "Menus";

	#endregion

	#region Constructors

	public TabControlsMenus()
	{
		InitializeComponent();
	}

	#endregion
}