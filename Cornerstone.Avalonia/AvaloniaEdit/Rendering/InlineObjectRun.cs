#nullable enable

#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// A inline UIElement in the document.
/// </summary>
public class InlineObjectElement : VisualLineElement
{
	#region Constructors

	/// <summary>
	/// Creates a new InlineObjectElement.
	/// </summary>
	/// <param name="documentLength"> The length of the element in the document. Must be non-negative. </param>
	/// <param name="element"> The element to display. </param>
	public InlineObjectElement(int documentLength, Control element)
		: base(1, documentLength)
	{
		Element = element ?? throw new ArgumentNullException(nameof(element));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the inline element that is displayed.
	/// </summary>
	public Control Element { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		return new InlineObjectRun(1, TextRunProperties, Element);
	}

	#endregion
}

/// <summary>
/// A text run with an embedded UIElement.
/// </summary>
public class InlineObjectRun : DrawableTextRun
{
	#region Fields

	internal Size DesiredSize;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new InlineObjectRun instance.
	/// </summary>
	/// <param name="length"> The length of the TextRun. </param>
	/// <param name="properties"> The <see cref="TextRunProperties" /> to use. </param>
	/// <param name="element"> The <see cref="Control" /> to display. </param>
	public InlineObjectRun(int length, TextRunProperties? properties, Control element)
	{
		if (length <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be positive");
		}

		Length = length;
		Properties = properties ?? throw new ArgumentNullException(nameof(properties));
		Element = element ?? throw new ArgumentNullException(nameof(element));

		DesiredSize = element.DesiredSize;
	}

	#endregion

	#region Properties

	public override double Baseline
	{
		get
		{
			var baseline = TextBlock.GetBaselineOffset(Element);
			if (double.IsNaN(baseline))
			{
				baseline = DesiredSize.Height;
			}
			return baseline;
		}
	}

	/// <summary>
	/// Gets the element displayed by the InlineObjectRun.
	/// </summary>
	public Control Element { get; }

	public override int Length { get; }

	public override TextRunProperties? Properties { get; }

	/// <inheritdoc />
	public override Size Size => DesiredSize;

	/// <summary>
	/// Gets the VisualLine that contains this object. This property is only available after the object
	/// was added to the text view.
	/// </summary>
	public VisualLine? VisualLine { get; internal set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Draw(DrawingContext drawingContext, Point origin)
	{
		// noop
	}

	#endregion
}