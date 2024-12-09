#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Xml;

#endregion

namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// Represents the data for a JSON token.
/// </summary>
public class MarkdownTokenData : TokenData<MarkdownTokenData, MarkdownTokenType>
{
	#region Properties

	public string ElementName { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the MarkdownTokenData with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(MarkdownTokenData update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			ElementName = update.ElementName;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ElementName)), x => x.ElementName = update.ElementName);
		}

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			MarkdownTokenData value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	/// <inheritdoc />
	public override void WriteTo(CodeSyntaxHtmlWriter writer)
	{
		switch (Type)
		{
			case MarkdownTokenType.BlockQuote:
			{
				writer.WriteRaw(ToString());
				return;
			}
			case MarkdownTokenType.Bold:
			{
				if (TokenIndexes.Length == 2)
				{
					writer.WriteSpan(SubString(StartIndex, TokenIndexes[0] - StartIndex), SyntaxColor.Parameter);
					writer.WriteRaw(SubString(TokenIndexes[0], TokenIndexes[1] - TokenIndexes[0]));
					writer.WriteSpan(SubString(TokenIndexes[1], EndIndex - TokenIndexes[1]), SyntaxColor.Parameter);
					return;
				}

				writer.WriteRaw(ToString());
				return;
			}
			case MarkdownTokenType.Italic:
			{
				if (TokenIndexes.Length == 2)
				{
					writer.WriteSpan(SubString(StartIndex, TokenIndexes[0] - StartIndex), SyntaxColor.Parameter);
					writer.WriteRaw(SubString(TokenIndexes[0], TokenIndexes[1] - TokenIndexes[0]));
					writer.WriteSpan(SubString(TokenIndexes[1], EndIndex - TokenIndexes[1]), SyntaxColor.Parameter);
					return;
				}

				writer.WriteRaw(ToString());
				return;
			}
			case MarkdownTokenType.Header:
			{
				if (TokenIndexes.Length == 4)
				{
					var className = SubStringUsingAbsoluteIndexes(TokenIndexes[2], TokenIndexes[3], false);
					writer.WriteElement("span", SubStringUsingAbsoluteIndexes(StartIndex, EndIndex - 1, true), new XmlAttribute("class", className));
					return;
				}

				writer.WriteRaw(ToString());
				return;
			}
			case MarkdownTokenType.Code:
			{
				if (TokenIndexes.Length == 6)
				{
					var language = SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2], true);
					var code = SubStringUsingAbsoluteIndexes(TokenIndexes[2] + 1, TokenIndexes[4], true);
					var codeSyntaxHtml = Tokenizer.ToCodeSyntaxHtml(language, code);

					writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[0], TokenIndexes[1] - 1, true), SyntaxColor.KeywordMuted);
					writer.WriteSpan(language, SyntaxColor.Keyword);
					writer.WriteRaw(codeSyntaxHtml);
					writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[4] + 1, EndIndex - 1, true), SyntaxColor.KeywordMuted);
				}
				else if (TokenIndexes.Length == 4)
				{
					var code = SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2], true);
					writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[0], TokenIndexes[1] - 1, true), SyntaxColor.KeywordMuted);
					writer.WriteRaw(code);
					writer.WriteSpan(SubStringUsingAbsoluteIndexes(TokenIndexes[2] + 1, EndIndex - 1, true), SyntaxColor.KeywordMuted);
				}
				else if (TokenIndexes.Length == 2)
				{
					writer.WriteSpan(SubString(TokenIndexes[0], 3), SyntaxColor.KeywordMuted);
					writer.WriteRaw(SubString(TokenIndexes[0] + 3, TokenIndexes[1] - TokenIndexes[0] - 3));
					writer.WriteSpan(SubString(TokenIndexes[1], 3), SyntaxColor.KeywordMuted);
				}
				return;
			}
			case MarkdownTokenType.Link:
			{
				writer.WriteRaw(SubString(StartIndex, (TokenIndexes[0] - StartIndex) + 1));
				writer.WriteSpan(SubString(TokenIndexes[0] + 1, TokenIndexes[1] - TokenIndexes[0] - 1), SyntaxColor.Comment);
				writer.WriteRaw(SubString(TokenIndexes[1], (TokenIndexes[2] - TokenIndexes[1]) + 1));
				writer.WriteSpan(SubString(TokenIndexes[2] + 1, TokenIndexes[3] - TokenIndexes[2] - 1), SyntaxColor.Link);
				writer.WriteRaw(SubString(TokenIndexes[3], EndIndex - TokenIndexes[3]));
				return;
			}
			case MarkdownTokenType.Text:
			default:
			{
				writer.WriteRaw(ToString());
				return;
			}
		}
	}

	public override void WriteTo(HtmlWriter writer)
	{
		if (TryGetParentElement(Type, out var parentElement))
		{
			writer.PopElementIfNot(parentElement, true);
		}

		switch (Type)
		{
			case MarkdownTokenType.BlockQuote:
			{
				if (TokenIndexes.Length == 1)
				{
					writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[0], EndIndex, false));
				}
				return;
			}
			case MarkdownTokenType.Bold:
			{
				var prefixLength = TokenIndexes[0] - StartIndex;
				var isAlsoItalic = (prefixLength > 2) && ((prefixLength % 2) == 1);
				if (isAlsoItalic)
				{
					writer.PushElement("em", false);
				}

				writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[0] - 1, TokenIndexes[1], false));

				if (isAlsoItalic)
				{
					writer.PopElement(false);
				}
				return;
			}
			case MarkdownTokenType.Italic:
			{
				writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[0] - 1, TokenIndexes[1], false));
				return;
			}
			case MarkdownTokenType.Code:
			{
				if (TokenIndexes.Length == 6)
				{
					var language = SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2], true);
					var code = SubStringUsingAbsoluteIndexes(TokenIndexes[3], TokenIndexes[4], true);
					var codeSyntaxHtml = Tokenizer.ToCodeSyntaxHtml(language, code);
					writer.PushElement("pre", false);
					writer.WriteRaw(codeSyntaxHtml);
					writer.PopElement(false);
				}
				else if (TokenIndexes.Length == 4)
				{
					var code = SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2], true);
					writer.PushElement("pre", false);
					writer.WriteRaw(code);
					writer.PopElement(false);
				}
				else
				{
					writer.WriteRaw(ToString());
				}
				return;
			}
			case MarkdownTokenType.Header:
			{
				if (TokenIndexes.Length == 1)
				{
					writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[0], EndIndex, false));
				}
				else if (TokenIndexes.Length == 4)
				{
					var className = SubStringUsingAbsoluteIndexes(TokenIndexes[2], TokenIndexes[3], false);
					writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[1], TokenIndexes[2], false), new XmlAttribute("class", className));
				}
				return;
			}
			case MarkdownTokenType.Link:
			{
				var value = SubStringUsingAbsoluteIndexes(TokenIndexes[0], TokenIndexes[1], false);
				var link = SubStringUsingAbsoluteIndexes(TokenIndexes[2], TokenIndexes[3], false);
				writer.WriteElement(ElementName, value, new XmlAttribute("href", link));
				break;
			}
			case MarkdownTokenType.OrderedList:
			{
				writer.PopElementIfNot("ol", true);
				writer.PushElementIfNot("ol", true);
				writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(StartIndex + 2, EndIndex - 1, true));
				break;
			}
			case MarkdownTokenType.UnorderedList:
			{
				writer.PopElementIfNot("ul", true);
				writer.PushElementIfNot("ul", true);
				writer.WriteElement(ElementName, SubStringUsingAbsoluteIndexes(TokenIndexes[0], EndIndex - 1, true));
				break;
			}
			case MarkdownTokenType.NewLine:
			{
				writer.WriteRaw(Environment.NewLine);
				return;
			}
			case MarkdownTokenType.Whitespace:
			{
				return;
			}
			default:
			{
				var elementValue = ToString();
				writer.WriteElement("p", elementValue);
				return;
			}
		}
	}

	private bool TryGetParentElement(MarkdownTokenType type, out string element)
	{
		element = type switch
		{
			MarkdownTokenType.OrderedList => "ol",
			MarkdownTokenType.UnorderedList => "ul",
			MarkdownTokenType.NewLine => null,
			MarkdownTokenType.Whitespace => null,
			_ => ""
		};

		return element != null;
	}

	#endregion
}