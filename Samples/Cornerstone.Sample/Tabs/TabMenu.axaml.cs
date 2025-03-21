#region References

using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabMenu : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Menu";

	#endregion

	#region Constructors

	public TabMenu() : this(null)
	{
	}

	[DependencyInjectionConstructor]
	public TabMenu(IDispatcher dispatcher) : base(dispatcher)
	{
		MenuItems = new SpeedyList<MenuItemData>
		{
			new()
			{
				Name = "File",
				IsParent = true,
				Children =
				{
					new MenuItemData { Name = "Open File" },
					new MenuItemData { Name = "Save File" },
					new MenuItemData { Name = "Close File" }
				}
			},
			new()
			{
				Name = "Options",
				IsParent = true,
				Children =
				{
					new MenuItemData { Name = "Full Screen", InputGesture = "Ctrl+F" },
					new MenuItemData { Name = "Minimize On Close" }
				}
			},
			new() { Name = "Search..." }
		};

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public ISpeedyList<MenuItemData> MenuItems { get; }

	#endregion
}