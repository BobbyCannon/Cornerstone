#region References

using System;
using Cornerstone.Presentation.Documentation;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Claims <see cref="TopLevel.BackRequested" /> for the navigator that owns the focused page.
/// Android back, the mouse back button, and Alt+Left all raise that event on the window.
/// Stays internal until <see cref="IsBackTarget" /> is driven by the subscriber list.
/// A public helper that still hardcodes PageNavigator and DocumentationReader would let other controls subscribe without joining the claim walk.
/// </summary>
internal static class SystemBack
{
	#region Methods

	public static void Subscribe(Control control, ref TopLevel topLevel, EventHandler<RoutedEventArgs> handler)
	{
		Unsubscribe(ref topLevel, handler);
		topLevel = TopLevel.GetTopLevel(control);
		if (topLevel is not null)
		{
			topLevel.BackRequested += handler;
		}
	}

	public static bool TryClaim(Control control, RoutedEventArgs e, bool canGoBack)
	{
		if (e.Handled || !canGoBack || !control.IsEffectivelyVisible)
		{
			return false;
		}

		if (HasCloserTarget(control))
		{
			return false;
		}

		var focused = TopLevel.GetTopLevel(control)?.FocusManager?.GetFocusedElement() as Visual;
		if (focused is not null)
		{
			var owner = FindOwner(focused);
			if (owner is not null && (owner != control))
			{
				return false;
			}
		}

		return true;
	}

	public static void Unsubscribe(ref TopLevel topLevel, EventHandler<RoutedEventArgs> handler)
	{
		if (topLevel is null)
		{
			return;
		}

		topLevel.BackRequested -= handler;
		topLevel = null;
	}

	private static bool CanGoBack(Visual visual)
	{
		if (visual is PageNavigator page)
		{
			return page.CanGoBack;
		}

		if (visual is DocumentationReader reader)
		{
			return reader.CanGoBack;
		}

		return false;
	}

	private static Control FindOwner(Visual focused)
	{
		var current = focused;
		while (current is not null)
		{
			if (IsBackTarget(current) && current.IsEffectivelyVisible && CanGoBack(current))
			{
				return (Control) current;
			}

			current = current.GetVisualParent();
		}

		return null;
	}

	private static bool HasCloserTarget(Control control)
	{
		foreach (var descendant in control.GetVisualDescendants())
		{
			if (IsBackTarget(descendant) && descendant.IsEffectivelyVisible && CanGoBack(descendant))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsBackTarget(Visual visual)
	{
		return visual is PageNavigator or DocumentationReader;
	}

	#endregion
}