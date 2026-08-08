#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Location;

[SourceReflection]
[Notifiable(["*"])]
public partial class HorizontalLocation : IHorizontalLocation
{
	#region Properties

	public partial double Accuracy { get; set; }
	public partial AccuracyReferenceType AccuracyReference { get; set; }
	public partial bool HasHeading { get; set; }
	public partial bool HasSpeed { get; set; }
	public partial bool HasValue { get; set; }
	public partial double Heading { get; set; }
	public partial double Latitude { get; set; }
	public partial double Longitude { get; set; }
	public partial string SourceName { get; set; }
	public partial double Speed { get; set; }
	public partial DateTime StatusTime { get; set; }

	#endregion
}

/// <summary>
/// Represents a horizontal location (lat, long).
/// </summary>
public interface IHorizontalLocation
	: IMinimalHorizontalLocation
{
	#region Properties

	public double Accuracy { get; set; }
	public AccuracyReferenceType AccuracyReference { get; set; }
	public bool HasHeading { get; set; }
	public bool HasSpeed { get; set; }
	public bool HasValue { get; set; }
	public double Heading { get; set; }
	public string SourceName { get; set; }
	public double Speed { get; set; }
	public DateTime StatusTime { get; set; }

	#endregion
}

/// <summary>
/// Represents a horizontal location (lat, long).
/// </summary>
public interface IMinimalHorizontalLocation
{
	#region Properties

	/// <summary>
	/// Ranges between -90 to 90 from North to South
	/// </summary>
	double Latitude { get; set; }

	/// <summary>
	/// Ranges between -180 to 180 from West to East
	/// </summary>
	double Longitude { get; set; }

	#endregion
}