#region References

using System;
using Cornerstone.VisualStudio.Core.Cleanup;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests.Cleanup;

public class CleanupPipelineTests
{
	#region Methods

	[Fact]
	public void CleanTrimsTrailingWhitespace()
	{
		var options = HygieneOnly();
		var input = "line1   \r\nline2\t\t\r\n";
		var result = CleanupPipeline.Clean(input, options);

		Assert.True(result.HasTextChange);
		Assert.Equal("line1\r\nline2\r\n", result.Text);
	}

	[Fact]
	public void CleanEnsuresFinalNewline()
	{
		var options = HygieneOnly();
		options.TrimTrailingWhitespace = false;
		var result = CleanupPipeline.Clean("hello", options);

		Assert.True(result.HasTextChange);
		Assert.Equal("hello\r\n", result.Text);
	}

	[Fact]
	public void CleanNormalizesToLf()
	{
		var options = HygieneOnly();
		options.NormalizeLineEndings = CleanupLineEndingMode.Lf;
		var result = CleanupPipeline.Clean("a\r\nb\r\n", options);

		Assert.Equal("a\nb\n", result.Text);
	}

	[Fact]
	public void CleanMalformedXmlStillAppliesHygiene()
	{
		var options = FullOptions();
		var input = "<Grid>\r\n  <Button   \r\n";
		var result = CleanupPipeline.Clean(input, options);

		Assert.True(result.HasTextChange);
		Assert.False(result.StructuralApplied);
		Assert.DoesNotContain("   \r\n", result.Text);
		Assert.Contains("well-formed", result.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void CleanFormatsAndSortsAttributes()
	{
		var options = FullOptions();
		var input =
			"<UserControl Width=\"100\" xmlns=\"https://github.com/avaloniaui\" x:Name=\"Root\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
			"<Button Height=\"20\" Width=\"10\"/></UserControl>";

		var result = CleanupPipeline.Clean(input, options);

		Assert.True(result.HasTextChange);
		Assert.True(result.StructuralApplied);
		// default xmlns before xmlns:x; Name early; Width after
		var rootOpen = result.Text.Substring(0, result.Text.IndexOf('>'));
		var xmlnsIdx = rootOpen.IndexOf("xmlns=", StringComparison.Ordinal);
		var xmlnsXIdx = rootOpen.IndexOf("xmlns:x=", StringComparison.Ordinal);
		var nameIdx = rootOpen.IndexOf("x:Name=", StringComparison.Ordinal);
		var widthIdx = rootOpen.IndexOf("Width=", StringComparison.Ordinal);
		Assert.True(xmlnsIdx >= 0 && xmlnsXIdx > xmlnsIdx);
		Assert.True(nameIdx > xmlnsXIdx);
		Assert.True(widthIdx > nameIdx);
		Assert.Contains("\n", result.Text);
	}

	[Fact]
	public void CleanPrefersSelfClosingEmptyElements()
	{
		var options = FullOptions();
		var input = "<Grid xmlns=\"https://github.com/avaloniaui\"><Button></Button></Grid>";
		var result = CleanupPipeline.Clean(input, options);

		Assert.True(result.StructuralApplied);
		Assert.Contains("<Button", result.Text);
		Assert.DoesNotContain("</Button>", result.Text);
	}

	[Fact]
	public void CleanSelectionDoesNotRunStructural()
	{
		var options = FullOptions();
		var input = "<Button Width=\"1\" Height=\"2\"></Button>   ";
		var result = CleanupPipeline.CleanSelection(input, options);

		Assert.False(result.StructuralApplied);
		Assert.Equal("<Button Width=\"1\" Height=\"2\"></Button>", result.Text);
	}

	[Fact]
	public void MatchesExtensionParsesConfiguredList()
	{
		var options = new CleanupOptions { FileExtensions = "axaml, .xaml;CS" };
		Assert.True(options.MatchesExtension(@"C:\a\Main.axaml"));
		Assert.True(options.MatchesExtension("View.xaml"));
		Assert.True(options.MatchesExtension("Foo.cs"));
		Assert.False(options.MatchesExtension("Foo.txt"));
	}

	[Fact]
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
		Assert.Equal(CleanupOutcome.Skipped, result.Outcome);
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
