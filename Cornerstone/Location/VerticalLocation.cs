#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Location;

[SourceReflection]
[Notifiable(["*"])]
public partial class VerticalLocation : IVerticalLocation
{
	#region Properties

	public partial double Accuracy { get; set; }
	public partial AccuracyReferenceType AccuracyReference { get; set; }
	public partial double Altitude { get; set; }
	public partial AltitudeReferenceType AltitudeReference { get; set; }
	public partial bool HasValue { get; set; }
	public partial string SourceName { get; set; }
	public partial DateTime StatusTime { get; set; }

	#endregion
}

/// <summary>
/// Represents a vertical location (alt, alt ref, acc, acc ref).
/// </summary>
public interface IVerticalLocation
	: IMinimalVerticalLocation
{
	#region Properties

	public double Accuracy { get; set; }
	public AccuracyReferenceType AccuracyReference { get; set; }
	public bool HasValue { get; set; }

	#endregion
}

/// <summary>
/// Represents a vertical location (alt, alt ref).
/// </summary>
public interface IMinimalVerticalLocation
{
	#region Properties

	/// <summary>
	/// The altitude of the location
	/// </summary>
	double Altitude { get; set; }

	/// <summary>
	/// The reference type for the altitude value.
	/// </summary>
	AltitudeReferenceType AltitudeReference { get; set; }

	#endregion
}