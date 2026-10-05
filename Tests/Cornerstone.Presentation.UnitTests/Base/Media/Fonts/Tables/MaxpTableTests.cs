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
public class MaxpTableTests
{
	#region Fields

	private static readonly string sInterFontUri = "resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts.Inter-Regular.ttf?assembly=Cornerstone.Presentation.UnitTests";

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void MaxpTableNumGlyphsShouldMatchGlyphTypefaceGlyphCount()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(maxpTable.NumGlyphs, typeface.GlyphCount);
	}

	[PresentationTestMethod]
	public void MaxpTableShouldHaveValidMaxComponentDepth()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(1, maxpTable.MaxComponentDepth);
	}

	[PresentationTestMethod]
	public void MaxpTableShouldHaveValidMaxCompositeContours()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(7, maxpTable.MaxCompositeContours);
	}

	[PresentationTestMethod]
	public void MaxpTableShouldHaveValidMaxCompositePoints()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(112, maxpTable.MaxCompositePoints);
	}

	[PresentationTestMethod]
	public void MaxpTableShouldHaveValidMaxStackElements()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(0, maxpTable.MaxStackElements);
	}

	[PresentationTestMethod]
	public void MaxpTableShouldHaveValidNumGlyphs()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(2547, maxpTable.NumGlyphs);
	}

	[PresentationTestMethod]
	public void MaxpTableTrueTypeShouldHaveVersion10()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(1, maxpTable.Version.Major);
		CornerstoneTest.AreEqual(0, maxpTable.Version.Minor);
	}

	[PresentationTestMethod]
	public void MaxpTableVersion10ShouldHaveValidMaxContours()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(12, maxpTable.MaxContours);
	}

	[PresentationTestMethod]
	public void MaxpTableVersion10ShouldHaveValidMaxPoints()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(148, maxpTable.MaxPoints);
	}

	[PresentationTestMethod]
	public void MaxpTableVersion10ShouldHaveValidMaxZones()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreEqual(1, maxpTable.MaxZones);
	}

	[PresentationTestMethod]
	public void ShouldLoadMaxpTableFromInterFont()
	{
		var assetLoader = new StandardAssetLoader();

		using var stream = assetLoader.Open(new Uri(sInterFontUri));

		var typeface = new GlyphTypeface(new CustomPlatformTypeface(stream));

		var maxpTable = MaxpTable.Load(typeface);

		CornerstoneTest.AreNotEqual(default, maxpTable);
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