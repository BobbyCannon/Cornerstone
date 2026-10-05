#region References

using System;
using Cornerstone.VisualStudio.Core.Cleanup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Cleanup;

[TestClass]
public class CleanupPipelineTests
{
	#region Methods

	[TestMethod]
	public void CleanTrimsTrailingWhitespace()
	{
		var options = HygieneOnly();
		var input = "line1   \r\nline2\t\t\r\n";
		var result = CleanupPipeline.Clean(input, options);

		Assert.IsTrue(result.HasTextChange);
		Assert.AreEqual("line1\r\nline2\r\n", result.Text);
	}

	[TestMethod]
	public void CleanEnsuresFinalNewline()
	{
		var options = HygieneOnly();
		options.TrimTrailingWhitespace = false;
		var result = CleanupPipeline.Clean("hello", options);

		Assert.IsTrue(result.HasTextChange);
		Assert.AreEqual("hello\r\n", result.Text);
	}

	[TestMethod]
	public void CleanNormalizesToLf()
	{
		var options = HygieneOnly();
		options.NormalizeLineEndings = CleanupLineEndingMode.Lf;
		var result = CleanupPipeline.Clean("a\r\nb\r\n", options);

		Assert.AreEqual("a\nb\n", result.Text);
	}

	[TestMethod]
	public void CleanMalformedXmlStillAppliesHygiene()
	{
		var options = FullOptions();
		var input = "<Grid>\r\n  <Button   \r\n";
		var result = CleanupPipeline.Clean(input, options);

		Assert.IsTrue(result.HasTextChange);
		Assert.IsFalse(result.StructuralApplied);
		Assert.IsFalse(result.Text.Contains("   \r\n"));
		StringAssert.Contains(result.Message.ToLowerInvariant(), "well-formed");
	}

	[TestMethod]
	public void CleanFormatsAndSortsAttributes()
	{
		var options = FullOptions();
		var input =
			"<UserControl Width=\"100\" xmlns=\"https://github.com/avaloniaui\" x:Name=\"Root\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
			"<Button Height=\"20\" Width=\"10\"/></UserControl>";

		var result = CleanupPipeline.Clean(input, options);

		Assert.IsTrue(result.HasTextChange);
		Assert.IsTrue(result.StructuralApplied);
		// default xmlns before xmlns:x; Name early; Width after
		var rootOpen = result.Text.Substring(0, result.Text.IndexOf('>'));
		var xmlnsIdx = rootOpen.IndexOf("xmlns=", StringComparison.Ordinal);
		var xmlnsXIdx = rootOpen.IndexOf("xmlns:x=", StringComparison.Ordinal);
		var nameIdx = rootOpen.IndexOf("x:Name=", StringComparison.Ordinal);
		var widthIdx = rootOpen.IndexOf("Width=", StringComparison.Ordinal);
		Assert.IsTrue(xmlnsIdx >= 0 && xmlnsXIdx > xmlnsIdx);
		Assert.IsTrue(nameIdx > xmlnsXIdx);
		Assert.IsTrue(widthIdx > nameIdx);
		StringAssert.Contains(result.Text, "\n");
	}

	[TestMethod]
	public void CleanPrefersSelfClosingEmptyElements()
	{
		var options = FullOptions();
		var input = "<Grid xmlns=\"https://github.com/avaloniaui\"><Button></Button></Grid>";
		var result = CleanupPipeline.Clean(input, options);

		Assert.IsTrue(result.StructuralApplied);
		StringAssert.Contains(result.Text, "<Button");
		Assert.IsFalse(result.Text.Contains("</Button>"));
	}

	[TestMethod]
	public void CleanSelectionDoesNotRunStructural()
	{
		var options = FullOptions();
		var input = "<Button Width=\"1\" Height=\"2\"></Button>   ";
		var result = CleanupPipeline.CleanSelection(input, options);

		Assert.IsFalse(result.StructuralApplied);
		Assert.AreEqual("<Button Width=\"1\" Height=\"2\"></Button>", result.Text);
	}

	[TestMethod]
	public void MatchesExtensionParsesConfiguredList()
	{
		var options = new CleanupOptions { FileExtensions = "axaml, .xaml;CS" };
		Assert.IsTrue(options.MatchesExtension(@"C:\a\Main.axaml"));
		Assert.IsTrue(options.MatchesExtension("View.xaml"));
		Assert.IsTrue(options.MatchesExtension("Foo.cs"));
		Assert.IsFalse(options.MatchesExtension("Foo.txt"));
	}

	[TestMethod]
	public void CleanNoRulesSkips()
	{
		var options = new CleanupOptions
		{
			TrimTrailingWhitespace = false,
			EnsureFinalNewline = false,
			NormalizeLineEndings = CleanupLineEndingMode.Keep,
			FormatXml = false,
			SortXmlns = false,
			SortAttributes = false,
			PreferSelfClosing = false
		};

		var result = CleanupPipeline.Clean("<a/>", options);
		Assert.AreEqual(CleanupOutcome.Skipped, result.Outcome);
	}

	private static CleanupOptions HygieneOnly()
	{
		return new CleanupOptions
		{
			TrimTrailingWhitespace = true,
			EnsureFinalNewline = true,
			NormalizeLineEndings = CleanupLineEndingMode.Crlf,
			FormatXml = false,
			SortXmlns = false,
			SortAttributes = false,
			PreferSelfClosing = false
		};
	}

	private static CleanupOptions FullOptions()
	{
		return new CleanupOptions
		{
			TrimTrailingWhitespace = true,
			EnsureFinalNewline = true,
			NormalizeLineEndings = CleanupLineEndingMode.Lf,
			FormatXml = true,
			SortXmlns = true,
			SortAttributes = true,
			PreferSelfClosing = true,
			IndentSize = 2
		};
	}

	#endregion
}
