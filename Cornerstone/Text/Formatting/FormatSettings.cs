#region References

using System.Linq;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Text.Parsing.CSharp;
using Cornerstone.Text.Parsing.Json;
using Cornerstone.Text.Parsing.Markdown;
using Cornerstone.Text.Parsing.PowerShell;
using Cornerstone.Text.Parsing.Xml;

#endregion

namespace Cornerstone.Text.Formatting;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class FormatSettings : CornerstoneObject
{
	#region Constructors

	public FormatSettings()
	{
		General = new DocumentFormatOptions();
		Json = new JsonFormatOptions();
		Xml = new XmlFormatOptions();
		Xaml = new XmlFormatOptions();
		Xaml.AttributesOnNewLine = true;
		Html = new XmlFormatOptions();
		Html.Dialect = XmlFormatDialect.Html;
		CSharp = new CSharpFormatOptions();
		Markdown = new MarkdownFormatOptions();
		PowerShell = new PowerShellFormatOptions();
		CopyGeneralTo(Json);
		CopyGeneralTo(Xml);
		CopyGeneralTo(Xaml);
		CopyGeneralTo(Html);
		CopyGeneralTo(CSharp);
		CopyGeneralTo(Markdown);
		CopyGeneralTo(PowerShell);
	}

	#endregion

	#region Properties

	[Notify]
	public partial CSharpFormatOptions CSharp { get; set; }

	[Notify]
	public partial DocumentFormatOptions General { get; set; }

	[Notify]
	public partial XmlFormatOptions Html { get; set; }

	[Notify]
	public partial JsonFormatOptions Json { get; set; }

	[Notify]
	public partial MarkdownFormatOptions Markdown { get; set; }

	[Notify]
	public partial PowerShellFormatOptions PowerShell { get; set; }

	[Notify]
	public partial XmlFormatOptions Xaml { get; set; }

	[Notify]
	public partial XmlFormatOptions Xml { get; set; }

	#endregion

	#region Methods

	public DocumentFormatOptions Resolve(string extension)
	{
		var value = NormalizeExtension(extension);
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}

		if (JsonTokenizer.Extensions.Contains(value))
		{
			return Json;
		}

		if (CSharpTokenizer.Extensions.Contains(value))
		{
			return CSharp;
		}

		if (IsXaml(value))
		{
			return Xaml;
		}

		if (IsHtml(value))
		{
			return Html;
		}

		if (XmlTokenizer.Extensions.Contains(value))
		{
			return Xml;
		}

		if (MarkdownTokenizer.Extensions.Contains(value))
		{
			return Markdown;
		}

		if (PowerShellTokenizer.Extensions.Contains(value))
		{
			return PowerShell;
		}

		return null;
	}

	private void CopyGeneralTo(DocumentFormatOptions target)
	{
		if ((target == null) || (General == null))
		{
			return;
		}

		target.IndentCharacter = General.IndentCharacter;
		target.IndentCount = General.IndentCount;
		target.LineEndingMode = General.LineEndingMode;
	}

	private static bool IsHtml(string value)
	{
		return (value == "html") || (value == "htm");
	}

	private static bool IsXaml(string value)
	{
		return (value == "cxaml") || (value == "axaml") || (value == "xaml");
	}

	private static string NormalizeExtension(string extension)
	{
		return string.IsNullOrWhiteSpace(extension)
			? null
			: extension.TrimStart('.').ToLowerInvariant();
	}

	#endregion
}
