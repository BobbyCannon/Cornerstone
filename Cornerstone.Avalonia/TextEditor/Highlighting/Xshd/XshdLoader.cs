﻿#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Avalonia.Media;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// Loads .xshd files, version 2.0.
/// Version 2.0 files are recognized by the namespace.
/// </summary>
internal static class XshdLoader
{
	#region Constants

	public const string Namespace = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008";

	#endregion

	#region Methods

	public static XshdSyntaxDefinition LoadDefinition(XmlReader reader)
	{
		reader = HighlightingLoader.GetValidatingReader(reader, true);
		reader.Read();
		return ParseDefinition(reader);
	}

	private static void CheckElementName(XmlReader reader, string name)
	{
		if (name != null)
		{
			if (name.Length == 0)
			{
				throw Error(reader, "The empty string is not a valid name.");
			}
			if (name.IndexOf('/') >= 0)
			{
				throw Error(reader, "Element names must not contain a slash.");
			}
		}
	}

	private static Exception Error(XmlReader reader, string message)
	{
		return Error(reader as IXmlLineInfo, message);
	}

	private static Exception Error(IXmlLineInfo lineInfo, string message)
	{
		if (lineInfo != null)
		{
			return new HighlightingDefinitionInvalidException(HighlightingLoader.FormatExceptionMessage(message, lineInfo.LineNumber, lineInfo.LinePosition));
		}
		return new HighlightingDefinitionInvalidException(message);
	}

	private static HighlightingBrush FixedColorHighlightingBrush(Color? color)
	{
		if (color == null)
		{
			return null;
		}
		return new SimpleHighlightingBrush(color.Value);
	}

	private static HighlightingBrush ParseColor(string color)
	{
		if (string.IsNullOrEmpty(color))
		{
			return null;
		}
		return FixedColorHighlightingBrush(Color.Parse(color));
	}

	private static XshdColor ParseColorAttributes(XmlReader reader)
	{
		var color = new XshdColor();
		SetPosition(color, reader);
		color.Foreground = ParseColor(reader.GetAttribute("foreground"));
		color.Background = ParseColor(reader.GetAttribute("background"));
		color.FontWeight = ParseFontWeight(reader.GetAttribute("fontWeight"));
		color.FontStyle = ParseFontStyle(reader.GetAttribute("fontStyle"));
		color.Underline = reader.GetBoolAttribute("underline");
		color.Strikethrough = reader.GetBoolAttribute("strikethrough");
		color.FontFamily = ParseFontFamily(reader.GetAttribute("fontFamily"));
		color.FontSize = ParseFontSize(reader.GetAttribute("fontSize"));
		return color;
	}

	private static XshdReference<XshdColor> ParseColorReference(XmlReader reader)
	{
		var color = reader.GetAttribute("color");
		if (color != null)
		{
			var pos = color.LastIndexOf('/');
			if (pos >= 0)
			{
				return new XshdReference<XshdColor>(color.Substring(0, pos), color.Substring(pos + 1));
			}
			return new XshdReference<XshdColor>(null, color);
		}
		return new XshdReference<XshdColor>(ParseColorAttributes(reader));
	}

	private static XshdSyntaxDefinition ParseDefinition(XmlReader reader)
	{
		Debug.Assert(reader.LocalName == "SyntaxDefinition");
		var def = new XshdSyntaxDefinition { Name = reader.GetAttribute("name") };
		var extensions = reader.GetAttribute("extensions");
		if (extensions != null)
		{
			def.Extensions.AddRange(extensions.Split(';'));
		}
		ParseElements(def.Elements, reader);
		Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
		Debug.Assert(reader.LocalName == "SyntaxDefinition");
		return def;
	}

