#region References

using System;
using Cornerstone.Presentation.Platform.Storage.FileIO;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class UriExtensionsTests
{
	#region Methods

	[PresentationTestMethod]
	public void AssemblyNameFromEmptyQueryNotParsed()
	{
		var uri = new Uri("resm:Cornerstone.Presentation.Theme.Accents.BaseLight.xaml");
		var name = uri.GetAssemblyNameFromQuery();

		CornerstoneTest.AreEqual(string.Empty, name);
	}

	[PresentationTestMethod]
	public void AssemblyNameFromQueryParsed()
	{
		const string key = "assembly";
		const string value = "Cornerstone.Presentation.Theme";

		var uri = new Uri($"resm:Cornerstone.Presentation.Theme.Accents.BaseLight.xaml?{key}={value}");
		var name = uri.GetAssemblyNameFromQuery();

		CornerstoneTest.AreEqual(value, name);
	}

	[PresentationTestMethod]
	[DataRow("/home/Projects.txt")]
	[DataRow("/home/Stahování/Požární kniha 2.txt")]
	[DataRow("C:\\%51.txt")]
	[DataRow("/home/asd#xcv.txt")]
	[DataRow("C:\\\\Work\\Projects.txt")]
	public void ShouldConvertFilePathToUriAndBack(string path)
	{
		var uri = StorageProviderHelpers.UriFromFilePath(path, false);

		CornerstoneTest.AreEqual(path, uri.LocalPath);
	}

	[PresentationTestMethod]
	[DataRow(@"\\?\D:\abcdefgh\abcdefgh\abcdefabcdefgh\abcdefghabcdefghabcdefgha\bcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh\abcdefghabcdefghabcdefgha\bcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh",
		@"D:\abcdefgh\abcdefgh\abcdefabcdefgh\abcdefghabcdefghabcdefgha\bcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh\abcdefghabcdefghabcdefgha\bcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh")]
	public void ShouldConvertLongFilePathToUriAndBack(string prepath, string path)
	{
		var uri = StorageProviderHelpers.UriFromFilePath(prepath, false);

		CornerstoneTest.AreEqual(path, uri.LocalPath);
	}

	#endregion
}