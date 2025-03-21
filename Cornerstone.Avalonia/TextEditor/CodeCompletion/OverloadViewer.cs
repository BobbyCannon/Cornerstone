#region References

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// Represents a text between "Up" and "Down" buttons.
/// </summary>
[DoNotNotify]
public class OverloadViewer : TemplatedControl
{
	#region Fields

	/// <summary>
	/// The ItemProvider property.
	/// </summary>
	public static readonly StyledProperty<IOverloadProvider> ProviderProperty =
		AvaloniaProperty.Register<OverloadViewer, IOverloadProvider>("Provider");

	/// <summary>
	/// The text property.
	/// </summary>
	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<OverloadViewer, string>("Text");

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the item provider.
	/// </summary>
	public IOverloadProvider Provider
	{
		get => GetValue(ProviderProperty);
		set => SetValue(ProviderProperty, value);
	}

	/// <summary>
	/// Gets/Sets the text between the Up and Down buttons.
	/// </summary>
	public string Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Changes the selected index.
	/// </summary>
	/// <param name="relativeIndexChange"> The relative index change - usual values are +1 or -1. </param>
	public void ChangeIndex(int relativeIndexChange)
	{
		var p = Provider;
		if (p != null)
		{
			var newIndex = p.SelectedIndex + relativeIndexChange;
			if (newIndex < 0)
			{
				newIndex = p.Count - 1;
			}
			if (newIndex >= p.Count)
			{
				newIndex = 0;
			}
			p.SelectedIndex = newIndex;
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs args)
	{
		base.OnApplyTemplate(args);

		var upButton = args.NameScope.Find<Button>("PART_UP");
		if (upButton != null)
		{
			upButton.Click += (sender, e) =>
			{
				e.Handled = true;
				ChangeIndex(-1);
			};
		}

		var downButton = args.NameScope.Find<Button>("PART_DOWN");
		if (downButton != null)
		{
			downButton.Click += (sender, e) =>
			{
				e.Handled = true;
				ChangeIndex(+1);
			};
		}
	}

	#endregion
}

/// <summary>
/// Converter to be used in the <see cref="OverloadViewer" /> control theme. Used to set the
/// visibility of the part showing the number of overloads.
/// </summary>
public sealed class CollapseIfSingleOverloadConverter : IValueConverter
{
	#region Properties

	public static CollapseIfSingleOverloadConverter Instance { get; } = new();

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		// Show the up/down arrows and the "i of n" text if there are 2 or more method overloads.
		if (value is int count)
		{
			return count >= 2;
		}

		return AvaloniaProperty.UnsetValue;
	}

	object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}