#region References

using System.Collections.Generic;
using Avalonia;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// The control that contains the text.
/// This control is used to allow other UIElements to be placed inside the TextView but
/// behind the text.
/// The text rendering process (VisualLine creation) is controlled by the TextView, this
/// class simply displays the created Visual Lines.
/// </summary>
/// <remarks>
/// This class does not contain any input handling and is invisible to hit testing. Input
/// is handled by the TextView.
/// This allows UIElements that are displayed behind the text, but still can react to mouse input.
/// </remarks>
internal sealed class TextLayer : Layer
{
	#region Fields

	/// <summary>
	/// the index of the text layer in the layers collection
	/// </summary>
	internal int Index;

	private readonly List<VisualLineDrawingVisual> _visuals = [];

	#endregion

	#region Constructors

	public TextLayer(TextView textView) : base(textView, KnownLayer.Text)
	{
	}

	#endregion

	#region Methods

	protected override void ArrangeCore(Rect finalRect)
	{
		base.ArrangeCore(finalRect);
		TextView.ArrangeTextLayer(_visuals);
	}

	internal void SetVisualLines(ICollection<VisualLine> visualLines)
	{
		foreach (var v in _visuals)
		{
			if (v.VisualLine.IsDisposed)
			{
				VisualChildren.Remove(v);
			}
		}

		_visuals.Clear();
		foreach (var newLine in visualLines)
		{
			var visual = newLine.Render();
			if (!visual.IsAdded)
			{
				VisualChildren.Add(visual);
				visual.IsAdded = true;
			}

			_visuals.Add(visual);
		}

		InvalidateArrange();
	}

	#endregion
}