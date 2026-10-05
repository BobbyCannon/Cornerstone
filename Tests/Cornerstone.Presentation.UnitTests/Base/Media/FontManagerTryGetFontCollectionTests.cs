#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class FontManagerTryGetFontCollectionTests
{
	#region Methods

	[PresentationTestMethod]
	public void TryGetFontCollectionAbsoluteResmReturnsSameInstanceOnSubsequentCalls()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests", UriKind.Absolute);
			var fm = FontManager.Current;

			fm.TryGetFontCollection(source, out var first);
			fm.TryGetFontCollection(source, out var second);

			CornerstoneTest.Same(first, second);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionAbsoluteResmReturnsTrue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests", UriKind.Absolute);

			CornerstoneTest.IsTrue(FontManager.Current.TryGetFontCollection(source, out var collection));
			CornerstoneTest.IsNotNull(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionAbsoluteResmYieldsEmbeddedFontCollection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("resm:Cornerstone.Presentation.UnitTests.Skia.RenderTestFonts?assembly=Cornerstone.Presentation.UnitTests", UriKind.Absolute);

			FontManager.Current.TryGetFontCollection(source, out var collection);

			CornerstoneTest.IsType<EmbeddedFontCollection>(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionCsresReturnsSameInstanceOnSubsequentCalls()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("csres://Cornerstone.Presentation.UnitTests/Skia/RenderTestFonts", UriKind.Absolute);
			var fm = FontManager.Current;

			fm.TryGetFontCollection(source, out var first);
			fm.TryGetFontCollection(source, out var second);

			CornerstoneTest.Same(first, second);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionCsresReturnsTrue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("csres://Cornerstone.Presentation.UnitTests/Skia/RenderTestFonts", UriKind.Absolute);

			CornerstoneTest.IsTrue(FontManager.Current.TryGetFontCollection(source, out var collection));
			CornerstoneTest.IsNotNull(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionCsresYieldsEmbeddedFontCollection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("csres://Cornerstone.Presentation.UnitTests/Skia/RenderTestFonts", UriKind.Absolute);

			FontManager.Current.TryGetFontCollection(source, out var collection);

			CornerstoneTest.IsType<EmbeddedFontCollection>(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionRegisteredFontsCollectionReturnsTrue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var key = new Uri("fonts:MyTest", UriKind.Absolute);
			var stub = new StubFontCollection(key);
			var fm = FontManager.Current;
			fm.AddFontCollection(stub);

			CornerstoneTest.IsTrue(fm.TryGetFontCollection(key, out var collection));
			CornerstoneTest.Same(stub, collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontSchemeAndSystemFontsKeyReturnSameInstance()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var fm = FontManager.Current;
			var schemeSource = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

			fm.TryGetFontCollection(schemeSource, out var fromScheme);
			fm.TryGetFontCollection(FontManager.SystemFontsKey, out var fromKey);

			CornerstoneTest.Same(fromScheme, fromKey);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontSchemeReturnsSameInstanceOnSubsequentCalls()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);
			var fm = FontManager.Current;

			fm.TryGetFontCollection(source, out var first);
			fm.TryGetFontCollection(source, out var second);

			CornerstoneTest.Same(first, second);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontSchemeReturnsTrue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

			CornerstoneTest.IsTrue(FontManager.Current.TryGetFontCollection(source, out var collection));
			CornerstoneTest.IsNotNull(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontSchemeYieldsSystemFontCollection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri($"{FontManager.SystemFontScheme}:Arial", UriKind.Absolute);

			FontManager.Current.TryGetFontCollection(source, out var collection);

			CornerstoneTest.IsType<SystemFontCollection>(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontsKeyReturnsTrue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			CornerstoneTest.IsTrue(FontManager.Current.TryGetFontCollection(FontManager.SystemFontsKey, out var collection));
			CornerstoneTest.IsNotNull(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionSystemFontsKeyYieldsSystemFontCollection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			FontManager.Current.TryGetFontCollection(FontManager.SystemFontsKey, out var collection);

			CornerstoneTest.IsType<SystemFontCollection>(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionUnknownSchemeDoesNotCacheNull()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			// Verify that repeated lookups for the same unknown-scheme URI
			// consistently return false/null rather than succeeding due to an
			// accidentally cached null or invalid entry.
			var source = new Uri("file:///some/path/fonts", UriKind.Absolute);
			var fm = FontManager.Current;

			CornerstoneTest.IsFalse(fm.TryGetFontCollection(source, out var first));
			CornerstoneTest.IsNull(first);

			CornerstoneTest.IsFalse(fm.TryGetFontCollection(source, out var second));
			CornerstoneTest.IsNull(second);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionUnknownSchemeReturnsFalse()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var source = new Uri("https://example.com/fonts", UriKind.Absolute);

			CornerstoneTest.IsFalse(FontManager.Current.TryGetFontCollection(source, out var collection));
			CornerstoneTest.IsNull(collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionUnregisteredFontsCollectionDoesNotCacheNull()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var key = new Uri("fonts:DoesNotExist2", UriKind.Absolute);
			var fm = FontManager.Current;

			// First call returns false
			CornerstoneTest.IsFalse(fm.TryGetFontCollection(key, out _));

			// Register after the first failed lookup
			var stub = new StubFontCollection(key);
			fm.AddFontCollection(stub);

			// Now it should be found if null had been cached this would still fail
			CornerstoneTest.IsTrue(fm.TryGetFontCollection(key, out var collection));
			CornerstoneTest.Same(stub, collection);
		}
	}

	[PresentationTestMethod]
	public void TryGetFontCollectionUnregisteredFontsCollectionReturnsFalse()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var key = new Uri("fonts:DoesNotExist", UriKind.Absolute);

			CornerstoneTest.IsFalse(FontManager.Current.TryGetFontCollection(key, out var collection));
			CornerstoneTest.IsNull(collection);
		}
	}

	#endregion

	#region Classes

	private sealed class StubFontCollection : IFontCollection
	{
		#region Constructors

		public StubFontCollection(Uri key)
		{
			Key = key;
		}

		#endregion

		#region Properties

		public int Count => 0;
		public FontFamily this[int index] => throw new NotSupportedException();

		public Uri Key { get; }

		#endregion

		#region Methods

		public void Dispose()
		{
		}

		public IEnumerator<FontFamily> GetEnumerator()
		{
			return Enumerable.Empty<FontFamily>().GetEnumerator();
		}

		public bool TryCreateSyntheticGlyphTypeface(GlyphTypeface glyphTypeface, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface syntheticGlyphTypeface)
		{
			syntheticGlyphTypeface = null;
			return false;
		}

		public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
		{
			familyTypefaces = null;
			return false;
		}

		public bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface glyphTypeface)
		{
			glyphTypeface = null;
			return false;
		}

		public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface glyphTypeface)
		{
			glyphTypeface = null;
			return false;
		}

		public bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string familyName, CultureInfo culture, out Typeface typeface)
		{
			typeface = default;
			return false;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	#endregion
}