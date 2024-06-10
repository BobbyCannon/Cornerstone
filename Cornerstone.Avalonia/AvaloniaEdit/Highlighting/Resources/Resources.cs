#region References

using System.IO;
using System.Reflection;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Resources;

public static class ResourceLoader
{
	#region Constants

	private const string _prefix = "Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Resources.";

	#endregion

	#region Fields

	private static Assembly _assembly;

	#endregion

	#region Methods

	public static Stream OpenStream(string name)
	{
		var s = GetAssembly().GetManifestResourceStream(_prefix + name);
		return s ?? throw new FileNotFoundException("The resource file '" + name + "' was not found.");
	}

	internal static void RegisterBuiltInHighlightings(HighlightingManager.DefaultHighlightingManager hlm)
	{
		hlm.RegisterHighlighting("C#", [".cs"], "CSharp.xshd");
		//hlm.RegisterHighlighting("CSS", [".css"], "CSS.xshd");
		hlm.RegisterHighlighting("HTML", [".htm", ".html", ".cshtml"], "HTML.xshd");
		hlm.RegisterHighlighting("JavaScript", [".js"], "JavaScript.xshd");
		hlm.RegisterHighlighting("Json", [".json"], "Json.xshd");
		hlm.RegisterHighlighting("MarkDown", [".md"], "MarkDown.xshd");
		hlm.RegisterHighlighting("PowerShell", [".ps1", ".psm1", ".psd1"], "PowerShell.xshd");
		hlm.RegisterHighlighting("TSQL", [".sql"], "TSQL.xshd");
		hlm.RegisterHighlighting("XML", (".xml;.xsl;.xslt;.xsd;.manifest;.config;.addin;" +
				".xshd;.wxs;.wxi;.wxl;.proj;.csproj;.vbproj;.ilproj;" +
				".booproj;.build;.xfrm;.targets;.xaml;.xpt;" +
				".xft;.map;.wsdl;.disco;.ps1xml;.nuspec").Split(';'),
			"XML.xshd");
		hlm.RegisterHighlighting("XmlDoc", null, "XmlDoc.xshd");
	}

	private static Assembly GetAssembly()
	{
		return _assembly ??= typeof(ResourceLoader).GetTypeInfo().Assembly;
	}

	#endregion
}