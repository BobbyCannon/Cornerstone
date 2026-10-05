#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Text;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Xml;

#endregion

namespace Cornerstone.Text.Formatting;

public class XmlDocumentFormatter : IDocumentFormatter
{
	#region Fields

	private static readonly HashSet<string> HtmlPreserveNames;
	private static readonly HashSet<string> HtmlVoidNames;

	#endregion

	#region Constructors

	static XmlDocumentFormatter()
	{
		HtmlVoidNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"area", "base", "br", "col", "embed", "hr", "img", "input",
			"link", "meta", "param", "source", "track", "wbr"
		};
		HtmlPreserveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"pre", "script", "style", "textarea"
		};
	}

	#endregion

	#region Methods

	public bool CanFormat(Tokenizer tokenizer)
	{
		return tokenizer is XmlTokenizer;
	}

	public string Format(IStringBuffer buffer, IEnumerable<Token> tokens, DocumentFormatOptions options)
	{
		if (buffer == null)
		{
			return string.Empty;
		}

		options ??= new XmlFormatOptions();
		var xmlOptions = options as XmlFormatOptions ?? new XmlFormatOptions();
		var list = new List<Token>();
		if (tokens != null)
		{
			foreach (var token in tokens)
			{
				if (token != null)
				{
					list.Add(token);
				}
			}
		}

		var writer = new Writer(buffer, list, options, xmlOptions);
		return writer.Write();
	}

	#endregion

	#region Classes

	private sealed class Tag
	{
		public bool IsEmpty;
		public bool IsEnd;
		public string Name;
		public bool Preserve;
		public int CloseIndex;
	}

	private sealed class Writer
	{
		private readonly IStringBuffer _buffer;
		private readonly StringBuilder _builder;
		private int _depth;
		private readonly bool _html;
		private readonly Stack<bool> _inline;
		private readonly bool _minify;
		private readonly string _newLine;
		private readonly DocumentFormatOptions _options;
		private bool _pendingIndent;
		private int _preserveUntilDepth;
		private readonly List<Token> _tokens;
		private readonly XmlFormatOptions _xmlOptions;

		public Writer(IStringBuffer buffer, List<Token> tokens, DocumentFormatOptions options, XmlFormatOptions xmlOptions)
		{
			_buffer = buffer;
			_tokens = tokens;
			_options = options;
			_xmlOptions = xmlOptions;
			_builder = new StringBuilder(buffer.Count);
			_newLine = options.ResolveNewLine(buffer);
			_minify = xmlOptions.Minify;
			_html = xmlOptions.Dialect == XmlFormatDialect.Html;
			_preserveUntilDepth = int.MaxValue;
			_inline = new Stack<bool>();
		}

		public string Write()
		{
			var i = 0;
			while (i < _tokens.Count)
			{
				var token = _tokens[i];
				if (IsPreserving())
				{
					if ((token.Type == XmlTokenizer.TokenTypeEndTagOpen)
						&& TryReadTag(i, out var endTag)
						&& endTag.IsEnd)
					{
						for (var j = i; j <= endTag.CloseIndex; j++)
						{
							AppendToken(_tokens[j]);
						}

						if (_inline.Count > 0)
						{
							_inline.Pop();
						}

						_depth = Math.Max(0, _depth - 1);
						if (_depth < _preserveUntilDepth)
						{
							_preserveUntilDepth = int.MaxValue;
						}

						i = endTag.CloseIndex + 1;
						continue;
					}

					if ((token.Type == XmlTokenizer.TokenTypeStartTagOpen)
						&& TryReadTag(i, out var nested)
						&& !nested.IsEnd
						&& !nested.IsEmpty)
					{
						_depth++;
						_inline.Push(true);
					}

					AppendToken(token);
					i++;
					continue;
				}

				if (IsIgnorable(token.Type))
				{
					i++;
					continue;
				}

				if ((token.Type == XmlTokenizer.TokenTypeStartTagOpen) || (token.Type == XmlTokenizer.TokenTypeEndTagOpen))
				{
					i = WriteElement(i);
					continue;
				}

				WritePendingIndent();
				_pendingIndent = false;
				AppendToken(token);
				i++;
			}

			return _builder.ToString();
		}

		private void AppendToken(Token token)
		{
			if ((token.StartOffset < 0) || (token.Length <= 0) || (token.StartOffset >= _buffer.Count))
			{
				return;
			}

			var length = token.Length;
			if ((token.StartOffset + length) > _buffer.Count)
			{
				length = _buffer.Count - token.StartOffset;
			}

			_builder.Append(_buffer.Substring(token.StartOffset, length));
		}

		private ContentKind Classify(int start, int matchingEnd)
		{
			if (matchingEnd < 0)
			{
				return ContentKind.Elements;
			}

			var hasText = false;
			var hasElements = false;
			var i = start;
			while (i < matchingEnd)
			{
				var token = _tokens[i];
				if (IsIgnorable(token.Type))
				{
					i++;
					continue;
				}

				if (token.Type == XmlTokenizer.TokenTypeStartTagOpen)
				{
					hasElements = true;
					if (!TryReadTag(i, out var tag))
					{
						break;
					}

					i = tag.CloseIndex + 1;
					continue;
				}

				if ((token.Type == XmlTokenizer.TokenTypeText)
					|| (token.Type == XmlTokenizer.TokenTypeEntityReference))
				{
					if (!IsTokenWhitespace(token))
					{
						hasText = true;
					}

					i++;
					continue;
				}

				if ((token.Type == XmlTokenizer.TokenTypeComment)
					|| (token.Type == XmlTokenizer.TokenTypeCData)
					|| (token.Type == XmlTokenizer.TokenTypeProcessingInstruction)
					|| (token.Type == XmlTokenizer.TokenTypeDocType))
				{
					hasElements = true;
					i++;
					continue;
				}

				i++;
			}

			if (hasElements && hasText)
			{
				return ContentKind.Mixed;
			}

			if (hasElements)
			{
				return ContentKind.Elements;
			}

			if (hasText)
			{
				return ContentKind.Text;
			}

			return ContentKind.None;
		}

		private int FindMatchingEnd(int start, string name)
		{
			var depth = 1;
			var i = start;
			while (i < _tokens.Count)
			{
				var token = _tokens[i];
				if ((token.Type != XmlTokenizer.TokenTypeStartTagOpen)
					&& (token.Type != XmlTokenizer.TokenTypeEndTagOpen))
				{
					i++;
					continue;
				}

				if (!TryReadTag(i, out var tag))
				{
					return -1;
				}

				if (tag.IsEnd)
				{
					if (string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase))
					{
						depth--;
						if (depth == 0)
						{
							return i;
						}
					}
				}
				else if (!tag.IsEmpty && !IsVoid(tag.Name))
				{
					if (string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase))
					{
						depth++;
					}
				}

				i = tag.CloseIndex + 1;
			}

			return -1;
		}

		private bool IsPreserving()
		{
			return _depth >= _preserveUntilDepth;
		}

		private bool IsTokenWhitespace(Token token)
		{
			if (token.Length <= 0)
			{
				return true;
			}

			for (var i = 0; i < token.Length; i++)
			{
				var offset = token.StartOffset + i;
				if (offset >= _buffer.Count)
				{
					break;
				}

				if (!char.IsWhiteSpace(_buffer[offset]))
				{
					return false;
				}
			}

			return true;
		}

		private bool IsVoid(string name)
		{
			return _html && !string.IsNullOrEmpty(name) && HtmlVoidNames.Contains(name);
		}

		private static bool IsIgnorable(int type)
		{
			return (type == TextProcessor.TokenTypeWhitespace)
				|| (type == TextProcessor.TokenTypeNewLine);
		}

		private string TokenText(Token token)
		{
			if ((token.StartOffset < 0) || (token.Length <= 0) || (token.StartOffset >= _buffer.Count))
			{
				return string.Empty;
			}

			var length = token.Length;
			if ((token.StartOffset + length) > _buffer.Count)
			{
				length = _buffer.Count - token.StartOffset;
			}

			return _buffer.Substring(token.StartOffset, length);
		}

		private bool TryReadTag(int index, out Tag tag)
		{
			tag = null;
			if ((index < 0) || (index >= _tokens.Count))
			{
				return false;
			}

			var open = _tokens[index];
			if ((open.Type != XmlTokenizer.TokenTypeStartTagOpen)
				&& (open.Type != XmlTokenizer.TokenTypeEndTagOpen))
			{
				return false;
			}

			tag = new Tag
			{
				IsEnd = open.Type == XmlTokenizer.TokenTypeEndTagOpen,
				Name = string.Empty,
				CloseIndex = index
			};

			for (var i = index + 1; i < _tokens.Count; i++)
			{
				var token = _tokens[i];
				if ((token.Type == XmlTokenizer.TokenTypeTagName) && string.IsNullOrEmpty(tag.Name))
				{
					tag.Name = TokenText(token);
					tag.CloseIndex = i;
					continue;
				}

				if ((token.Type == XmlTokenizer.TokenTypeAttributeName)
					&& string.Equals(TokenText(token), "xml:space", StringComparison.OrdinalIgnoreCase)
					&& ((i + 2) < _tokens.Count)
					&& (_tokens[i + 1].Type == XmlTokenizer.TokenTypeEquals)
					&& (_tokens[i + 2].Type == XmlTokenizer.TokenTypeAttributeValue))
				{
					var value = TokenText(_tokens[i + 2]).Trim('"', '\'');
					tag.Preserve = string.Equals(value, "preserve", StringComparison.OrdinalIgnoreCase);
				}

				if (token.Type == XmlTokenizer.TokenTypeEmptyElementClose)
				{
					tag.IsEmpty = true;
					tag.CloseIndex = i;
					return true;
				}

				if (token.Type == XmlTokenizer.TokenTypeTagClose)
				{
					tag.CloseIndex = i;
					return true;
				}

				if ((token.Type == XmlTokenizer.TokenTypeStartTagOpen)
					|| (token.Type == XmlTokenizer.TokenTypeEndTagOpen))
				{
					tag.CloseIndex = i - 1;
					return true;
				}

				tag.CloseIndex = i;
			}

			return true;
		}

		private int WriteElement(int index)
		{
			if (!TryReadTag(index, out var tag))
			{
				AppendToken(_tokens[index]);
				return index + 1;
			}

			if (tag.IsEnd)
			{
				var inline = (_inline.Count > 0) && _inline.Pop();
				if (_depth > 0)
				{
					_depth--;
				}

				if (!_minify && !inline)
				{
					_builder.Append(_newLine);
					_builder.Append(_options.Indent(_depth));
				}

				_pendingIndent = false;
				return WriteTag(index, out _);
			}

			if ((_inline.Count > 0) && !_inline.Peek() && !_minify && !_pendingIndent)
			{
				_builder.Append(_newLine);
				_pendingIndent = true;
			}

			WritePendingIndent();
			_pendingIndent = false;
			var next = WriteTag(index, out tag);
			if (tag.IsEmpty || IsVoid(tag.Name))
			{
				return next;
			}

			var matchingEnd = FindMatchingEnd(next, tag.Name);
			var kind = Classify(next, matchingEnd);
			var preserve = tag.Preserve || (_html && HtmlPreserveNames.Contains(tag.Name ?? string.Empty));
			if (preserve)
			{
				_inline.Push(true);
				_depth++;
				_preserveUntilDepth = _depth;
				return next;
			}

			if (_minify)
			{
				_inline.Push(true);
				_depth++;
				return next;
			}

			if ((kind == ContentKind.None) || (kind == ContentKind.Text))
			{
				_inline.Push(true);
				_depth++;
				return next;
			}

			_inline.Push(false);
			_depth++;
			if (kind == ContentKind.Elements)
			{
				_builder.Append(_newLine);
				_pendingIndent = true;
			}

			return next;
		}

		private int WriteTag(int index, out Tag tag)
		{
			TryReadTag(index, out tag);
			var closeIndex = tag?.CloseIndex ?? index;
			var firstAttribute = true;
			for (var i = index; i <= closeIndex; i++)
			{
				var token = _tokens[i];
				if (IsIgnorable(token.Type))
				{
					continue;
				}

				if (token.Type == XmlTokenizer.TokenTypeAttributeName)
				{
					if (_minify)
					{
						if (!firstAttribute)
						{
							_builder.Append(' ');
						}
					}
					else if (_xmlOptions.AttributesOnNewLine)
					{
						_builder.Append(_newLine);
						_builder.Append(_options.Indent(_depth + 1));
					}
					else
					{
						_builder.Append(' ');
					}

					firstAttribute = false;
					AppendToken(token);
					continue;
				}

				if (token.Type == XmlTokenizer.TokenTypeEmptyElementClose)
				{
					if (!_minify && _xmlOptions.SpaceBeforeEmptyClose)
					{
						_builder.Append(' ');
					}

					_builder.Append("/>");
					continue;
				}

				AppendToken(token);
			}

			return closeIndex + 1;
		}

		private void WritePendingIndent()
		{
			if (!_pendingIndent || _minify)
			{
				return;
			}

			_builder.Append(_options.Indent(_depth));
			_pendingIndent = false;
		}
	}

	private enum ContentKind
	{
		None,
		Text,
		Elements,
		Mixed
	}

	#endregion
}
