#region References

using System.Collections.Generic;
using Cornerstone.Data.Charts;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class AsciiChartServiceTest : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void HorizontalBar()
	{
		var service = new AsciiChartService();
		var actual = service.Generate(ChartType.HorizontalBar, new ChartData
			{
				Datasets = new List<ChartDataset>
				{
					new() { Data = [1, 2, 3, 4, 5], Label = "Row 1", Type = "bar" }
				}
			},
			80,
			20
		);

		//var expected = "";
		actual.Dump();
		//AreEqual(expected, actual);
	}

	#endregion
}