#region References

using System;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public sealed class DataFormatTests
{
	#region Methods

	[PresentationTestMethod]
	public void CreateInProcessFormatAllowsNonASCIIIdentifiers()
	{
		var format = DataFormat.CreateInProcessFormat<string>("日本語フォーマット");

		CornerstoneTest.AreEqual("日本語フォーマット", format.Identifier);
	}

	[PresentationTestMethod]
	public void CreateInProcessFormatReturnsFormatWithCorrectIdentifier()
	{
		var format = DataFormat.CreateInProcessFormat<string>("my-format");

		CornerstoneTest.AreEqual("my-format", format.Identifier);
	}

	[PresentationTestMethod]
	public void CreateInProcessFormatReturnsFormatWithInProcessKind()
	{
		var format = DataFormat.CreateInProcessFormat<string>("my-format");

		CornerstoneTest.AreEqual(DataFormatKind.InProcess, format.Kind);
	}

	[PresentationTestMethod]
	public void CreateInProcessFormatThrowsOnEmptyIdentifier()
	{
		Assert.Throws<ArgumentException>(() => DataFormat.CreateInProcessFormat<string>(string.Empty));
	}

	[PresentationTestMethod]
	public void CreateInProcessFormatThrowsOnNullIdentifier()
	{
		Assert.Throws<ArgumentNullException>(() => DataFormat.CreateInProcessFormat<string>(null!));
	}

	[PresentationTestMethod]
	public void InProcessFormatCoexistsWithOtherFormatsInDataTransfer()
	{
		var inProcessFormat = DataFormat.CreateInProcessFormat<string>("my-inprocess");
		var item = new DataTransferItem();
		item.SetText("plain text");
		item.Set(inProcessFormat, "in-process data");

		var dataTransfer = new DataTransfer();
		dataTransfer.Add(item);

		CornerstoneTest.Contains(dataTransfer.Formats, DataFormat.Text);
		CornerstoneTest.Contains(dataTransfer.Formats, inProcessFormat);
		CornerstoneTest.AreEqual("plain text", item.TryGetValue(DataFormat.Text));
		CornerstoneTest.AreEqual("in-process data", item.TryGetValue(inProcessFormat));
	}

	[PresentationTestMethod]
	public void InProcessFormatEqualitySameIdentifier()
	{
		var format1 = DataFormat.CreateInProcessFormat<string>("my-format");
		var format2 = DataFormat.CreateInProcessFormat<string>("my-format");

		CornerstoneTest.AreEqual(format1, format2);
		CornerstoneTest.IsTrue(format1 == format2);
	}

	[PresentationTestMethod]
	public void InProcessFormatInequalityDifferentIdentifier()
	{
		var format1 = DataFormat.CreateInProcessFormat<string>("format-a");
		var format2 = DataFormat.CreateInProcessFormat<string>("format-b");

		CornerstoneTest.AreNotEqual(format1, format2);
		CornerstoneTest.IsTrue(format1 != format2);
	}

	[PresentationTestMethod]
	public void InProcessFormatInequalityDifferentKindSameIdentifier()
	{
		var inProcess = DataFormat.CreateInProcessFormat<string>("test-format");
		var application = DataFormat.CreateStringApplicationFormat("test-format");

		CornerstoneTest.AreNotEqual<DataFormat>(inProcess, application);
	}

	[PresentationTestMethod]
	public void InProcessFormatWorksWithDataTransferItemSetAndGet()
	{
		var format = DataFormat.CreateInProcessFormat<string>("my-inprocess");
		var item = new DataTransferItem();
		item.Set(format, "hello");

		var value = item.TryGetValue(format);

		CornerstoneTest.AreEqual("hello", value);
	}

	[PresentationTestMethod]
	public void ToSystemNameThrowsForInProcess()
	{
		var format = DataFormat.CreateInProcessFormat<string>("test");

		Assert.Throws<InvalidOperationException>(() => format.ToSystemName("prefix."));
	}

	[PresentationTestMethod]
	public void TryGetRawWithMismatchedFormatReturnsNullForSingleFormatItem()
	{
		var item = DataTransferItem.CreateText("hello");

		var result = item.TryGetRaw(DataFormat.Bitmap);

		CornerstoneTest.IsNull(result);
	}

	#endregion
}