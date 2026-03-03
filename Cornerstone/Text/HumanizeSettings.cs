#region References

using Cornerstone.Data.Times;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text;

[SourceReflection]
public struct HumanizeSettings : IHumanizeSettings
{
	#region Constructors

	/// <summary>
	/// Initialize the default settings
	/// </summary>
	public HumanizeSettings()
	{
		MaxUnit = TimeUnit.Year;
		MaxUnitSegments = 1;
		MinUnit = TimeUnit.Ticks;
		Precision = 0;
		WordFormat = WordFormat.Abbreviation;
	}

	#endregion

	#region Properties

	public TimeUnit MaxUnit { get; set; }

	public int MaxUnitSegments { get; set; }

	public TimeUnit MinUnit { get; set; }

	public int Precision { get; set; }

	public WordFormat WordFormat { get; set; }

	#endregion
}

/// <summary>
/// The setting when humanizing content.
/// </summary>
public interface IHumanizeSettings
{
	#region Properties

	/// <summary>
	/// The maximum unit when converting time.
	/// </summary>
	TimeUnit MaxUnit { get; set; }

	/// <summary>
	/// The number of unit segments.
	/// Ex. 1 max unit for time span of 01:12:36 => 1 h
	/// 2 max unit for time span of 01:12:36 => 1 h and 12 m
	/// </summary>
	int MaxUnitSegments { get; set; }

	/// <summary>
	/// The minimum unit when converting time.
	/// </summary>
	TimeUnit MinUnit { get; set; }

	/// <summary>
	/// The number precision when dealing with decimals.
	/// </summary>
	int Precision { get; set; }

	/// <summary>
	/// The format for words.
	/// </summary>
	WordFormat WordFormat { get; set; }

	#endregion
}