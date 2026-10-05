#region References

using System.Windows;

#endregion

namespace Cornerstone.VisualStudio.Views;

/// <summary>
/// Older name for <see cref="VsControlStyles"/>.
/// </summary>
public static class VsTheme
{
	#region Fields

	public static readonly DependencyProperty UseVsThemeProperty;

	#endregion

	#region Constructors

	static VsTheme()
	{
		UseVsThemeProperty = DependencyProperty.RegisterAttached(
			"UseVsTheme",
			typeof(bool),
			typeof(VsTheme),
			new PropertyMetadata(false, OnUseVsThemeChanged));
	}

	#endregion

	#region Methods

	public static bool GetUseVsTheme(UIElement element)
	{
		return VsControlStyles.GetUseVsControlStyles(element);
	}

	public static void SetUseVsTheme(UIElement element, bool value)
	{
		VsControlStyles.SetUseVsControlStyles(element, value);
	}

	private static void OnUseVsThemeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		VsControlStyles.SetUseVsControlStyles(sender, (bool) args.NewValue);
	}

	#endregion
}
