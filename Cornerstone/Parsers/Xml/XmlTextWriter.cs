#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Xml;

/// <summary>
/// A writer for XML text.
/// </summary>
public class XmlTextBuilder : TextBuilder
{
	#region Fields

	private readonly ConcurrentStack<XmlStack> _stack;

	#endregion

	#region Constructors

	public XmlTextBuilder()
	{
		_stack = new ConcurrentStack<XmlStack>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Append the string but escape it as processing.
	/// </summary>
	/// <param name="value"> The value to be processed. </param>
	/// <param name="quoteCharacter"> The character that surrounds this string. </param>
	/// <param name="builder"> The builder to append to. </param>
	/// <remarks>
	/// https://stackoverflow.com/questions/1091945/what-characters-do-i-need-to-escape-in-xml-documents
	/// </remarks>
	public static void AppendEscapedString(string value, ITextBuilder builder, char quoteCharacter = ' ')
	{
		if (value == null)
		{
			return;
		}

		// "   &quot;
		// '   &apos;
		// <   &lt;
		// >   &gt;
		// &   &amp;
		var containedByDoubleQuote = quoteCharacter == '"';
		var containedBySingleQuote = quoteCharacter == '\'';
		var quoteStack = new Stack<char>();
		var doubleQuoteStack = new Stack<bool>();
		var singleQuoteStack = new Stack<bool>();

		foreach (var character in value)
		{
			var lastQuote = quoteStack.Count > 0 ? quoteStack.Peek() : '\0';
			var lastDoubleQuoteEncoded = (doubleQuoteStack.Count > 0) && doubleQuoteStack.Peek();
			var lastSingleQuoteEncoded = (singleQuoteStack.Count > 0) && singleQuoteStack.Peek();

			switch (character)
			{
				case '"' when containedByDoubleQuote
					|| lastDoubleQuoteEncoded
					|| ((lastQuote == '\'')
						&& (doubleQuoteStack.Count > 0)
					):
				{
					builder.Append("&quot;");
					ProcessStack(quoteStack, doubleQuoteStack, true, character);
					continue;
				}
				case '"':
				{
					builder.Append(character);
					ProcessStack(quoteStack, doubleQuoteStack, false, character);
					continue;
				}
				case '\'' when containedBySingleQuote
					|| lastSingleQuoteEncoded
					|| ((lastQuote == '"')
						&& (singleQuoteStack.Count > 0)
					):
				{
					builder.Append("&apos;");
					ProcessStack(quoteStack, singleQuoteStack, true, character);
					continue;
				}
				case '\'':
				{
					builder.Append(character);
					ProcessStack(quoteStack, singleQuoteStack, false, character);
					continue;
				}
				case '<':
				{
					builder.Append("&lt;");
					continue;
				}
				case '>':
				{
					builder.Append("&gt;");
					continue;
				}
				case '&':
				{
					builder.Append("&amp;");
					continue;
				}
				case '\r' when quoteCharacter != ' ':
				{
					builder.Append("&#xD;");
					continue;
				}
				case '\n' when quoteCharacter != ' ':
				{
					builder.Append("&#xA;");
					continue;
				}
				default:
				{
					builder.Append(character);
					break;
				}
			}
		}
	}

	/// <summary>
	/// Escape the string.
	/// </summary>
	/// <param name="value"> The value to be processed. </param>
	/// <param name="quoteCharacter"> The character that surrounds this string. </param>
	/// <returns> The escaped string. </returns>
	/// <remarks>
	/// https://stackoverflow.com/questions/1091945/what-characters-do-i-need-to-escape-in-xml-documents
	/// </remarks>
	public static string EscapedString(string value, char quoteCharacter = ' ')
	{
		var builder = new TextBuilder();
		AppendEscapedString(value, builder, quoteCharacter);
		return builder.ToString();
	}

	public void Flush()
	{
	}

	public void WriteAttributeString(string name, string value)
	{
		Append(' ');
		Append(name);
		Append("=\"");
		AppendEscapedString(value, this, '"');
		Append('"');
	}

	public void WriteEndDocument()
	{
	}

	public void WriteEndElement()
	{
		if (!_stack.TryPop(out var element))
		{
			return;
		}

		if (element.HasContent || element.HasChildren)
		{
			if (element.HasChildren)
			{
				NewLine();
			}

			PopIndent();
			Append("</");
			Append(element.Name);
			Append('>');
			return;
		}

		Append(" />");
	}

	public void WriteStartDocument(XmlDeclaration declaration)
	{
		if (declaration == null)
		{
			return;
		}

		// <?xml version="1.0" encoding="utf-8"?>
		Append("<?xml");

		if (!string.IsNullOrWhiteSpace(declaration.Version))
		{
			Append(" version=\"");
			Append(declaration.Version);
			Append('"');
		}

		if (!string.IsNullOrWhiteSpace(declaration.Encoding))
		{
			Append(" encoding=\"");
			Append(declaration.Encoding);
			Append('"');
		}

		AppendLine("?>");
	}

	public void WriteStartElement(string name)
	{
		PrepareForChildren(x => x.HasChildren = true);

		if (Length > 0)
		{
			NewLine();
		}

		Append('<');
		Append(name);

		_stack.Push(new XmlStack(name));
	}

	public void WriteString(string value)
	{
		PrepareForChildren(x => x.HasContent = true);
		AppendEscapedString(value, this);
	}

	internal void PrepareForChildren(Action<XmlStack> update)
	{
		if (!_stack.TryPeek(out var current))
		{
			return;
		}

		if (!current.HasContent && !current.HasChildren)
		{
			Append('>');
			PushIndent();
		}

		update.Invoke(current);
	}

	private static void ProcessStack(Stack<char> quoteStack, Stack<bool> encodedStack, bool encoded, char character)
	{
		if ((quoteStack.Count > 0) && (quoteStack.Peek() == character))
		{
			quoteStack.Pop();
			encodedStack.Pop();
		}
		else
		{
			quoteStack.Push(character);
			encodedStack.Push(encoded);
		}
	}

	#endregion

	#region Classes

	internal class XmlStack
	{
		#region Constructors

		public XmlStack(string name)
		{
			Name = name;
		}

		#endregion

		#region Properties

		public bool HasChildren { get; set; }

		public bool HasContent { get; set; }

		public string Name { get; }

		#endregion
	}

	#endregion
}