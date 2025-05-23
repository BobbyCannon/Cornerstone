﻿#region References

using Avalonia.Media;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Collections;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TextEditor.CodeCompletion;

public class CompletionData : Notifiable, ICompletionData
{
	#region Constructors

	public CompletionData()
	{
	}

	public CompletionData(string displayText, string completionText, object description, IImage image, decimal priority, int caretOffset)
	{
		CaretOffset = caretOffset;
		CompletionText = completionText;
		Description = description;
		DisplayText = displayText;
		Image = image;
		Priority = priority;
	}

	#endregion

	#region Properties

	public int CaretOffset { get; set; }

	public string CompletionText { get; set; }

	public object Description { get; set; }

	public string DisplayText { get; set; }

	public IImage Image { get; set; }

	public decimal Priority { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual string CalculateFinalResult(TextArea textArea, IRange completionRange)
	{
		return CompletionText;
	}

	#endregion
}

/// <summary>
/// Describes an entry in the <see cref="CompletionList" />.
/// </summary>
/// <remarks>
/// Note that the CompletionList uses data binding against the properties in this interface.
/// Thus, your implementation of the interface must use public properties; not explicit interface implementation.
/// </remarks>
public interface ICompletionData
{
	#region Properties

	/// <summary>
	/// An offset to move the caret after complete.
	/// </summary>
	int CaretOffset { get; set; }

	/// <summary>
	/// Gets the text to complete.
	/// </summary>
	string CompletionText { get; set; }

	/// <summary>
	/// Gets the description.
	/// </summary>
	object Description { get; set; }

	/// <summary>
	/// Gets the text to display. This property is used to filter the list of visible elements.
	/// </summary>
	string DisplayText { get; set; }

	/// <summary>
	/// Gets the image.
	/// </summary>
	IImage Image { get; set; }

	/// <summary>
	/// Gets the priority. This property is used in the selection logic. You can use it to prefer selecting those items
	/// which the user is accessing most frequently.
	/// </summary>
	decimal Priority { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Perform the completion.
	/// </summary>
	/// <param name="textArea"> The text area on which completion is performed. </param>
	/// <param name="completionRange">
	/// The text segment that was used by the completion window if
	/// the user types (segment between CompletionWindow.StartOffset and CompletionWindow.EndOffset).
	/// </param>
	/// <returns> The new text to complete with. </returns>
	string CalculateFinalResult(TextArea textArea, IRange completionRange);

	#endregion
}