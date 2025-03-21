#region References

using System;
using Avalonia;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents the column rendering the line offsets.
/// </summary>
public class OffsetColumn : Column
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="IsUppercase" /> property.
	/// </summary>
	public static readonly StyledProperty<bool> IsUppercaseProperty;

	private Size _minimumSize;

	#endregion

	#region Constructors

	static OffsetColumn()
	{
		IsUppercaseProperty = AvaloniaProperty.Register<HexColumn, bool>(nameof(IsUppercase), true);
		IsUppercaseProperty.Changed.AddClassHandler<HexColumn, bool>(OnIsUpperCaseChanged);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a value indicating whether the hexadecimal digits should be rendered in uppercase or not.
	/// </summary>
	public bool IsUppercase
	{
		get => GetValue(IsUppercaseProperty);
		set => SetValue(IsUppercaseProperty, value);
	}

	/// <inheritdoc />
	public override Size MinimumSize => _minimumSize;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override TextLine CreateTextLine(VisualBytesLine line)
	{
		if (HexView is null)
		{
			throw new InvalidOperationException();
		}

		var offset = line.Range.Start.ByteIndex;
		var text = IsUppercase
			? $"{offset:X8}:"
			: $"{offset:x8}:";

		return CreateTextLine(text);
	}

	/// <inheritdoc />
	public override void Measure()
	{
		if (HexView is null)
		{
			_minimumSize = default;
		}
		else
		{
			var dummy = CreateTextLine("00000000:")!;
			_minimumSize = new Size(dummy.Width, dummy.Height);
		}
	}

	private TextLine CreateTextLine(string text)
	{
		if (HexView is null)
		{
			return null;
		}

		var properties = GetTextRunProperties();
		return TextFormatter.Current.FormatLine(
			new SimpleTextSource(text, properties),
			0,
			double.MaxValue,
			new GenericTextParagraphProperties(properties)
		)!;
	}

	private static void OnIsUpperCaseChanged(HexColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
	{
		arg1.HexView?.InvalidateVisualLines();
	}

	#endregion
}