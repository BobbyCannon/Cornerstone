#region References

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.EditorHost;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class CornerstoneBrushCompletionTests
{
	private static readonly CompletionMetadata PresentationMetadata;

	static CornerstoneBrushCompletionTests()
	{
		var presentation = Path.GetFullPath(Path.Combine(
			typeof(CornerstoneBrushCompletionTests).Assembly.Location,
			"..", "..", "..", "..", "..", "..",
			"Cornerstone.Presentation", "bin", "Debug", "net10.0", "Cornerstone.Presentation.dll"));
		Assert.IsTrue(File.Exists(presentation), presentation);
		PresentationMetadata = new MetadataReader(new DnlibMetadataProvider())
			.GetForTargetAssembly(new FolderAssemblyProvider(presentation));
	}

	[TestMethod]
	public void ForegroundBlueInsideDocumentReturnsBlue()
	{
		var xaml = """
			<UserControl xmlns="https://github.com/BobbyCannon/Cornerstone"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<TextBlock Classes="Dim"
						Text="About This"
						FontSize="26"
						Foreground="Bl"
						HorizontalAlignment="Center" />
			</UserControl>
			""";
		var marker = "Foreground=\"Bl";
		var caret = xaml.IndexOf(marker) + marker.Length;
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, caret);
		Assert.IsNotNull(set, "completion set was null at caret " + caret);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "Blue"));
		Assert.AreEqual(caret - 2, set.StartPosition);

		var afterQuote = xaml.IndexOf("Foreground=\"Bl\"") + "Foreground=\"Bl\"".Length;
		var after = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, afterQuote);
		Assert.IsNotNull(after, "completion set was null with the caret on the closing quote");
		Assert.IsTrue(after.Completions.Any(completion => completion.InsertText == "Blue"));
		Assert.AreEqual(afterQuote - 3, after.StartPosition);
	}

	[TestMethod]
	public void HorizontalAlignmentReturnsCenter()
	{
		var xaml = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" HorizontalAlignment=\"C";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "Center"));
	}

	[TestMethod]
	public async Task SameAssemblyNameOnTwoPathsKeepsHints()
	{
		var presentation = Path.GetFullPath(Path.Combine(
			typeof(CornerstoneBrushCompletionTests).Assembly.Location,
			"..", "..", "..", "..", "..", "..",
			"Cornerstone.Presentation", "bin", "Debug", "net10.0", "Cornerstone.Presentation.dll"));
		var root = Path.Combine(Path.GetTempPath(), "cscache-" + Guid.NewGuid().ToString("N"));
		var copy = Path.Combine(root, "other", "Cornerstone.Presentation.dll");
		Directory.CreateDirectory(Path.GetDirectoryName(copy));
		File.Copy(presentation, copy);
		var solution = Path.Combine(root, "solution");
		Directory.CreateDirectory(solution);
		MetadataCache.UseSolutionDirectory(solution);
		try
		{
			await MetadataCache.GetHitAsync(new[] { presentation, copy }, null);
			var cacheDir = Path.Combine(solution, ".cscache");
			AssertHasColorAndAlignment(ReadFragment(cacheDir, presentation));
			AssertHasColorAndAlignment(ReadFragment(cacheDir, copy));
		}
		finally
		{
			try
			{
				Directory.Delete(root, true);
			}
			catch (IOException)
			{
				// The cache files can stay in temp. The assertions already ran.
			}
		}
	}

	private static CompletionMetadata ReadFragment(string cacheDir, string assemblyPath)
	{
		var info = new FileInfo(assemblyPath);
		var fingerprint = assemblyPath + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks + "\n";
		CompletionMetadata metadata;
		Assert.IsTrue(MetadataDiskCache.TryRead(cacheDir, assemblyPath, fingerprint, out metadata), assemblyPath);
		return metadata;
	}

	private static void AssertHasColorAndAlignment(CompletionMetadata metadata)
	{
		var color = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" Foreground=\"R";
		var colors = new CompletionEngine().GetCompletions(metadata, color, color.Length);
		Assert.IsNotNull(colors);
		Assert.IsTrue(colors.Completions.Any(completion => completion.InsertText == "Red"));

		var alignment = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" HorizontalAlignment=\"C";
		var alignments = new CompletionEngine().GetCompletions(metadata, alignment, alignment.Length);
		Assert.IsNotNull(alignments);
		Assert.IsTrue(alignments.Completions.Any(completion => completion.InsertText == "Center"));
	}

	[TestMethod]
	public void ForegroundPartialColorReturnsRed()
	{
		var xaml = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" Foreground=\"R";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "Red"));
	}

	[TestMethod]
	public void TextDecorationsReturnsUnderline()
	{
		var xaml = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" TextDecorations=\"U";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "Underline"));
	}

	[TestMethod]
	public void CursorReturnsHand()
	{
		var xaml = "<Button xmlns=\"https://github.com/BobbyCannon/Cornerstone\" Cursor=\"Ha";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "Hand"));
	}

	[TestMethod]
	public void TextTrimmingReturnsWordEllipsis()
	{
		var xaml = "<TextBlock xmlns=\"https://github.com/BobbyCannon/Cornerstone\" TextTrimming=\"W";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "WordEllipsis"));
	}

	[TestMethod]
	public void RenderTransformReturnsRotate()
	{
		var xaml = "<Button xmlns=\"https://github.com/BobbyCannon/Cornerstone\" RenderTransform=\"ro";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "rotate"));
	}

	[TestMethod]
	public void EffectReturnsBlur()
	{
		var xaml = "<Button xmlns=\"https://github.com/BobbyCannon/Cornerstone\" Effect=\"bl";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "blur("));
	}

	[TestMethod]
	public void CacheModeReturnsBitmapCache()
	{
		var xaml = "<Button xmlns=\"https://github.com/BobbyCannon/Cornerstone\" CacheMode=\"Bi";
		var set = new CompletionEngine().GetCompletions(PresentationMetadata, xaml, xaml.Length);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(completion => completion.InsertText == "BitmapCache"));
	}

	[TestMethod]
	public void ImageSourceUsesImageHints()
	{
		var image = PresentationMetadata.Namespaces
			.SelectMany(pair => pair.Value.Values)
			.FirstOrDefault(type => type.Name == "Image");
		var source = image?.Properties.FirstOrDefault(property => property.Name == "Source");
		Assert.IsNotNull(source);
		Assert.IsNotNull(source.Type);
		Assert.IsTrue(source.Type.HasHintValues);
	}
}
