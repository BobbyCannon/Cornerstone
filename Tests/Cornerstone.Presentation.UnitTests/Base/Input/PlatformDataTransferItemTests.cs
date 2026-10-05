#region References

using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public sealed class PlatformDataTransferItemTests
{
	#region Methods

	[PresentationTestMethod]
	public async Task TryGetRawAsyncShouldReturnExpectedValueWhenFormatIsKnown()
	{
		var format = DataFormat.CreateBytesApplicationFormat("test-format");
		var item = new TestPlatformDataTransferItem([format]);

		var value = await item.TryGetRawAsync(format);

		CornerstoneTest.Same(format, value);
	}

	[PresentationTestMethod]
	public async Task TryGetRawAsyncShouldReturnNullWhenFormatIsUnknown()
	{
		var format = DataFormat.CreateBytesApplicationFormat("test-format");
		var item = new TestPlatformDataTransferItem([]);

		var value = await item.TryGetRawAsync(format);

		CornerstoneTest.IsNull(value);
	}

	[PresentationTestMethod]
	public void TryGetRawShouldReturnExpectedValueWhenFormatIsKnown()
	{
		var format = DataFormat.CreateBytesApplicationFormat("test-format");
		var item = new TestPlatformDataTransferItem([format]);

		var value = item.TryGetRaw(format);

		CornerstoneTest.Same(format, value);
	}

	[PresentationTestMethod]
	public void TryGetRawShouldReturnNullWhenFormatIsUnknown()
	{
		var format = DataFormat.CreateBytesApplicationFormat("test-format");
		var item = new TestPlatformDataTransferItem([]);

		var value = item.TryGetRaw(format);

		CornerstoneTest.IsNull(value);
	}

	#endregion

	#region Classes

	private sealed class TestPlatformDataTransferItem(DataFormat[] dataFormats) : PlatformDataTransferItem
	{
		#region Methods

		protected override DataFormat[] ProvideFormats()
		{
			return dataFormats;
		}

		protected override object TryGetRawCore(DataFormat format)
		{
			return format;
		}

		#endregion
	}

	#endregion
}