#region References

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Text.Formatting;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class DocumentFormatOptions : CornerstoneObject
{
	#region Constructors

	public DocumentFormatOptions()
	{
		IndentCharacter = '\t';
		IndentCount = 1;
		LineEndingMode = LineEndingMode.Detect;
	}

	#endregion

	#region Properties

	[Category("Indentation")]
	[DisplayName("Indent Character")]
	[Description("Character used for each indent level.")]
	[Notify]
	public partial char IndentCharacter { get; set; }

	[Category("Indentation")]
	[DisplayName("Indent Count")]
	[Description("Number of indent characters per level.")]
	[Range(0, 16)]
	[Notify]
	public partial int IndentCount { get; set; }

	[Category("Line Endings")]
	[DisplayName("Line Ending")]
	[Description("Line ending to emit. Detect uses the document's existing endings.")]
	[Notify]
	public partial LineEndingMode LineEndingMode { get; set; }

	[Browsable(false)]
	[Notify]
	public partial string NewLine { get; set; }

	#endregion

	#region Methods

	public string ResolveNewLine(IStringBuffer buffer)
	{
		if (!string.IsNullOrEmpty(NewLine))
		{
			return NewLine;
		}

		switch (LineEndingMode)
		{
			case LineEndingMode.Lf:
			{
				return "\n";
			}
			case LineEndingMode.Crlf:
			{
				return "\r\n";
			}
			default:
			{
				if (buffer == null)
				{
					return Environment.NewLine;
				}

				var text = buffer.ToString();
				return text.Contains("\r\n") ? "\r\n" : "\n";
			}
		}
	}

	public string Indent(int depth)
	{
		if ((depth <= 0) || (IndentCount <= 0))
		{
			return string.Empty;
		}

		return new string(IndentCharacter, IndentCount * depth);
	}

	#endregion
}