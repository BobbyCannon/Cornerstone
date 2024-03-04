#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data.Charts;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// A service for creating text charts.
/// </summary>
public class AsciiChartService
{
	#region Methods

	/// <summary>
	/// Generate the chart by the provided values.
	/// </summary>
	/// <param name="type"> The type of chart to create. </param>
	/// <param name="data"> The data for the chart. </param>
	/// <param name="rows"> The text row count (height). </param>
	/// <param name="columns"> The text column count (width). </param>
	/// <returns></returns>
	public string Generate(ChartType type, ChartData data, int rows, int columns)
	{
		return type switch
		{
			ChartType.Bar => BarChart(data, rows, columns),
			ChartType.HorizontalBar => HorizontalBarChart(data, rows, columns),
			_ => "This chart is not implemented yet."
		};
	}

	private string BarChart(ChartData chart, int rows, int columns)
	{
		if (columns == 0)
		{
			columns = chart.Datasets.Max(x => x.Data.Count);
		}

		if (rows == 0)
		{
			rows = columns * 2;
		}

		var builder = new TextBuilder();
		builder.Fill(' ', 0, rows * columns);
		return builder.ToString();
	}

	private string HorizontalBarChart(ChartData chart, int rows, int columns)
	{
		if (rows == 0)
		{
			rows = chart.Datasets.Max(x => x.Data.Count());
		}

		var builder = new TextBuilder();
		builder.Fill(' ', 0, (rows * columns) + ((rows - 1) * 2));
		var labels = chart.Labels?.ToList() ?? new List<string>();
		var labelMaxWidth = labels.Count > 0 ? labels.Max(x => x.Length) + 1 : 0;

		foreach (var dataset in chart.Datasets)
		{
			var maxValue = dataset.Data.Max();
			var columnsPerPercentage = 100m / (columns - labelMaxWidth) / 100;
			var currentRow = 0;

			foreach (var data in dataset.Data)
			{
				var percentage = data / maxValue;
				var range = (int)(percentage / columnsPerPercentage);
				var rowStartOffset = (currentRow * columns) + (currentRow * 2);
				var rowBarOffset = rowStartOffset + labelMaxWidth;
				builder.Fill('█', rowBarOffset, range);

				var rowEndOffset = (rowStartOffset + columns);
				builder.Set('\r', rowEndOffset + 1);
				builder.Set('\n', rowEndOffset + 2);
				currentRow++;
			}
		}

		return builder.ToString();
	}

	#endregion
}