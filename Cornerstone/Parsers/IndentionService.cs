#region References

using System;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public class IndentionService
{
	#region Constructors

	public IndentionService(IStringBuffer buffer)
	{
		Buffer = buffer;
		Indent = "\t";
	}

	#endregion

	#region Properties

	public string Indent { get; }

	protected IStringBuffer Buffer { get; }

	#endregion

	#region Methods

	public static IndentionService GetByExtension(string extension, StringGapBuffer buffer)
	{
		return new IndentionService(buffer);
	}

	public virtual bool TryGetIndent(TextRange previousLine, TextRange currentLine, out ReadOnlySpan<char> indent)
	{
		indent = default;
		if (previousLine.Length == 0)
		{
			return false;
		}

		var indentLevel = CountLeadingIndent(previousLine);
		if (indentLevel <= 0)
		{
			return false;
		}

		indent = BuildIndentString(indentLevel);
		return true;
	}

	private ReadOnlySpan<char> BuildIndentString(int level)
	{
		if (level == 1)
		{
			return Indent;
		}

		return new string(Indent[0], Indent.Length * level).AsSpan();
	}

	private int CountLeadingIndent(TextRange line)
	{
		if (line.Length == 0)
		{
			return 0;
		}

		var indentSpan = Indent.AsSpan();
		var indentLen = indentSpan.Length;
		if (indentLen == 0)
		{
			return 0;
		}

		var count = 0;
		var i = 0;

		while ((i + indentLen) <= line.Length)
		{
			// Check if the next segment exactly matches the indent string
			if (Buffer.Equals(i + line.StartOffset, indentSpan))
			{
				count++;
				i += indentLen;
			}
			else
			{
				// Stop at first non-indent character (or partial indent)
				break;
			}
		}

		return count;
	}

	#endregion
}