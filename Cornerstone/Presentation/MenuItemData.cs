#region References

using System.Windows.Input;
using Cornerstone.Attributes;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Presentation;

public class MenuItemData : SpeedyTree<MenuItemData>, IMenuItemData
{
	#region Constructors

	public MenuItemData() : this(null)
	{
	}

	public MenuItemData(IDispatcher dispatcher)
		: base(null, dispatcher)
	{
	}

	#endregion

	#region Properties

	[SerializationIgnore]
	public ICommand Command { get; set; }

	[SerializationIgnore]
	public object CommandParameter { get; set; }

	public string InputGesture { get; set; }

	public bool IsParent { get; set; }

	public string Name { get; set; }

	public int Order { get; set; }

	#endregion

	#region Methods

	public override bool CanHaveChildren()
	{
		return IsParent;
	}

	public ISpeedyList GetMenuItems()
	{
		return Children;
	}

	public override string ToString()
	{
		return Name;
	}

	#endregion
}

public interface IMenuItemData
{
	#region Properties

	ICommand Command { get; set; }

	object CommandParameter { get; set; }

	string InputGesture { get; set; }

	bool IsParent { get; set; }

	string Name { get; set; }

	int Order { get; set; }

	#endregion

	#region Methods

	public ISpeedyList GetMenuItems();

	#endregion
}