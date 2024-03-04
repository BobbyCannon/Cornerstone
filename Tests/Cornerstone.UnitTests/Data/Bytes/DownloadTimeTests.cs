#region References

using Cornerstone.Data.Bytes;
using Cornerstone.Data.Times;
using Cornerstone.Text;
using Cornerstone.Text.Human;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data.Bytes;

[TestClass]
public class DownloadTimeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Humanize()
	{
		AreEqual("1 Second", new DownloadTime(ByteRate.Cellular2G, ByteRate.Cellular2G).Humanize());
		AreEqual("1 s", new DownloadTime(ByteRate.Cellular2G, ByteRate.Cellular2G)
			.Humanize(new HumanizeOptions { WordFormat = WordFormat.Abbreviation })
		);
		AreEqual("2.796 Minutes", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular2G)
			.Humanize(new HumanizeOptions { MinUnit = TimeUnit.Minute })
		);
		AreEqual("2.796 m", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular2G)
			.Humanize(new HumanizeOptions { MinUnit = TimeUnit.Minute, WordFormat = WordFormat.Abbreviation })
		);
		AreEqual("167.772 s", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular2G)
			.Humanize(new HumanizeOptions { MaxUnit = TimeUnit.Second, MinUnit = TimeUnit.Second, WordFormat = WordFormat.Abbreviation })
		);
		AreEqual("200 Milliseconds", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular3G).Humanize());
		AreEqual("200 ms", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular3G)
			.Humanize(new HumanizeOptions { WordFormat = WordFormat.Abbreviation })
		);
		AreEqual("0.2 s", new DownloadTime(ByteSize.FromMegabytes(1), ByteRate.Cellular3G)
			.Humanize(new HumanizeOptions { MinUnit = TimeUnit.Second, WordFormat = WordFormat.Abbreviation })
		);
	}

	[TestMethod]
	public void ToDetailedMessage()
	{
		var time = new DownloadTime(ByteSize.FromMegabytes(7.5), ByteSize.FromMegabits(3.55));
		var actual = time.ToDetailedMessage();
		var expected = "To Download: 7.5 MB\r\nRate: 7.5 MB\r\nEstimate: 17.722 Seconds";

		AreEqual(expected, actual);
	}

	#endregion
}