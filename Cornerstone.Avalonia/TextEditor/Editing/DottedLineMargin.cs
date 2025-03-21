#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Margin for use with the text area.
/// A vertical dotted line to separate the line numbers from the text view.
/// </summary>
public static class DottedLineMargin
{
	#region Fields

	private static readonly object _tag;

	#endregion

	#region Constructors

	static DottedLineMargin()
	{
		_tag = new();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Creates a vertical dotted line to separate the line numbers from the text view.
	/// </summary>
	public static Control Create()
	{
		var line = new Line
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(0, 1),
			Stretch = Stretch.Fill,
			StrokeThickness = 1,
			StrokeLineCap = PenLineCap.Round,
			Margin = new Thickness(0, 0, 8, 0),
			Tag = _tag
		};

		return line;
	}

	/// <summary>
	/// Gets whether the specified UIElement is the result of a DottedLineMargin.Create call.
	/// </summary>
	public static bool IsDottedLineMargin(Control element)
	{
		var l = element as Line;
		return (l != null) && (l.Tag == _tag);
	}

	#endregion
}