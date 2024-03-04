namespace Cornerstone.Data.Charts;

/// <summary>
/// Represents the different types of charts.
/// </summary>
public enum ChartType
{
	/// <summary>
	/// Unknown
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Bar Chart
	/// </summary>
	Bar = 1,

	/// <summary>
	/// Line Chart
	/// </summary>
	Line = 2,

	/// <summary>
	/// Pie Chart
	/// </summary>
	Pie = 3,

	/// <summary>
	/// Donut Chart
	/// </summary>
	Doughnut = 4,

	/// <summary>
	/// List Chart
	/// </summary>
	List = 5,

	/// <summary>
	/// Stacked Bar Chart
	/// </summary>
	StackedBar = 6,

	/// <summary>
	/// Horizontal Bar Chart
	/// </summary>
	HorizontalBar = 7
}