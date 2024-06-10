#region References

using System;
using System.IO;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Utils;
// TODO: This class (and derived classes) is currently unused; decide whether to keep it.
// (until this is decided, keep the class internal)

/// <summary>
/// A text writer that supports creating spans of highlighted text.
/// </summary>
internal abstract class RichTextWriter : TextWriter
{
	#region Methods

	/// <summary>
	/// Begin a span that links to the specified URI.
	/// </summary>
	public virtual void BeginHyperlinkSpan(Uri uri)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a colored span.
	/// </summary>
	public virtual void BeginSpan(Color foregroundColor)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font weight.
	/// </summary>
	public virtual void BeginSpan(FontWeight fontWeight)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font style.
	/// </summary>
	public virtual void BeginSpan(FontStyle fontStyle)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a span with modified font family.
	/// </summary>
	public virtual void BeginSpan(FontFamily fontFamily)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Begin a highlighted span.
	/// </summary>
	public virtual void BeginSpan(HighlightingColor highlightingColor)
	{
		BeginUnhandledSpan();
	}

	/// <summary>
	/// Marks the end of the current span.
	/// </summary>
	public abstract void EndSpan();

	/// <summary>
	/// Increases the indentation level.
	/// </summary>
	public abstract void Indent();

	/// <summary>
	/// Decreases the indentation level.
	/// </summary>
	public abstract void Unindent();

	/// <summary>
	/// Writes the RichText instance.
	/// </summary>
	public void Write(RichText richText)
	{
		Write(richText, 0, richText.Length);
	}

	/// <summary>
	/// Writes the RichText instance.
	/// </summary>
	public virtual void Write(RichText richText, int offset, int length)
	{
		foreach (var section in richText.GetHighlightedSections(offset, length))
		{
			BeginSpan(section.Color);
			Write(richText.Text.Substring(section.Offset, section.Length));
			EndSpan();
		}
	}

	/// <summary>
	/// Gets called by the RichTextWriter base class when a BeginSpan() method
	/// that is not overwritten gets called.
	/// </summary>
	protected abstract void BeginUnhandledSpan();

	#endregion
}