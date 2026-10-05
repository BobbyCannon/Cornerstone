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
public class HeadTableTests
{
	#region Fields

	private static readonly string sInterFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidBoundingBox()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(-2080, headTable.XMin);
		CornerstoneTest.AreEqual(7274, headTable.XMax);
		CornerstoneTest.AreEqual(-900, headTable.YMin);
		CornerstoneTest.AreEqual(3072, headTable.YMax);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidCreatedTimestamp()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.IsTrue(headTable.Created > new DateTime(1904, 1, 1));
		CornerstoneTest.IsTrue(headTable.Created < DateTime.UtcNow);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidFlags()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));

		CornerstoneTest.IsTrue(headTable.Flags.HasFlag(HeadFlags.BaselineAtY0));
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidFontDirectionHint()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(FontDirectionHint.LeftToRightWithNeutrals, headTable.FontDirectionHint);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidFontRevision()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.IsTrue(headTable.FontRevision.ToFloat() > 0);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidGlyphDataFormat()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(GlyphDataFormat.Current, headTable.GlyphDataFormat);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidIndexToLocFormat()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(IndexToLocFormat.Long, headTable.IndexToLocFormat);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidLowestRecPPEM()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(6, headTable.LowestRecPPEM);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidMagicNumber()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(0x5F0F3CF5u, headTable.MagicNumber);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidModifiedTimestamp()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.IsTrue(headTable.Modified > new DateTime(1904, 1, 1));
		CornerstoneTest.IsTrue(headTable.Modified < DateTime.UtcNow);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidUnitsPerEm()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual(2816, headTable.UnitsPerEm);
	}

	[PresentationTestMethod]
	public void HeadTableShouldHaveValidVersion()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		CornerstoneTest.IsTrue(HeadTable.TryLoad(typeface, out var headTable));
		CornerstoneTest.AreEqual((ushort) 1, headTable.Version.Major);
		CornerstoneTest.AreEqual((ushort) 0, headTable.Version.Minor);
	}

	[PresentationTestMethod]
	public void ShouldLoadHeadTableFromInterFont()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var success = HeadTable.TryLoad(typeface, out var headTable);

		CornerstoneTest.IsTrue(success);
		CornerstoneTest.IsNotNull(headTable);
	}

	#endregion
}

internal class CustomPlatformTypeface : IPlatformTypeface
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