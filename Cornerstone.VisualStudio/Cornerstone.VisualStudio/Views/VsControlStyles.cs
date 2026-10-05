#region References

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

#endregion

namespace Cornerstone.VisualStudio.Views;

/// <summary>
/// Applies Views/Themes/VsControlStyles.xaml so extension UI follows the Visual Studio theme.
/// </summary>
public static class VsControlStyles
{
	#region Fields

	private static readonly Uri DictionaryUri;
	private static readonly DependencyProperty AppliedProperty;
	private static readonly Dictionary<FrameworkElement, ThemeChangedEventHandler> _themeHandlers;
	public static readonly DependencyProperty UseVsControlStylesProperty;

	#endregion

	#region Constructors

	static VsControlStyles()
	{
		DictionaryUri = new Uri(
			"pack://application:,,,/Cornerstone.VisualStudio;component/Views/Themes/VsControlStyles.xaml",
			UriKind.Absolute);
		_themeHandlers = new Dictionary<FrameworkElement, ThemeChangedEventHandler>();
		AppliedProperty = DependencyProperty.RegisterAttached(
			"Applied",
			typeof(bool),
			typeof(VsControlStyles),
			new PropertyMetadata(false));
		UseVsControlStylesProperty = DependencyProperty.RegisterAttached(
			"UseVsControlStyles",
			typeof(bool),
			typeof(VsControlStyles),
			new PropertyMetadata(false, OnUseVsControlStylesChanged));
	}

	#endregion

	#region Methods

	public static bool GetUseVsControlStyles(DependencyObject element)
	{
		return (bool) element.GetValue(UseVsControlStylesProperty);
	}

	public static void SetUseVsControlStyles(DependencyObject element, bool value)
	{
		element.SetValue(UseVsControlStylesProperty, value);
	}

	private static void OnUseVsControlStylesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var element = sender as FrameworkElement;
		if ((element == null) || !(bool) args.NewValue || (bool) element.GetValue(AppliedProperty))
		{
			return;
		}

		element.SetValue(AppliedProperty, true);
		if (element.Resources == null)
		{
			element.Resources = new ResourceDictionary();
		}

		// Shell dialog styles cover controls this dictionary does not restyle.
		// Our dictionary is merged last so ListView, Button, and the rest keep the VsBrush templates.
		try
		{
			ThemedDialogStyleLoader.SetUseDefaultThemedDialogStyles(element, true);
		}
		catch (Exception)
		{
		}

		var styles = new ResourceDictionary();
		styles.Source = DictionaryUri;
		element.Resources.MergedDictionaries.Add(styles);
		ApplyRowHoverBrush(element);
		element.Loaded += OnThemedElementLoaded;
		element.Unloaded += OnThemedElementUnloaded;

		if (element is Control control)
		{
			if (control.ReadLocalValue(Control.BackgroundProperty) == DependencyProperty.UnsetValue)
			{
				control.SetResourceReference(Control.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
			}

			if (control.ReadLocalValue(Control.ForegroundProperty) == DependencyProperty.UnsetValue)
			{
				control.SetResourceReference(Control.ForegroundProperty, VsBrushes.ToolWindowTextKey);
			}
		}

		if (element.IsInitialized)
		{
			RefreshImplicitStyles(element);
		}
		else
		{
			element.Initialized += OnElementInitialized;
		}
	}

	private static void OnElementInitialized(object sender, EventArgs args)
	{
		var element = (FrameworkElement) sender;
		element.Initialized -= OnElementInitialized;
		RefreshImplicitStyles(element);
	}