	private static void ParseElements(ICollection<XshdElement> c, XmlReader reader)
	{
		if (reader.IsEmptyElement)
		{
			return;
		}
		while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement))
		{
			Debug.Assert(reader.NodeType == XmlNodeType.Element);
			if (reader.NamespaceURI != Namespace)
			{
				if (!reader.IsEmptyElement)
				{
					reader.Skip();
				}
				continue;
			}
			switch (reader.Name)
			{
				case "RuleSet":
					c.Add(ParseRuleSet(reader));
					break;
				case "Property":
					c.Add(ParseProperty(reader));
					break;
				case "Color":
					c.Add(ParseNamedColor(reader));
					break;
				case "Keywords":
					c.Add(ParseKeywords(reader));
					break;
				case "Span":
					c.Add(ParseSpan(reader));
					break;
				case "Import":
					c.Add(ParseImport(reader));
					break;
				case "Rule":
					c.Add(ParseRule(reader));
					break;
				default:
					throw new NotSupportedException("Unknown element " + reader.Name);
			}
		}
	}

	private static FontFamily ParseFontFamily(string fontFamily)
	{
		if (string.IsNullOrEmpty(fontFamily))
		{
			return null;
		}
		return new FontFamily(fontFamily);
	}

	private static int? ParseFontSize(string size)
	{
		return int.TryParse(size, out var result) ? result : null;
	}

	private static FontStyle? ParseFontStyle(string fontStyle)
	{
		if (string.IsNullOrEmpty(fontStyle))
		{
			return null;
		}
		return (FontStyle) Enum.Parse(typeof(FontStyle), fontStyle, true);
	}

	private static FontWeight? ParseFontWeight(string fontWeight)
	{
		if (string.IsNullOrEmpty(fontWeight))
		{
			return null;
		}
		return (FontWeight) Enum.Parse(typeof(FontWeight), fontWeight, true);
	}

	private static XshdImport ParseImport(XmlReader reader)
	{
		var import = new XshdImport();
		SetPosition(import, reader);
		import.RuleSetReference = ParseRuleSetReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Skip();
		}
		return import;
	}

	private static XshdKeywords ParseKeywords(XmlReader reader)
	{
		var keywords = new XshdKeywords();
		SetPosition(keywords, reader);
		keywords.ColorReference = ParseColorReference(reader);
		reader.Read();
		while (reader.NodeType != XmlNodeType.EndElement)
		{
			Debug.Assert(reader.NodeType == XmlNodeType.Element);
			keywords.Words.Add(reader.ReadElementContentAsString());
		}
		return keywords;
	}

	private static XshdColor ParseNamedColor(XmlReader reader)
	{
		var color = ParseColorAttributes(reader);
		// check removed: invisible named colors may be useful now that apps can read highlighting data
		//if (color.Foreground == null && color.FontWeight == null && color.FontStyle == null)
		//	throw Error(reader, "A named color must have at least one element.");
		color.Name = reader.GetAttribute("name");
		CheckElementName(reader, color.Name);
		color.ExampleText = reader.GetAttribute("exampleText");
		return color;
	}

	private static XshdElement ParseProperty(XmlReader reader)
	{
		var property = new XshdProperty();
		SetPosition(property, reader);
		property.Name = reader.GetAttribute("name");
		property.Value = reader.GetAttribute("value");
		return property;
	}

	private static XshdRule ParseRule(XmlReader reader)
	{
		var rule = new XshdRule();
		SetPosition(rule, reader);
		rule.ColorReference = ParseColorReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Read();
			if (reader.NodeType == XmlNodeType.Text)
			{
				rule.Regex = reader.ReadContentAsString();
				rule.RegexType = XshdRegexType.IgnorePatternWhitespace;
			}
		}
		return rule;
	}

	private static XshdRuleSet ParseRuleSet(XmlReader reader)
	{
		var ruleSet = new XshdRuleSet();
		SetPosition(ruleSet, reader);
		ruleSet.Name = reader.GetAttribute("name");
		ruleSet.IgnoreCase = reader.GetBoolAttribute("ignoreCase");

		CheckElementName(reader, ruleSet.Name);
		ParseElements(ruleSet.Elements, reader);
		return ruleSet;
	}

	private static XshdReference<XshdRuleSet> ParseRuleSetReference(XmlReader reader)
	{
		var ruleSet = reader.GetAttribute("ruleSet");
		if (ruleSet != null)
		{
			// '/' is valid in highlighting definition names, so we need the last occurence
			var pos = ruleSet.LastIndexOf('/');
			if (pos >= 0)
			{
				return new XshdReference<XshdRuleSet>(ruleSet.Substring(0, pos), ruleSet.Substring(pos + 1));
			}
			return new XshdReference<XshdRuleSet>(null, ruleSet);
		}
		return new XshdReference<XshdRuleSet>();
	}

	private static XshdSpan ParseSpan(XmlReader reader)
	{
		var span = new XshdSpan();
		SetPosition(span, reader);
		span.BeginRegex = reader.GetAttribute("begin");
		span.EndRegex = reader.GetAttribute("end");
		span.Multiline = reader.GetBoolAttribute("multiline") ?? false;
		span.SpanColorReference = ParseColorReference(reader);
		span.RuleSetReference = ParseRuleSetReference(reader);
		if (!reader.IsEmptyElement)
		{
			reader.Read();
			while (reader.NodeType != XmlNodeType.EndElement)
			{
				Debug.Assert(reader.NodeType == XmlNodeType.Element);
				switch (reader.Name)
				{
					case "Begin":
						if (span.BeginRegex != null)
						{
							throw Error(reader, "Duplicate Begin regex");
						}
						span.BeginColorReference = ParseColorReference(reader);
						span.BeginRegex = reader.ReadElementContentAsString();
						span.BeginRegexType = XshdRegexType.IgnorePatternWhitespace;
						break;
					case "End":
						if (span.EndRegex != null)
						{
							throw Error(reader, "Duplicate End regex");
						}
						span.EndColorReference = ParseColorReference(reader);
						span.EndRegex = reader.ReadElementContentAsString();
						span.EndRegexType = XshdRegexType.IgnorePatternWhitespace;
						break;
					case "RuleSet":
						if (span.RuleSetReference.ReferencedElement != null)
						{
							throw Error(reader, "Cannot specify both inline RuleSet and RuleSet reference");
						}
						span.RuleSetReference = new XshdReference<XshdRuleSet>(ParseRuleSet(reader));
						reader.Read();
						break;
					default:
						throw new NotSupportedException("Unknown element " + reader.Name);
				}
			}
		}
		return span;
	}

	/// <summary>
	/// Sets the element's position to the XmlReader's position.
	/// </summary>
	private static void SetPosition(XshdElement element, XmlReader reader)
	{
		if (reader is IXmlLineInfo lineInfo)
		{
			element.LineNumber = lineInfo.LineNumber;
			element.ColumnNumber = lineInfo.LinePosition;
		}
	}

	#endregion
}