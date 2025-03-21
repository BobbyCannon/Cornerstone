#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// Xshd visitor implementation that saves an .xshd file as XML.
/// </summary>
public sealed class SaveXshdVisitor : IXshdVisitor
{
	#region Constants

	/// <summary>
	/// XML namespace for XSHD.
	/// </summary>
	public const string Namespace = XshdLoader.Namespace;

	#endregion

	#region Fields

	private readonly XmlWriter _writer;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new SaveXshdVisitor instance.
	/// </summary>
	public SaveXshdVisitor(XmlWriter writer)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
	}

	#endregion

	#region Methods

	/// <summary>
	/// Writes the specified syntax definition.
	/// </summary>
	public void WriteDefinition(XshdSyntaxDefinition definition)
	{
		if (definition == null)
		{
			throw new ArgumentNullException(nameof(definition));
		}
		_writer.WriteStartElement("SyntaxDefinition", Namespace);
		if (definition.Name != null)
		{
			_writer.WriteAttributeString("name", definition.Name);
		}
		if (definition.Extensions != null)
		{
			_writer.WriteAttributeString("extensions", string.Join(";", definition.Extensions.ToArray()));
		}

		definition.AcceptElements(this);

		_writer.WriteEndElement();
	}

	object IXshdVisitor.VisitColor(XshdColor color)
	{
		_writer.WriteStartElement("Color", Namespace);
		if (color.Name != null)
		{
			_writer.WriteAttributeString("name", color.Name);
		}
		WriteColorAttributes(color);
		if (color.ExampleText != null)
		{
			_writer.WriteAttributeString("exampleText", color.ExampleText);
		}
		_writer.WriteEndElement();
		return null;
	}

	object IXshdVisitor.VisitImport(XshdImport import)
	{
		_writer.WriteStartElement("Import", Namespace);
		WriteRuleSetReference(import.RuleSetReference);
		_writer.WriteEndElement();
		return null;
	}

	object IXshdVisitor.VisitKeywords(XshdKeywords keywords)
	{
		_writer.WriteStartElement("Keywords", Namespace);
		WriteColorReference(keywords.ColorReference);
		foreach (var word in keywords.Words)
		{
			_writer.WriteElementString("Word", Namespace, word);
		}
		_writer.WriteEndElement();
		return null;
	}

	object IXshdVisitor.VisitRule(XshdRule rule)
	{
		_writer.WriteStartElement("Rule", Namespace);
		WriteColorReference(rule.ColorReference);

		_writer.WriteString(rule.Regex);

		_writer.WriteEndElement();
		return null;
	}

	object IXshdVisitor.VisitRuleSet(XshdRuleSet ruleSet)
	{
		_writer.WriteStartElement("RuleSet", Namespace);

		if (ruleSet.Name != null)
		{
			_writer.WriteAttributeString("name", ruleSet.Name);
		}
		WriteBoolAttribute("ignoreCase", ruleSet.IgnoreCase);

		ruleSet.AcceptElements(this);

		_writer.WriteEndElement();
		return null;
	}

	object IXshdVisitor.VisitSpan(XshdSpan span)
	{
		_writer.WriteStartElement("Span", Namespace);
		WriteColorReference(span.SpanColorReference);
		if ((span.BeginRegexType == XshdRegexType.Default) && (span.BeginRegex != null))
		{
			_writer.WriteAttributeString("begin", span.BeginRegex);
		}
		if ((span.EndRegexType == XshdRegexType.Default) && (span.EndRegex != null))
		{
			_writer.WriteAttributeString("end", span.EndRegex);
		}
		WriteRuleSetReference(span.RuleSetReference);
		if (span.Multiline)
		{
			_writer.WriteAttributeString("multiline", "true");
		}

		if (span.BeginRegexType == XshdRegexType.IgnorePatternWhitespace)
		{
			WriteBeginEndElement("Begin", span.BeginRegex, span.BeginColorReference);
		}
		if (span.EndRegexType == XshdRegexType.IgnorePatternWhitespace)
		{
			WriteBeginEndElement("End", span.EndRegex, span.EndColorReference);
		}

		span.RuleSetReference.InlineElement?.AcceptVisitor(this);

		_writer.WriteEndElement();
		return null;
	}

	private void WriteBeginEndElement(string elementName, string regex, XshdReference<XshdColor> colorReference)
	{
		if (regex != null)
		{
			_writer.WriteStartElement(elementName, Namespace);
			WriteColorReference(colorReference);
			_writer.WriteString(regex);
			_writer.WriteEndElement();
		}
	}

	private void WriteBoolAttribute(string attributeName, bool? value)
	{
		if (value != null)
		{
			_writer.WriteAttributeString(attributeName, value.Value ? "true" : "false");
		}
	}

	[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "The file format requires lowercase, and all possible values are English-only")]
	private void WriteColorAttributes(XshdColor color)
	{
		if (color.Foreground != null)
		{
			_writer.WriteAttributeString("foreground", color.Foreground.ToString());
		}
		if (color.Background != null)
		{
			_writer.WriteAttributeString("background", color.Background.ToString());
		}
		if (color.FontWeight != null)
		{
			_writer.WriteAttributeString("fontWeight", color.FontWeight.Value.ToString().ToLowerInvariant());
		}
		if (color.FontStyle != null)
		{
			_writer.WriteAttributeString("fontStyle", color.FontStyle.Value.ToString().ToLowerInvariant());
		}
	}

	private void WriteColorReference(XshdReference<XshdColor> color)
	{
		if (color.InlineElement != null)
		{
			WriteColorAttributes(color.InlineElement);
		}
		else if (color.ReferencedElement != null)
		{
			if (color.ReferencedDefinition != null)
			{
				_writer.WriteAttributeString("color", color.ReferencedDefinition + "/" + color.ReferencedElement);
			}
			else
			{
				_writer.WriteAttributeString("color", color.ReferencedElement);
			}
		}
	}

	private void WriteRuleSetReference(XshdReference<XshdRuleSet> ruleSetReference)
	{
		if (ruleSetReference.ReferencedElement != null)
		{
			if (ruleSetReference.ReferencedDefinition != null)
			{
				_writer.WriteAttributeString("ruleSet", ruleSetReference.ReferencedDefinition + "/" + ruleSetReference.ReferencedElement);
			}
			else
			{
				_writer.WriteAttributeString("ruleSet", ruleSetReference.ReferencedElement);
			}
		}
	}

	#endregion
}