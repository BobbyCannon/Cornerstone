﻿#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Location;
using Cornerstone.Parsers.Csv;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Csv;

[TestClass]
public class CsvWriterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldWrite()
	{
		var location = new VerticalLocation
		{
			Accuracy = 1.2,
			AccuracyReference = AccuracyReferenceType.Meters,
			Altitude = 123.45,
			AltitudeReference = AltitudeReferenceType.Ellipsoid,
			Flags = LocationFlags.All,
			ProviderName = "Provider 1",
			SourceName = "The Source",
			StatusTime = new DateTime(2022, 11, 21, 12, 16, 15, DateTimeKind.Utc)
		};

		var history = new SpeedyList<VerticalLocation>();
		location.Flags = LocationFlags.HasLocation | LocationFlags.HasSpeed;
		location.StatusTime += TimeSpan.FromSeconds(1);
		history.Add((VerticalLocation) location.ShallowClone());

		location.Flags = LocationFlags.All;
		location.StatusTime += TimeSpan.FromSeconds(1);
		history.Add((VerticalLocation) location.ShallowClone());

		var actual = new StringWriter();
		var option = new CsvConverterSettings();
		CsvWriter.Write(actual, option, history.ToArray());
		actual.Dump();

		var expected = @"Accuracy,AccuracyReference,Altitude,AltitudeReference,Flags,HasAccuracy,HasHeading,HasSpeed,HasValue,Heading,InformationId,ProviderName,SourceName,Speed,StatusTime
1.2,1,123.45,2,6,True,False,True,True,0,2ab3b4a0-a387-409a-bba3-b74f75972463,Provider 1,The Source,0,2022-11-21T12:16:16.0000000Z
1.2,1,123.45,2,7,True,True,True,True,0,2ab3b4a0-a387-409a-bba3-b74f75972463,Provider 1,The Source,0,2022-11-21T12:16:17.0000000Z
";
		AreEqual(expected, actual.ToString());
	}

	#endregion
}