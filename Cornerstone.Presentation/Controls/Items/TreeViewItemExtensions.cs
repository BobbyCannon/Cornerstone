#region References

using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Items;

public static class TreeViewItemExtensions
{
	#region Fields

	public static readonly AttachedProperty<bool> IsRefreshingProperty;

	#endregion

	#region Constructors

	static TreeViewItemExtensions()
	{
		IsRefreshingProperty = PresentationProperty.RegisterAttached<TreeViewItem, bool>("IsRefreshing", typeof(TreeViewItemExtensions));
	}

	#endregion

	#region Methods

	public static bool GetIsRefreshing(PresentationObject element)
	{
		return element.GetValue(IsRefreshingProperty);
	}

	public static void SetIsRefreshing(PresentationObject element, bool value)
	{
		element.SetValue(IsRefreshingProperty, value);
	}

	#endregion
}