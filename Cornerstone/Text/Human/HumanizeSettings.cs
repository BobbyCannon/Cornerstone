#region References

using Cornerstone.Data.Times;

#endregion

namespace Cornerstone.Text.Human;

/// <inheritdoc />
public struct HumanizeSettings : IHumanizeSettings
{
	#region Constructors

	/// <summary>
	/// Initialize the default settings
	/// </summary>
	public HumanizeSettings()
	{
		MaxUnit = TimeUnit.Year;
		MaxUnitSegments = 2;
		MinUnit = TimeUnit.Ticks;
		Precision = 3;
		WordFormat = WordFormat.Full;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public TimeUnit MaxUnit { get; set; }

	/// <inheritdoc />
	public int MaxUnitSegments { get; set; }

	/// <inheritdoc />
	public TimeUnit MinUnit { get; set; }

	/// <inheritdoc />
	public int Precision { get; set; }

	/// <inheritdoc />
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