	private static void RefreshImplicitStyles(DependencyObject node)
	{
		var element = node as FrameworkElement;
		if (element != null)
		{
			if (element.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
			{
				element.InvalidateProperty(FrameworkElement.StyleProperty);
			}

			// Scroll bars inside this tree follow the same brushes. The editor view is parented later, so it keeps the shell scroll bars.
			if (element is ScrollViewer)
			{
				ApplyScrollBarStyle(element);
			}
		}

		foreach (var child in LogicalTreeHelper.GetChildren(node))
		{
			var dependencyChild = child as DependencyObject;
			if (dependencyChild != null)
			{
				RefreshImplicitStyles(dependencyChild);
			}
		}
	}

	private static void OnThemedElementLoaded(object sender, RoutedEventArgs args)
	{
		var element = (FrameworkElement) sender;
		// The shell dialog loader merges its list and button styles during Initialized,
		// after our dictionary, and that list hover is the pale blue row.
		PromoteStyleDictionary(element);
		ApplyRowHoverBrush(element);
		ApplyOwnedStyles(element, element);
		if (_themeHandlers.ContainsKey(element))
		{
			return;
		}

		ThemeChangedEventHandler handler = themeArgs => ApplyRowHoverBrush(element);
		_themeHandlers[element] = handler;
		VSColorTheme.ThemeChanged += handler;
	}

	private static void PromoteStyleDictionary(FrameworkElement element)
	{
		ResourceDictionary ours = null;
		foreach (var dictionary in element.Resources.MergedDictionaries)
		{
			if (dictionary.Source == DictionaryUri)
			{
				ours = dictionary;
			}
		}

		if (ours == null)
		{
			return;
		}

		element.Resources.MergedDictionaries.Remove(ours);
		element.Resources.MergedDictionaries.Add(ours);
	}

	private static void ApplyOwnedStyles(FrameworkElement root, DependencyObject node)
	{
		var list = node as ListView;
		if (list != null)
		{
			var listStyle = root.TryFindResource("VsListViewStyle") as Style;
			var itemStyle = root.TryFindResource("VsListViewItemStyle") as Style;
			if (listStyle != null)
			{
				list.Style = listStyle;
			}

			if (itemStyle != null)
			{
				list.ItemContainerStyle = itemStyle;
			}
		}

		var button = node as Button;
		if (button != null)
		{
			var buttonStyle = root.TryFindResource("VsButtonStyle") as Style;
			if (buttonStyle != null)
			{
				button.Style = buttonStyle;
			}
		}

		// Shell combo style keeps the live theme brushes. Our text box style must not paint the inner editor.
		var combo = node as ComboBox;
		if (combo != null)
		{
			combo.SetResourceReference(FrameworkElement.StyleProperty, VsResourceKeys.ThemedDialogComboBoxStyleKey);
			combo.Loaded -= OnComboBoxLoaded;
			combo.Loaded += OnComboBoxLoaded;
			ReleaseComboBoxTextBox(combo);
		}

		foreach (var child in LogicalTreeHelper.GetChildren(node))
		{
			var childObject = child as DependencyObject;
			if (childObject != null)
			{
				ApplyOwnedStyles(root, childObject);
			}
		}
	}

	private static void OnComboBoxLoaded(object sender, RoutedEventArgs args)
	{
		ReleaseComboBoxTextBox((ComboBox) sender);
	}

	private static void ReleaseComboBoxTextBox(ComboBox combo)
	{
		combo.ApplyTemplate();
		if (combo.Template == null)
		{
			return;
		}

		var editable = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
		if ((editable == null) || (editable.ReadLocalValue(FrameworkElement.StyleProperty) != DependencyProperty.UnsetValue))
		{
			return;
		}

		editable.Style = null;
	}

	private static void OnThemedElementUnloaded(object sender, RoutedEventArgs args)
	{
		var element = (FrameworkElement) sender;
		ThemeChangedEventHandler handler;
		if (!_themeHandlers.TryGetValue(element, out handler))
		{
			return;
		}

		VSColorTheme.ThemeChanged -= handler;
		_themeHandlers.Remove(element);
	}

	private static void ApplyRowHoverBrush(FrameworkElement element)
	{
		var background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
		var hover = background.GetBrightness() < 0.5f
			? Color.FromArgb(0x3A, 0xFF, 0xFF, 0xFF)
			: Color.FromArgb(0x18, 0x00, 0x00, 0x00);
		var brush = element.Resources["VsRowHoverBrush"] as SolidColorBrush;
		if ((brush == null) || brush.IsFrozen)
		{
			element.Resources["VsRowHoverBrush"] = new SolidColorBrush(hover);
			return;
		}

		brush.Color = hover;
	}

	private static void ApplyScrollBarStyle(FrameworkElement element)
	{
		var scrollBarStyle = element.TryFindResource("VsScrollBarStyle") as Style;
		if ((scrollBarStyle == null) || element.Resources.Contains(typeof(ScrollBar)))
		{
			return;
		}

		element.Resources.Add(typeof(ScrollBar), new Style(typeof(ScrollBar), scrollBarStyle));
	}

	#endregion
}
