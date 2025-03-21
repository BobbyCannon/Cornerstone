using Avalonia.Controls;

namespace Cornerstone.Avalonia.Controls;

public class TabItemViewModel
{
	#region Constructors

	public TabItemViewModel() : this(string.Empty, string.Empty)
	{
	}

	public TabItemViewModel(string tabName, string tabIcon)
	{
		TabName = tabName;
		TabIcon = tabIcon;
	}

	#endregion

	#region Properties

	public string TabIcon { get; set; }

	public string TabName { get; set; }

	public Control TabContent { get; set; }

	#endregion
}