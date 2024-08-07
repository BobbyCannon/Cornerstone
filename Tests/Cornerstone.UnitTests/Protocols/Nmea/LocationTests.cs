﻿#region References

using Cornerstone.Protocols.Nmea;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Nmea;

[TestClass]
public class LocationTests : CornerstoneUnitTest
{
	#region Methods

	/// <summary>
	/// Longitude = "02436.77459E"
	/// </summary>
	[TestMethod]
	public void TestMethodLocationEast()
	{
		var l = new NmeaLocation("02436.77459", "E");
		var d = l.ToDecimal();
		AreEqual("24.61291", d.ToString("0.00000"));
	}

	[TestMethod]
	public void TestMethodLocationNone()
	{
		var l = new NmeaLocation("", "");
		var d = l.ToDecimal();
		AreEqual(-1, d);
	}

	/// <summary>
	/// Latitude = "4036.82924N"
	/// </summary>
	[TestMethod]
	public void TestMethodLocationNorth()
	{
		var l = new NmeaLocation("4036.82924", "N");
		var d = l.ToDecimal();
		AreEqual("40.613821", d.ToString("0.000000"));
	}

	[TestMethod]
	public void TestMethodLocationSouth()
	{
		var l = new NmeaLocation("4036.82924", "S");
		var d = l.ToDecimal();
		AreEqual("-40.61382", d.ToString("0.00000"));
	}

	[TestMethod]
	public void TestMethodLocationWest()
	{
		var l = new NmeaLocation("02436.77459", "W");
		var d = l.ToDecimal();
		AreEqual("-24.61291", d.ToString("0.00000"));
	}

	#endregion
}