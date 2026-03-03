#region References

using Avalonia.Controls;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

[SourceReflection]
public partial class TabItemViewModel : Notifiable
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