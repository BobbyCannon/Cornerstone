#region References

using System.Collections.Concurrent;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Xml;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Html;

public class HtmlWriter
{
	#region Fields

	private readonly ConcurrentStack<string> _elementStack;

	#endregion

	#region Constructors

	public HtmlWriter()
	{
		_elementStack = new ConcurrentStack<string>();

		Builder = new TextBuilder();
	}

	#endregion

	#region Properties

	protected TextBuilder Builder { get; }

	#endregion

	#region Methods

	public void PopElement(bool dedent)
	{
		if (dedent)
		{
			Builder.PopIndent();
		}

		_elementStack.TryPop(out var poppedElement);

		WriteRaw("</");
		WriteRaw(poppedElement);
		WriteRaw(">");

		if (dedent)
		{
			Builder.NewLine();
		}
	}

	public void PopElementIfNot(string element, bool dedent)
	{
		if (_elementStack.IsEmpty
			|| !_elementStack.TryPeek(out var currentElement)
			|| (currentElement == element))
		{
			return;
		}

		PopElement(dedent);
	}

	public void PushElement(string element, bool indent)
	{
		WriteRaw("<");
		WriteRaw(element);
		WriteRaw(">");

		_elementStack.Push(element);

		if (indent)
		{
			Builder.NewLine();
			Builder.PushIndent();
		}
	}

	public void PushElementIfNot(string element, bool indent)
	{
		if (_elementStack.TryPeek(out var currentElement)
			&& (currentElement == element))
		{
			return;
		}

		PushElement(element, indent);
	}

	public virtual void Start()
	{
		AppendHtmlElementHeadStart();
		WriteStyles();
		AppendHtmlElementHeadEnd();
		AppendHtmlElementBodyStart();
	}

	public virtual void Stop()
	{
		AppendHtmlElementBodyEnd();
		AppendHtmlElementHtmlEnd();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Builder.ToString();
	}

	public static string WrapHtmlSnippet(string html)
	{
		var writer = new HtmlWriter();
		writer.AppendHtmlElementHeadStart();
		writer.WriteStyles();
		writer.AppendHtmlElementHeadEnd();
		writer.AppendHtmlElementBodyStart();
		writer.WriteRaw(html);
		writer.AppendHtmlElementBodyEnd();
		writer.AppendHtmlElementHtmlEnd();
		return writer.ToString();
	}

	public void WriteElement(string elementName, string value, params XmlAttribute[] attributes)
	{
		WriteRaw("<");
		WriteRaw(elementName);

		foreach (var attribute in attributes)
		{
			Builder.Append(" ");
			Builder.Append(attribute.Name);
			Builder.Append("=");
			Builder.Append("\"");
			Builder.Append(attribute.Value);
			Builder.Append("\"");
		}

		WriteRaw(">");
		WriteRaw(value);
		WriteRaw("</");
		WriteRaw(elementName);
		WriteRaw(">");
	}

	public void WriteRaw(string value)
	{
		Builder.Append(value);
	}

	public void WriteSpan(string value, SyntaxColor color)
	{
		Builder.Append($"<span class=\"syntax{color}\">");
		Builder.Append(value);
		Builder.Append("</span>");
	}

	protected void AddStyle(string name, params string[] values)
	{
		Builder.Append(name);
		Builder.AppendLineThenPushIndent(" {");

		foreach (var value in values)
		{
			Builder.AppendLine(value);
		}

		Builder.PopIndentThenAppendLine("}");
	}

	protected virtual void WriteStyles()
	{
		AddStyle("*, *::before, *::after",
			"-webkit-box-sizing: border-box;",
			"-moz-box-sizing: border-box;",
			"box-sizing: border-box;"
		);

		AddStyle("html, body",
			"background: #191919;",
			"color: #D8D8D8;",
			"font-style: normal;",
			"font-size: 16px;",
			"font-size: 1rem;",
			"line-height: 1.5;"
		);
		
		for (var i = 1; i <= 6; i++)
		{
			AddStyle($"h{i}",
				$"font-size: {26 - i}px;",
				"line-height: 1;",
				"margin-block-start: 0.25em;",
				"margin-block-end: 0.5em;",
				"margin-inline-start: 0px;",
				"margin-inline-end: 0px;"
			);
		}

		foreach (var style in EnumExtensions.GetAllEnumDetails<SyntaxColor>())
		{
			AddStyle($".syntax{style.Value.Name}", $"color: {style.Key.ToHtmlColor()};");
		}

		AddStyle("a", $"color: {SyntaxColor.Link.ToHtmlColor()}");

		AddStyle("pre",
			"background: #212121;",
			"padding: 10px;",
			"font-family: monospace, monospace;"
		);

		AddStyle("blockquote",
			"background: #252525;",
			"border-left: 10px solid #323232;",
			"margin: 10px;",
			"padding: 10px;"
		);
	}

	private void AppendHtmlElementBodyEnd()
	{
		Builder.AppendLineThenPopIndent("</body>");
	}

	private void AppendHtmlElementBodyStart()
	{
		Builder.Append("<body>");
	}

	private void AppendHtmlElementHeadEnd()
	{
		Builder.PopIndentThenAppendLine("</style>")
			.PopIndentThenAppendLine("</head>");
	}

	private void AppendHtmlElementHeadStart()
	{
		Builder.AppendLineThenPushIndent("<html>")
			.AppendLineThenPushIndent("<head>")
			.AppendLineThenPushIndent("<style>");
	}

	private void AppendHtmlElementHtmlEnd()
	{
		Builder.AppendLineThenPopIndent("</html>");
	}

	#endregion
}