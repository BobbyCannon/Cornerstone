#region References

using System;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

[SourceReflection]
public partial class MenuItemView : SpeedyTree<MenuItemView>
{
	#region Properties

	public ICommand Command { get; set; }

	public object CommandParameter { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string InputGesture { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsParent { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Name { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Order { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Guid? ParentSyncId { get; set; }

	#endregion
}