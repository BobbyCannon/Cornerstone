#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media;

[TestClass]
public class GlyphRunTests
{
	#region Methods

	[PresentationTestMethod]
	public void GlyphRunWithLeadingSpaceHasCorrectInkBounds()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
			var shapedBuffer = TextShaper.Current.ShapeText(" I", options);

			var glyphRun1 = CreateGlyphRun(shapedBuffer);
			var bounds = glyphRun1.InkBounds;

			CornerstoneTest.IsTrue(bounds.Left > 0);
		}
	}

	[PresentationTestMethod]
	public void ShouldAddHalfLineGapToBaseline()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
			var shapedBuffer = TextShaper.Current.ShapeText("F", options);

			var textMetrics = new TextMetrics(shapedBuffer.GlyphTypeface, 14);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var expectedBaseline = -textMetrics.Ascent + (textMetrics.LineGap / 2);

			CornerstoneTest.AreEqual(expectedBaseline, glyphRun.Metrics.Baseline);
		}
	}

	[PresentationTestMethod]
	public void ShouldCharacterHitFromDistanceZeroWidth()
	{
		const string df7Font = "resm:Cornerstone.Presentation.UnitTests.Skia.Fonts?assembly=Cornerstone.Presentation.UnitTests#DF7segHMI";
		const string text = "3,47-=?:#";

		using (Start())
		{
			var typeface = new Typeface(df7Font);
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 0);
			var shapedBuffer = TextShaper.Current.ShapeText(text, options);

			CornerstoneTest.NotEmpty(shapedBuffer);

			var firstGlyphInfo = shapedBuffer[0];

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var characterHit = glyphRun.GetCharacterHitFromDistance(firstGlyphInfo.GlyphAdvance, out _);

			CornerstoneTest.AreEqual(2, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
		}
	}

	[DataRow("ABC012345", 0)] //LeftToRight
	[DataRow("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
	[PresentationTestMethod]
	public void ShouldGetCharacterHitFromDistance(string text, int direction)
	{
		using (Start())
		{
			var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, (sbyte) direction, CultureInfo.CurrentCulture);
			var shapedBuffer =
				TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			if (glyphRun.IsLeftToRight)
			{
				var characterHit =
					glyphRun.GetCharacterHitFromDistance(glyphRun.Bounds.Width, out _);

				CornerstoneTest.AreEqual(glyphRun.Characters.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
			}
			else
			{
				var characterHit =
					glyphRun.GetCharacterHitFromDistance(0, out _);

				CornerstoneTest.AreEqual(glyphRun.Characters.Length, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
			}

			var rects = BuildRects(glyphRun);

			var lastCluster = -1;
			var index = 0;

			if (!glyphRun.IsLeftToRight)
			{
				rects.Reverse();
			}

			foreach (var rect in rects)
			{
				var currentCluster = glyphRun.GlyphInfos[index].GlyphCluster;

				while ((currentCluster == lastCluster) && ((index + 1) < glyphRun.GlyphInfos.Count))
				{
					currentCluster = glyphRun.GlyphInfos[++index].GlyphCluster;
				}

				//Non trailing edge
				var distance = glyphRun.IsLeftToRight ? rect.Left : rect.Right;

				var characterHit = glyphRun.GetCharacterHitFromDistance(distance, out _);

				CornerstoneTest.AreEqual(currentCluster, characterHit.FirstCharacterIndex + characterHit.TrailingLength);

				lastCluster = currentCluster;

				index++;
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHitNonTrailingRightToLeft()
	{
		const string text = "נִקּוּד";

		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 1);
			var shapedBuffer = TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			//Get the distance to the left side of the first glyph
			var actualDistance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(text.Length));

			var expectedDistance = 0.0;

			CornerstoneTest.AreEqual(expectedDistance, actualDistance, 2);

			//Get the distance to the right side of the first glyph
			actualDistance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(text.Length - 1));

			expectedDistance = shapedBuffer[0].GlyphAdvance;

			CornerstoneTest.AreEqual(expectedDistance, actualDistance, 2);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHitWithinCluster()
	{
		var text = "எடுத்துக்காட்டு வழி வினவல்";

		using (Start())
		{
			var cp = Codepoint.ReadAt(text, 0, out _);

			CornerstoneTest.IsTrue(FontManager.Current.TryMatchCharacter(cp, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal, null, null, out var typeface));

			var options = new TextShaperOptions(typeface.GlyphTypeface, 12);

			var shapedBuffer = TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var clusterWidth = new List<double>();
			var distances = new List<double>();
			var clusters = new List<int>();
			var lastCluster = -1;
			var currentDistance = 0.0;
			var currentAdvance = 0.0;

			foreach (var glyphInfo in shapedBuffer)
			{
				if (lastCluster != glyphInfo.GlyphCluster)
				{
					clusterWidth.Add(currentAdvance);
					distances.Add(currentDistance);
					clusters.Add(glyphInfo.GlyphCluster);

					currentAdvance = 0;
				}

				lastCluster = glyphInfo.GlyphCluster;
				currentDistance += glyphInfo.GlyphAdvance;
				currentAdvance += glyphInfo.GlyphAdvance;
			}

			clusterWidth.RemoveAt(0);

			clusterWidth.Add(currentAdvance);

			var expectedLeftHit = new CharacterHit(11);

			var distance = glyphRun.GetDistanceFromCharacterHit(expectedLeftHit);

			var expectedLeft = distances[6];

			CornerstoneTest.AreEqual(expectedLeft, distance);

			var leftHit = glyphRun.GetCharacterHitFromDistance(expectedLeft, out _);

			CornerstoneTest.AreEqual(11, leftHit.FirstCharacterIndex + leftHit.TrailingLength);

			var expectedRight = distances[7];

			distance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(12));

			CornerstoneTest.AreEqual(expectedRight, distance);

			var expectedRightHit = new CharacterHit(13);

			distance = glyphRun.GetDistanceFromCharacterHit(expectedRightHit);

			CornerstoneTest.AreEqual(expectedRight, distance);

			var rightHit = glyphRun.GetCharacterHitFromDistance(expectedRight, out _);

			CornerstoneTest.AreEqual(13, rightHit.FirstCharacterIndex + rightHit.TrailingLength);
		}
	}

	[PresentationTestMethod]
	public void ShouldGetDistanceFromCharacterHitZeroWidth()
	{
		const string text = "נִקּוּד";

		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14, 1);
			var shapedBuffer = TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var clusters = glyphRun.GlyphInfos.GroupBy(x => x.GlyphCluster);

			var rightSideDistances = new List<double>();

			var leftSideDistances = new List<double>();

			var currentX = 0.0;

			foreach (var cluster in clusters)
			{
				leftSideDistances.Add(currentX);

				currentX += cluster.Sum(x => x.GlyphAdvance);

				rightSideDistances.Add(currentX);
			}

			var characterIndices = clusters.Select(x => x.First().GlyphCluster).ToList();

			var characterHit = new CharacterHit(text.Length);

			for (var i = 0; i < characterIndices.Count; i++)
			{
				var characterIndex = characterIndices[i];

				var leftSideDistance = leftSideDistances[i];

				var leftSideCharacterHit = glyphRun.GetCharacterHitFromDistance(leftSideDistance, out _);

				var distance = glyphRun.GetDistanceFromCharacterHit(leftSideCharacterHit);

				CornerstoneTest.AreEqual(leftSideDistance, distance, 2);

				var previousCharacterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

				distance = glyphRun.GetDistanceFromCharacterHit(new CharacterHit(characterIndex));

				var rightSideDistance = rightSideDistances[i];

				CornerstoneTest.AreEqual(rightSideDistance, distance, 2);

				characterHit = previousCharacterHit;
			}
		}
	}

	[DataRow("ABC012345", 0)] //LeftToRight
	[DataRow("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
	[PresentationTestMethod]
	public void ShouldGetNextCharacterHit(string text, int direction)
	{
		using (Start())
		{
			var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, (sbyte) direction, CultureInfo.CurrentCulture);
			var shapedBuffer =
				TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var characterHit = new CharacterHit(0);
			var rects = BuildRects(glyphRun);

			if (glyphRun.IsLeftToRight)
			{
				foreach (var rect in rects)
				{
					characterHit = glyphRun.GetNextCaretCharacterHit(characterHit);

					var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

					CornerstoneTest.AreEqual(rect.Right, distance);
				}
			}
			else
			{
				foreach (var rect in rects)
				{
					characterHit = glyphRun.GetNextCaretCharacterHit(characterHit);

					var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

					CornerstoneTest.AreEqual(rect.Left, distance);
				}
			}
		}
	}

	[DataRow("ABC012345", 0)] //LeftToRight
	[DataRow("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן", 1)] //RightToLeft
	[PresentationTestMethod]
	public void ShouldGetPreviousCharacterHit(string text, int direction)
	{
		using (Start())
		{
			var options = new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, (sbyte) direction, CultureInfo.CurrentCulture);
			var shapedBuffer =
				TextShaper.Current.ShapeText(text, options);

			var glyphRun = CreateGlyphRun(shapedBuffer);

			var characterHit = new CharacterHit(text.Length);
			var rects = BuildRects(glyphRun);

			rects.Reverse();

			if (glyphRun.IsLeftToRight)
			{
				foreach (var rect in rects)
				{
					characterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

					var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

					CornerstoneTest.AreEqual(rect.Left, distance);
				}
			}
			else
			{
				foreach (var rect in rects)
				{
					characterHit = glyphRun.GetPreviousCaretCharacterHit(characterHit);

					var distance = glyphRun.GetDistanceFromCharacterHit(characterHit);

					CornerstoneTest.AreEqual(rect.Right, distance);
				}
			}
		}
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/12676
	[PresentationTestMethod]
	public void SimilarRunsHaveSameInkBoundsAfterBlobCreation()
	{
		using (Start())
		{
			var typeface = new Typeface("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests#Inter");
			var options = new TextShaperOptions(typeface.GlyphTypeface, 14);
			var shapedBuffer = TextShaper.Current.ShapeText("F", options);

			var glyphRun1 = CreateGlyphRun(shapedBuffer);
			var bounds1 = glyphRun1.InkBounds;
			((GlyphRunImpl) glyphRun1.PlatformImpl.Item).GetTextBlob(new TextOptions { TextRenderingMode = TextRenderingMode.SubpixelAntialias }, default);

			var bounds2 = CreateGlyphRun(shapedBuffer).InkBounds;

			CornerstoneTest.AreEqual(bounds1, bounds2);
		}
	}

	private static List<Rect> BuildRects(GlyphRun glyphRun)
	{
		var height = glyphRun.Bounds.Height;

		var currentX = glyphRun.IsLeftToRight ? 0d : glyphRun.Bounds.Width;

		var rects = new List<Rect>(glyphRun.GlyphInfos!.Count);

		var lastCluster = -1;

		for (var index = 0; index < glyphRun.GlyphInfos.Count; index++)
		{
			var currentCluster = glyphRun.GlyphInfos[index].GlyphCluster;

			var advance = glyphRun.GlyphInfos[index].GlyphAdvance;

			if (lastCluster != currentCluster)
			{
				if (glyphRun.IsLeftToRight)
				{
					rects.Add(new Rect(currentX, 0, advance, height));
				}
				else
				{
					rects.Add(new Rect(currentX - advance, 0, advance, height));
				}
			}
			else
			{
				var rect = rects[index - 1];

				rects.Remove(rect);

				rect = glyphRun.IsLeftToRight ? rect.WithWidth(rect.Width + advance) : new Rect(rect.X - advance, 0, rect.Width + advance, height);

				rects.Add(rect);
			}

			if (glyphRun.IsLeftToRight)
			{
				currentX += advance;
			}
			else
			{
				currentX -= advance;
			}

			lastCluster = currentCluster;
		}

		return rects;
	}

	private static GlyphRun CreateGlyphRun(ShapedBuffer shapedBuffer)
	{
		return new GlyphRun(
			shapedBuffer.GlyphTypeface,
			shapedBuffer.FontRenderingEmSize,
			shapedBuffer.Text,
			shapedBuffer,
			biDiLevel: shapedBuffer.BidiLevel);
	}

	private static IDisposable Start()
	{
		var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
			.With(renderInterface: new PlatformRenderInterface(),
				fontManagerImpl: new CustomFontManagerImpl()));

		return disposable;
	}

	#endregion
}