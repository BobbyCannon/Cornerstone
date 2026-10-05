#region References

using Cornerstone.Data;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.Items;

[SourceReflection]
public partial class TabItemViewModel : CornerstoneObject
{
	#region Constructors

	public TabItemViewModel() : this(string.Empty, string.Empty, true)
	{
	}

	public TabItemViewModel(string tabName, string tabIcon, bool showInMenu)
	{
		TabName = tabName;
		TabIcon = tabIcon;
		ShowInMenu = showInMenu;
	}

	#endregion

	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool ShowInMenu { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Control TabContent { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string TabIcon { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string TabName { get; set; }

	#endregion
}