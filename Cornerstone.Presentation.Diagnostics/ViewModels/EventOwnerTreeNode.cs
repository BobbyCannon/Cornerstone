#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Diagnostics.Views;
using Cornerstone.Presentation.Interactivity;

#endregion

namespace Cornerstone.Presentation.Diagnostics.ViewModels;

public class EventOwnerTreeNode : EventTreeNodeBase
{
	#region Constructors

	public EventOwnerTreeNode(Type type, IEnumerable<RoutedEvent> events, EventsPageViewModel vm)
		: base(null, type.Name)
	{
		Children = new ReadOnlyPresentationList<EventTreeNodeBase>(
			new PresentationList<EventTreeNodeBase>(
				events
					.OrderBy(e => e.Name)
					.Select(EventTreeNodeBase (e) => new EventTreeNode(this, e, vm))
					.ToArray())
		);
		IsExpanded = true;
	}

	#endregion

	#region Properties

	public override bool? IsEnabled
	{
		get => base.IsEnabled;
		set
		{
			if (base.IsEnabled != value)
			{
				base.IsEnabled = value;

				if (_updateChildren && (value != null))
				{
					foreach (var child in Children!)
					{
						try
						{
							child._updateParent = false;
							child.IsEnabled = value;
						}
						finally
						{
							child._updateParent = true;
						}
					}
				}
			}
		}
	}

	#endregion
}