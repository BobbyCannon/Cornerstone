#region References

using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Controls;

public static class TreeViewItemExtensions
{
	#region Fields

	public static readonly AttachedProperty<bool> IsRefreshingProperty;

	#endregion

	#region Constructors

	static TreeViewItemExtensions()
	{
		IsRefreshingProperty = AvaloniaProperty.RegisterAttached<TreeViewItem, bool>("IsRefreshing", typeof(TreeViewItemExtensions));
	}

	#endregion

	#region Methods

	public static bool GetIsRefreshing(AvaloniaObject element)
	{
		return element.GetValue(IsRefreshingProperty);
	}

	public static void SetIsRefreshing(AvaloniaObject element, bool value)
	{
		element.SetValue(IsRefreshingProperty, value);
	}

	#endregion
}