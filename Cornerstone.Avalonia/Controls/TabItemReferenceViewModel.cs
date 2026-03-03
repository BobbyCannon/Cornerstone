#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class TabItemReferenceViewModel
{
	#region Constructors

	public TabItemReferenceViewModel() : this(string.Empty, 0, string.Empty, new Thickness(0), typeof(object), true)
	{
	}

	public TabItemReferenceViewModel(string tabName, int tabOrder, string tabIcon, Thickness iconMargin, Type tabType, bool showInMenu)
	{
		TabName = tabName;
		TabIcon = tabIcon;
		TabIconMargin = iconMargin;
		TabOrder = tabOrder;
		TabTypeName = tabType.ToAssemblyName();
		ShowInMenu = showInMenu;
	}

	#endregion

	#region Properties

	public Control Control { get; set; }

	public bool ShowInMenu { get; set; }

	public string TabIcon { get; set; }

	public Thickness TabIconMargin { get; }

	public string TabName { get; set; }

	public int TabOrder { get; set; }

	public string TabTypeName { get; set; }

	#endregion
}