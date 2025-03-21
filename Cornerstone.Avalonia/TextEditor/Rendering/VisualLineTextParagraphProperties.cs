#region References

using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

internal sealed class VisualLineTextParagraphProperties : TextParagraphProperties
{
	#region Fields

	internal TextRunProperties defaultTextRunProperties;
	internal bool firstLineInParagraph;
	internal double indent;
	internal double tabSize;
	internal TextWrapping textWrapping;

	#endregion

	#region Properties

	public override double DefaultIncrementalTab => tabSize;
	public override TextRunProperties DefaultTextRunProperties => defaultTextRunProperties;
	public override bool FirstLineInParagraph => firstLineInParagraph;

	public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

	//public override TextMarkerProperties TextMarkerProperties { get { return null; } }
	public override double Indent => indent;
	public override double LineHeight => DefaultTextRunProperties.FontRenderingEmSize * 1.35;
	public override TextAlignment TextAlignment => TextAlignment.Left;

	public override TextWrapping TextWrapping => textWrapping;

	#endregion
}