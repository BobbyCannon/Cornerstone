#region References

using Cornerstone.Protocols.Nmea;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Nmea;

[TestClass]
public class LocationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EmptyInput()
	{
		var l = new NmeaLocation("", "");
		var d = l.ToDecimal();
		AreEqual(0, d);

		var e = NmeaLocation.FromLatitude(d);
		AreEqual(new NmeaLocation("00000.00000", "N"), e);
		AreEqual(0, new NmeaLocation("00000.00000", "N").ToDecimal());
		AreEqual(0, new NmeaLocation("00000.00000", "E").ToDecimal());
		AreEqual(0, new NmeaLocation("00000.00000", "S").ToDecimal());
		AreEqual(0, new NmeaLocation("00000.00000", "W").ToDecimal());
	}

	[TestMethod]
	public void TestConversion()
	{
		//
		// Latitude: 90, -90   Longitude: 180, -180
		//
		var scenarios = new (string Degree, string Indicator, decimal Decimal, bool longitude)[]
		{
			("00000.00000", "N", 0m, false),
			("09000.00000", "N", 90m, false),
			("09000.00000", "S", -90m, false),
			("00000.00000", "E", 0m, true),
			("18000.00000", "E", 180m, true),
			("18000.00000", "W", -180m, true),
			("03345.57624", "N", 33.759604m, false),
			("08423.95482", "W", -84.399247m, true),
			("03246.26336", "N", 32.771056m, false),
			("07955.81770", "W", -79.930295m, true),
			("04026.88030", "N", 40.448005m, false),
			("01636.01722", "E", 16.600287m, true),
			("05449.90878", "S", -54.831813m, false),
			("06650.22462", "W",  -66.837077m, true),
			("02436.77459", "E", 24.612909833333333333333333333m, true),
			("02436.77459", "W", -24.612909833333333333333333333m, true),
			("04036.82924", "N", 40.613820666666666666666666667m, false),
			("04036.82924", "S", -40.613820666666666666666666667m, false),
		};

		foreach (var scenario in scenarios)
		{
			var e = scenario.longitude 
				? NmeaLocation.FromLongitude(scenario.Decimal)
				: NmeaLocation.FromLatitude(scenario.Decimal);
			AreEqual(new NmeaLocation(scenario.Degree, scenario.Indicator), e);

			var l = new NmeaLocation(scenario.Degree, scenario.Indicator);
			var d = l.ToDecimal();
			AreEqual(scenario.Decimal, d);
		}
	}

	#endregion
}