#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Data.Charts;

/// <summary>
/// Represents a chart data.
/// </summary>
public class ChartData
{
	#region Properties

	/// <summary>
	/// The data for this chart.
	/// </summary>
	public IList<ChartDataset> Datasets { get; set; }

	/// <summary>
	/// The labels for this chart.
	/// </summary>
	public IList<string> Labels { get; set; }

	#endregion
}