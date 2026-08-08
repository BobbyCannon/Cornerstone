#region References

using System.Drawing;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public sealed partial class Token : TextRange
{
	#region Constructors

	public Token() : this(0, 0, 0)
	{
	}

	public Token(int type, int startOffset, int endOffset, SyntaxKind color = SyntaxKind.None,
		bool bold = false, bool italic = false, bool strikethrough = false,
		Color? foreground = null, Color? background = null)
	{
		Update(type, startOffset, endOffset, color, bold, italic, strikethrough, (uint?) foreground?.ToArgb(), (uint?) background?.ToArgb());
	}

	#endregion

	#region Properties

	[Notify]
	public partial uint? Background { get; set; }

	[Notify]
	public partial bool Bold { get; set; }

	[Notify]
	public partial uint? Foreground { get; set; }

	[Notify]
	public partial bool Italic { get; set; }

	[Notify]
	public partial bool Strikethrough { get; set; }

	[Notify]
	public partial SyntaxKind SyntaxKind { get; set; }

	[Notify]
	public partial int Type { get; set; }

	#endregion

	#region Methods

	public override string ToString()
	{
		return $"{TextProcessor.TryGetTokenizerStateOrTypeCode(nameof(Type), this)} @ {StartOffset}..{EndOffset} ({Length})";
	}

	public void Update(int type, int startOffset, int endOffset, SyntaxKind syntaxKind,
		bool bold, bool italic, bool strikethrough, uint? foreground, uint? background)
	{
		Type = type;
		StartOffset = startOffset;
		EndOffset = endOffset;
		SyntaxKind = syntaxKind;
		Bold = bold;
		Italic = italic;
		Strikethrough = strikethrough;
		Foreground = foreground;
		Background = background;
	}

	#endregion
}