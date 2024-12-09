#region References

using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Parsers.Html;

public class CodeSyntaxHtmlWriter : HtmlWriter
{
	#region Methods

	public override void Start()
	{
		base.Start();
		Builder.Append("<pre>").PopIndent();
	}

	public override void Stop()
	{
		Builder.Append("</pre>");
		base.Stop();
	}

	#endregion
}