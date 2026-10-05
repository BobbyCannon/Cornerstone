#region References

using System.Globalization;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class Bcp47ScriptResolverTests
{
	#region Methods

	[PresentationTestMethod]
	public void InvariantCultureReturnsNull()
	{
		CornerstoneTest.IsNull(Bcp47ScriptResolver.GetScriptSubtag(CultureInfo.InvariantCulture));
	}

	[PresentationTestMethod]
	public void NullCultureReturnsNull()
	{
		CornerstoneTest.IsNull(Bcp47ScriptResolver.GetScriptSubtag(null));
	}

	[PresentationTestMethod]
	[DataRow("ja", "Jpan")]
	[DataRow("ja-JP", "Jpan")]
	[DataRow("ko", "Kore")]
	[DataRow("ko-KR", "Kore")]
	[DataRow("zh", "Hans")]
	[DataRow("zh-CN", "Hans")]
	[DataRow("zh-SG", "Hans")]
	[DataRow("zh-TW", "Hant")]
	[DataRow("zh-HK", "Hant")]
	[DataRow("zh-MO", "Hant")]
	[DataRow("zh-Hans-CN", "Hans")]
	[DataRow("zh-Hant-TW", "Hant")]
	[DataRow("ru", "Cyrl")]
	[DataRow("ru-RU", "Cyrl")]
	[DataRow("en", "Latn")]
	[DataRow("en-US", "Latn")]
	[DataRow("de-DE", "Latn")]
	[DataRow("ar", "Arab")]
	[DataRow("he", "Hebr")]
	[DataRow("th", "Thai")]
	[DataRow("el", "Grek")]
	[DataRow("sr-Cyrl", "Cyrl")]
	[DataRow("sr-Latn-RS", "Latn")]
	public void ResolvesExpectedScriptSubtag(string cultureName, string expected)
	{
		var culture = CultureInfo.GetCultureInfo(cultureName);

		CornerstoneTest.AreEqual(expected, Bcp47ScriptResolver.GetScriptSubtag(culture));
	}

	[PresentationTestMethod]
	public void UnknownLanguageReturnsNull()
	{
		// 'xx' is reserved as a private-use language tag; no canonical script.
		var culture = CultureInfo.GetCultureInfo("xx");

		CornerstoneTest.IsNull(Bcp47ScriptResolver.GetScriptSubtag(culture));
	}

	#endregion
}