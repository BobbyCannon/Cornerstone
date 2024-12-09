using System;

#pragma warning disable 1591

namespace Cornerstone.Protocols.Nmea;

/// <summary>
/// Position - location
/// </summary>
public class NmeaLocation
{
	#region Constructors

	public NmeaLocation(string degree, string indicator)
	{
		Degree = degree;
		Indicator = indicator;
	}

	#endregion

	#region Properties

	public string Degree { get; }

	public string Indicator { get; }

	#endregion

	#region Methods

	/// <summary>
	/// </summary>
	/// <returns> The NMEA location. </returns>
	public static NmeaLocation FromLatitude(decimal value)
	{
		return Parse(value, latitude: true);
	}
	
	/// <summary>
	/// </summary>
	/// <returns> The NMEA location. </returns>
	public static NmeaLocation FromLongitude(decimal value)
	{
		return Parse(value, latitude: false);
	}

	private static NmeaLocation Parse(decimal value, bool latitude)
	{
		var isNegative = value < 0;
		value = Math.Abs(value);
		var degrees = (int)value;
		value = (value - degrees) * 60;
		var minutes = (int)value;
		value = Math.Round((value - minutes) * 100000.0m, MidpointRounding.AwayFromZero);
		var seconds = (int)value;

		// NmeaLocation("4036.82924", "S")
		// -40.61382
		// -1 for W and S
		return new NmeaLocation(
			$"{degrees:D3}{minutes:D2}.{seconds:D5}",
			isNegative
				? (latitude ? "S" : "W")
				: (latitude ? "N" : "E")
		);
	}

	/// <summary>
	/// XXYY.YYYY = XX + (YYYYYY / 600000) graden.
	/// (d)dd + (mm.mmmm/60) (* -1 for W and S)
	/// </summary>
	/// <returns> </returns>
	public decimal ToDecimal()
	{
		if (string.IsNullOrEmpty(Degree) || string.IsNullOrEmpty(Indicator))
		{
			return 0;
		}

		// ddmm.mmmm
		var ddmm = Degree.Split('.');
		var dd = ddmm[0].Substring(0, ddmm[0].Length - 2);
		var mm = ddmm[0].Substring(ddmm[0].Length - 2);
		var mmmm = ddmm[1];
		var minute = mm + "." + mmmm;

		// indicators
		var nesw = Indicator;
		var plusMinus = (nesw == "S") || (nesw == "W") ? -1 : 1;
		var value = (System.Convert.ToDecimal(dd) + (System.Convert.ToDecimal(minute) / 60.0m)) * plusMinus;
		return value;
	}

	public override string ToString()
	{
		return ToDecimal().ToString();
	}

	#endregion
}