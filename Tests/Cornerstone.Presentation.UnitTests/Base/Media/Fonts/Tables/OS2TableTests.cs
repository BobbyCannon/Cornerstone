#region References

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.Fonts.Tables;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts.Tables;

[TestClass]
public class OS2TableTests
{
	#region Fields

	private static readonly string sInterFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void OS2TableInterRegularShouldBeRegular()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.IsTrue(os2Table.Selection.HasFlag(OS2Table.FontSelectionFlags.REGULAR));
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveConsistentAscentValues()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(2728, os2Table.TypoAscender);
		CornerstoneTest.AreEqual(2728, os2Table.WinAscent);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveConsistentDescentValues()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(-680, os2Table.TypoDescender);
		CornerstoneTest.AreEqual(680, os2Table.WinDescent);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidPanose()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		var panose = os2Table.Panose;
		CornerstoneTest.AreEqual(PanoseFamilyKind.LatinText, panose.FamilyKind);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidStrikeoutMetrics()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(192, os2Table.StrikeoutSize);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidTypoMetrics()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(2728, os2Table.TypoAscender);
		CornerstoneTest.AreEqual(-680, os2Table.TypoDescender);
		CornerstoneTest.IsTrue(os2Table.TypoAscender > os2Table.TypoDescender);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidWeightClass()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(400, os2Table.WeightClass);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidWidthClass()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(5, os2Table.WidthClass);
	}

	[PresentationTestMethod]
	public void OS2TableShouldHaveValidWinMetrics()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
		CornerstoneTest.AreEqual(2728, os2Table.WinAscent);
		CornerstoneTest.AreEqual(680, os2Table.WinDescent);
	}

	[PresentationTestMethod]
	public void ShouldLoadOS2TableFromInterFont()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var loaded = OS2Table.TryLoad(typeface, out var os2Table);

		CornerstoneTest.IsTrue(loaded);
	}

	#endregion

	#region Classes

	private class CustomPlatformTypeface : IPlatformTypeface
	{
		#region Fields

		private readonly UnmanagedFontMemory _fontMemory;

		#endregion

		#region Constructors

		public CustomPlatformTypeface(Stream stream, string fontFamily = "Custom")
		{
			_fontMemory = UnmanagedFontMemory.LoadFromStream(stream);
			FamilyName = fontFamily;
		}

		#endregion

		#region Properties

		public string FamilyName { get; }

		public FontSimulations FontSimulations => FontSimulations.None;

		public FontStretch Stretch => FontStretch.Normal;

		public FontStyle Style => FontStyle.Normal;

		public FontWeight Weight => FontWeight.Normal;

		#endregion

		#region Methods

		public void Dispose()
		{
			_fontMemory.Dispose();
		}

		public bool TryGetStream([NotNullWhen(true)] out Stream stream)
		{
			var memory = _fontMemory.Memory;

			var handle = memory.Pin();
			stream = new PinnedUnmanagedMemoryStream(handle, memory.Length);

			return true;
		}

		public bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table)
		{
			return _fontMemory.TryGetTable(tag, out table);
		}

		#endregion

		#region Classes

		private sealed class PinnedUnmanagedMemoryStream : UnmanagedMemoryStream
		{
			#region Fields

			private MemoryHandle _handle;

			#endregion

			#region Constructors

			public unsafe PinnedUnmanagedMemoryStream(MemoryHandle handle, long length)
				: base((byte*) handle.Pointer, length)
			{
				_handle = handle;
			}

			#endregion

			#region Methods

			protected override void Dispose(bool disposing)
			{
				try
				{
					base.Dispose(disposing);
				}
				finally
				{
					_handle.Dispose();
				}
			}

			#endregion
		}

		#endregion
	}

	#endregion
}