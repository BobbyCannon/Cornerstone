#region References

using System;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Indentation.CSharp;

/// <summary>
/// Smart indentation for C#.
/// </summary>
public class CSharpIndentationStrategy : DefaultIndentationStrategy
{
	#region Fields

	private string _indentationString = "\t";

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new CSharpIndentationStrategy.
	/// </summary>
	public CSharpIndentationStrategy()
	{
	}

	/// <summary>
	/// Creates a new CSharpIndentationStrategy and initializes the settings using the text editor options.
	/// </summary>
	public CSharpIndentationStrategy(TextEditorOptions options)
	{
		IndentationString = options.IndentationString;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the indentation string.
	/// </summary>
	public string IndentationString
	{
		get => _indentationString;
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("Indentation string must not be null or empty");
			}
			_indentationString = value;
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Performs indentation using the specified document accessor.
	/// </summary>
	/// <param name="document"> Object used for accessing the document line-by-line </param>
	/// <param name="keepEmptyLines"> Specifies whether empty lines should be kept </param>
	public void Indent(IDocumentAccessor document, bool keepEmptyLines)
	{
		if (document == null)
		{
			throw new ArgumentNullException(nameof(document));
		}
		var settings = new IndentationSettings
		{
			IndentString = IndentationString,
			LeaveEmptyLines = keepEmptyLines
		};

		var r = new IndentationReformatter();
		r.Reformat(document, settings);
	}

	/// <inheritdoc cref="IIndentationStrategy.IndentLine" />
	public override void IndentLine(TextDocument document, DocumentLine line)
	{
		var lineNr = line.LineNumber;
		var acc = new TextDocumentAccessor(document, lineNr, lineNr);
		Indent(acc, false);

		var t = acc.Text;
		if (t.Length == 0)
		{
			// use AutoIndentation for new lines in comments / verbatim strings.
			base.IndentLine(document, line);
		}
	}

	/// <inheritdoc cref="IIndentationStrategy.IndentLines" />
	public override void IndentLines(TextDocument document, int beginLine, int endLine)
	{
		Indent(new TextDocumentAccessor(document, beginLine, endLine), true);
	}

	#endregion
}