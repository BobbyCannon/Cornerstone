#region References

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.Controls;

[DoNotNotify]
public class MenuItemWithCheckBox : MenuItem
{
	#region Constructors

	public MenuItemWithCheckBox()
	{
		var checkbox = new CheckBox { DataContext = this };
		checkbox.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(IsChecked), BindingMode.TwoWay));
		
		Icon = checkbox;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnClick(RoutedEventArgs e)
	{
		IsChecked = !IsChecked;
		base.OnClick(e);
	}

	#endregion
}