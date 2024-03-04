#region References

using System;

#endregion

namespace Cornerstone.Attributes;

/// <summary>
/// Represents a header value for a CSV.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CsvHeaderAttribute : CornerstoneAttribute
{
	#region Constructors

	/// <inheritdoc />
	public CsvHeaderAttribute(string name)
	{
		HeaderName = name;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The name for the header.
	/// </summary>
	public string HeaderName { get; set; }

	#endregion
}