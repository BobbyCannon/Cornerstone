#region References

using System;
using System.Text;
using Cornerstone.Presentation.Platform.Storage.FileIO;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Platform;

[TestClass]
public class StorageProviderHelperTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("YXZhLnYxLmJjbAAAAAAAAEM6Ly9maWxlLnR4dA==", "C://file.txt")]
	[DataRow("C://file.txt", "C://file.txt")]
	public void CanDecodeBclBookmarks(string bookmark, string expected)
	{
		var a = StorageBookmarkHelper.EncodeBclBookmark(expected);
		CornerstoneTest.IsTrue(StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var localPath));
		CornerstoneTest.AreEqual(expected, localPath);
	}

	[PresentationTestMethod]
	[DataRow("YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==", "C://file.txt")]
	public void CanDecodeBookmark(string encodedBookmark, string expectedNativeBookmark)
	{
		var platform = "test"u8;
		var expectedNativeBookmarkBytes = Encoding.UTF8.GetBytes(expectedNativeBookmark);

		CornerstoneTest.AreEqual(StorageBookmarkHelper.DecodeResult.Success, StorageBookmarkHelper.TryDecodeBookmark(platform, encodedBookmark, out var nativeBookmark));

		CornerstoneTest.AreEqual(expectedNativeBookmarkBytes, nativeBookmark);
	}

	[PresentationTestMethod]
	public void CanEncodeAndDecodeBookmark()
	{
		var platform = "test"u8;
		var nativeBookmark = "bookmark"u8;

		var bookmark = StorageBookmarkHelper.EncodeBookmark(platform, nativeBookmark);

		CornerstoneTest.IsNotNull(bookmark);

		CornerstoneTest.AreEqual(StorageBookmarkHelper.DecodeResult.Success, StorageBookmarkHelper.TryDecodeBookmark(platform, bookmark, out var nativeBookmarkRet));

		CornerstoneTest.IsNotNull(nativeBookmarkRet);

		CornerstoneTest.IsTrue(nativeBookmark.SequenceEqual(nativeBookmarkRet));
	}

	[PresentationTestMethod]
	[DataRow("C://file.txt", "YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==")]
	public void CanEncodeBookmark(string nativeBookmark, string expectedEncodedBookmark)
	{
		var platform = "test"u8;

		var bookmark = StorageBookmarkHelper.EncodeBookmark(platform, nativeBookmark);

		CornerstoneTest.AreEqual(expectedEncodedBookmark, bookmark);
		CornerstoneTest.IsNotNull(bookmark);
	}

	[PresentationTestMethod]
	[DataRow("YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==")] // "test" platform passed instead of "bcl"
	[DataRow("ZYXasHKJASd87124")]
	public void FailsToDecodeInvalidBclBookmarks(string bookmark)
	{
		CornerstoneTest.IsFalse(StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var localPath));
		CornerstoneTest.IsNull(localPath);
	}

	#endregion
}