#region References

using System;
using Avalonia.Controls;
using Avalonia.Data;

#endregion

namespace Avalonia.Diagnostics.Controls;

public class FilterTextBox : TextBox
{
	#region Fields

	public static readonly StyledProperty<bool> UseCaseSensitiveFilterProperty;
	public static readonly StyledProperty<bool> UseRegexFilterProperty;
	public static readonly StyledProperty<bool> UseWholeWordFilterProperty;

	#endregion

	#region Constructors

	public FilterTextBox()
	{
		Classes.Add("filter-text-box");
	}

	static FilterTextBox()
	{
		UseCaseSensitiveFilterProperty = AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseCaseSensitiveFilter),
			defaultBindingMode: BindingMode.TwoWay);
		UseRegexFilterProperty = AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseRegexFilter),
			defaultBindingMode: BindingMode.TwoWay);
		UseWholeWordFilterProperty = AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseWholeWordFilter),
			defaultBindingMode: BindingMode.TwoWay);
	}

	#endregion

	#region Properties

	public bool UseCaseSensitiveFilter
	{
		get => GetValue(UseCaseSensitiveFilterProperty);
		set => SetValue(UseCaseSensitiveFilterProperty, value);
	}

	public bool UseRegexFilter
	{
		get => GetValue(UseRegexFilterProperty);
		set => SetValue(UseRegexFilterProperty, value);
	}

	public bool UseWholeWordFilter
	{
		get => GetValue(UseWholeWordFilterProperty);
		set => SetValue(UseWholeWordFilterProperty, value);
	}

	protected override Type StyleKeyOverride => typeof(TextBox);

	#endregion
}