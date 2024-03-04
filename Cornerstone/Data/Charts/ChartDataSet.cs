#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Data.Charts;

/// <summary>
/// Base chart data for all charts.
/// </summary>
public class ChartDataset
{
	#region Properties

	/// <summary>
	/// The chart data points.
	/// </summary>
	public IList<decimal> Data { get; set; }

	/// <summary>
	/// The label for the dataset which appears in the legend and tooltips.
	/// </summary>
	public string Label { get; set; }

	/// <summary>
	/// The type of the chart data
	/// </summary>
	public string Type { get; set; }

	#endregion
}