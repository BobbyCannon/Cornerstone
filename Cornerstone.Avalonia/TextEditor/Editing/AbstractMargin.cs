#region References

using System;
using System.Diagnostics;
using Avalonia;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Text.Document;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Base class for margins.
/// Margins don't have to derive from this class, it just helps maintain a reference to the TextView
/// and the TextDocument.
/// AbstractMargin derives from FrameworkElement, so if you don't want to handle visual children and rendering
/// on your own, choose another base class for your margin!
/// </summary>
[DoNotNotify]
public abstract class AbstractMargin : CornerstoneControl, ITextViewConnect
{
	#region Fields

	/// <summary>
	/// TextView property.
	/// </summary>
	public static readonly StyledProperty<TextView> TextViewProperty =
		AvaloniaProperty.Register<AbstractMargin, TextView>(nameof(TextView));

	// automatically set/unset TextView property using ITextViewConnect
	private bool _wasAutoAddedToTextView;

	#endregion

	#region Constructors

	public AbstractMargin()
	{
		this.GetPropertyChangedObservable(TextViewProperty)
			.Subscribe(o =>
			{
				_wasAutoAddedToTextView = false;
				OnTextViewChanged(o.OldValue as TextView, o.NewValue as TextView);
			});
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the document associated with the margin.
	/// </summary>
	public TextEditorDocument Document { get; private set; }

	/// <summary>
	/// Gets/sets the text view for which line numbers are displayed.
	/// </summary>
	/// <remarks> Adding a margin to <see cref="TextArea.LeftMargins" /> will automatically set this property to the text area's TextView. </remarks>
	public TextView TextView
	{
		get => GetValue(TextViewProperty);
		set => SetValue(TextViewProperty, value);
	}

	protected TextArea TextArea { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Called when the <see cref="Document" /> is changing.
	/// </summary>
	protected virtual void OnDocumentChanged(TextEditorDocument oldDocument, TextEditorDocument newDocument)
	{
		Document = newDocument;
	}

	/// <summary>
	/// Called when the <see cref="TextView" /> is changing.
	/// </summary>
	protected virtual void OnTextViewChanged(TextView oldTextView, TextView newTextView)
	{
		if (oldTextView != null)
		{
			oldTextView.DocumentChanged -= TextViewDocumentChanged;
		}

		if (newTextView != null)
		{
			newTextView.DocumentChanged += TextViewDocumentChanged;
		}

		TextViewDocumentChanged(null, null);

		if (oldTextView != null)
		{
			oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
		}

		if (newTextView != null)
		{
			newTextView.VisualLinesChanged += TextViewVisualLinesChanged;

			// find the text area belonging to the new text view
			TextArea = newTextView.GetService(typeof(TextArea)) as TextArea;
		}
		else
		{
			TextArea = null;
		}
	}

	/// <summary>
	/// Called when the attached text views visual lines change.
	/// Default behavior is to Invalidate Margins Visual.
	/// </summary>
	protected virtual void OnTextViewVisualLinesChanged()
	{
		InvalidateVisual();
	}

	void ITextViewConnect.AddToTextView(TextView textView)
	{
		if (TextView == null)
		{
			TextView = textView;
			_wasAutoAddedToTextView = true;
		}
		else if (TextView != textView)
		{
			throw new InvalidOperationException("This margin belongs to a different TextView.");
		}
	}

	void ITextViewConnect.RemoveFromTextView(TextView textView)
	{
		if (_wasAutoAddedToTextView && (TextView == textView))
		{
			TextView = null;
			Debug.Assert(!_wasAutoAddedToTextView); // setting this.TextView should have unset this flag
		}
	}

	private void TextViewDocumentChanged(object sender, EventArgs e)
	{
		OnDocumentChanged(Document, TextView?.Document);
	}

	private void TextViewVisualLinesChanged(object sender, EventArgs e)
	{
		OnTextViewVisualLinesChanged();
	}

	#endregion
}