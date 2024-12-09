#region References

using System;
using System.Linq;
using Cornerstone.Data.Times;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data.Times;

[TestClass]
public class HolidayTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetHolidays()
	{
		var scenarios = new (int year, HolidayValue[])[]
		{
			(
				2024,
				[
					new HolidayValue
					{
						Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.NewYearsDay,
						InLieuDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
					},
					new HolidayValue
					{
						Date = new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.Easter,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2024, 3, 29, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.GoodFriday,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2024, 9, 2, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.LaborDay,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2024, 11, 28, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.ThanksgivingDay,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.ChristmasDay,
						InLieuDate = new DateTime(2024, 12, 25, 0, 0, 0, DateTimeKind.Local)
					}
				]
			),
			(
				2025,
				[
					new HolidayValue
					{
						Date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.NewYearsDay,
						InLieuDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local)
					},
					new HolidayValue
					{
						Date = new DateTime(2025, 4, 13, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.Easter,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2025, 4, 11, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.GoodFriday,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.LaborDay,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2025, 11, 27, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.ThanksgivingDay,
						InLieuDate = null
					},
					new HolidayValue
					{
						Date = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Local),
						Holiday = Holiday.ChristmasDay,
						InLieuDate = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Local)
					}
				]
			)
		};

		foreach (var scenario in scenarios)
		{
			var actual = HolidayService.GetHolidays(scenario.year);
			AreEqual(scenario.Item2, actual.ToArray(),
				() => actual.DumpCSharp(new CodeWriterSettings
				{
					OutputMode = CodeWriterMode.Instance,
					TextFormat = TextFormat.Indented,
					IgnoreReadOnly = true
				})
			);
		}
	}

	#endregion
}