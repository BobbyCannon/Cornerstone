﻿namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Allows <see cref="VisualLineElementGenerator" />s, <see cref="IVisualLineTransformer" />s and
/// <see cref="IBackgroundRenderer" />s to be notified when they are added or removed from a text view.
/// </summary>
public interface ITextViewConnect
{
	#region Methods

	/// <summary>
	/// Called when added to a text view.
	/// </summary>
	void AddToTextView(TextView textView);

	/// <summary>
	/// Called when removed from a text view.
	/// </summary>
	void RemoveFromTextView(TextView textView);

	#endregion
}