#region References

using Cornerstone.Protocols.Nmea.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Nmea;

[TestClass]
public class NmeaMessageTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void TestMethodExtractChecksum()
	{
		var n = new RmcMessage();
		var m = "$GNRMC,143718.00,A,4513.13793,N,01859.19704,E,0.050,,290719,,,A*65";
		var c = n.ExtractChecksum(m);
		AreEqual("65", c);
	}

	[TestMethod]
	public void TestMethodExtractChecksumInvalidSize()
	{
		var n = new RmcMessage();
		AreEqual(string.Empty, n.ExtractChecksum(null));
		AreEqual(string.Empty, n.ExtractChecksum(""));
		AreEqual(string.Empty, n.ExtractChecksum(" "));
		AreEqual(string.Empty, n.ExtractChecksum("  "));
		AreEqual(string.Empty, n.ExtractChecksum("   "));
		AreEqual(string.Empty, n.ExtractChecksum("$*"));
		AreEqual(string.Empty, n.ExtractChecksum("$GNRMC,*"));
	}

	[TestMethod]
	public void TestMethodExtractChecksumNoStar()
	{
		var n = new RmcMessage();
		var m = "$GNRMC,143718.00,A,4513.13793,N,01859.19704,E,0.050,,290719,,,A";
		var c = n.ExtractChecksum(m);
		AreEqual(string.Empty, c);
	}

	[TestMethod]
	public void TestMethodParseChecksum()
	{
		var n = new RmcMessage();
		AreEqual("00", n.ParseChecksum(""));
		AreEqual("00", n.ParseChecksum("X"));
		AreEqual("00", n.ParseChecksum("$"));
		AreEqual("58", n.ParseChecksum("$X"));
		AreEqual("58", n.ParseChecksum("$X*"));
		AreEqual("59", n.ParseChecksum("$Y"));
		AreEqual("59", n.ParseChecksum("$Y*"));
		AreEqual("65", n.ParseChecksum("$GNRMC,143718.00,A,4513.13793,N,01859.19704,E,0.050,,290719,,,A"));
		AreEqual("65", n.ParseChecksum("$GNRMC,143718.00,A,4513.13793,N,01859.19704,E,0.050,,290719,,,A*65"));
	}

	#endregion
}