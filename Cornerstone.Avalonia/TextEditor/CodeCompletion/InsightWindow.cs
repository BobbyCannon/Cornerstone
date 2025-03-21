#region References

using System;
using Cornerstone.Avalonia.TextEditor.Editing;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

/// <summary>
/// A popup-like window that is attached to a text segment.
/// </summary>
public class InsightWindow : CompletionWindowBase
{
	#region Constructors

	/// <summary>
	/// Creates a new InsightWindow.
	/// </summary>
	public InsightWindow(TextArea textArea) : base(textArea)
	{
		CloseAutomatically = true;
		AttachEvents();
		Initialize();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether the insight window should close automatically.
	/// The default value is true.
	/// </summary>
	public bool CloseAutomatically { get; set; }

	/// <inheritdoc />
	protected override bool CloseOnFocusLost => CloseAutomatically;

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void DetachEvents()
	{
		TextArea.Caret.PositionChanged -= CaretPositionChanged;
		base.DetachEvents();
	}

	private void AttachEvents()
	{
		TextArea.Caret.PositionChanged += CaretPositionChanged;
	}

	private void CaretPositionChanged(object sender, EventArgs e)
	{
		if (CloseAutomatically)
		{
			var offset = TextArea.Caret.Offset;
			if ((offset < StartOffset) || (offset > EndOffset))
			{
				Hide();
			}
		}
	}

	private void Initialize()
	{
		// TODO: working area
		//var caret = this.TextArea.Caret.CalculateCaretRectangle();
		//var pointOnScreen = this.TextArea.TextView.PointToScreen(caret.Location - this.TextArea.TextView.ScrollOffset);
		//Rect workingArea = System.Windows.Forms.Screen.FromPoint(pointOnScreen.ToSystemDrawing()).WorkingArea.ToWpf().TransformFromDevice(this);
		//MaxHeight = workingArea.Height;
		//MaxWidth = Math.Min(workingArea.Width, Math.Max(1000, workingArea.Width * 0.6));
	}

	#endregion
}