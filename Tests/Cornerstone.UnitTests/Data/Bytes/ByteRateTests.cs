#region References

using System;
using Cornerstone.Data.Bytes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data.Bytes;

[TestClass]
public class ByteRateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Humanize()
	{
		AreEqual("7 MB/s", new ByteRate(ByteSize.FromMegabytes(7), TimeSpan.FromSeconds(1)).Humanize());
		AreEqual("58.72 Mb/s", new ByteRate(ByteSize.FromMegabytes(7), TimeSpan.FromSeconds(1)).Humanize("Mb"));
		AreEqual("3.5 MB/s", new ByteRate(ByteSize.FromMegabytes(7), TimeSpan.FromSeconds(2)).Humanize());
		AreEqual("29.36 Mb/s", new ByteRate(ByteSize.FromMegabytes(7), TimeSpan.FromSeconds(2)).Humanize("Mb"));
		AreEqual("50 MB/s", new ByteRate(ByteSize.FromMegabytes(100), TimeSpan.FromSeconds(2)).Humanize());
		AreEqual("122.07 KB/s", new ByteRate(ByteSize.FromKilobits(1000), TimeSpan.FromSeconds(1)).Humanize());
	}

	#endregion
}