#region References

using System;
using Cornerstone.Avalonia.Drawing;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Drawing;

[TestClass]
public class DrawingContextHelperTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FormatNumber()
	{
		// 24 digits + sign + dot + safety
		Span<int> buffer = stackalloc int[32];

		var scenarios = new (double Value, int Precision, int DecimalPosition, bool HasSign, int[] Digits)[]
		{
			// 0..5 ── Already good ones (for reference) ──
			(0.0, 2, 2, false, [0, -1, 0, 0]),
			(1.0, 2, 2, false, [1, -1, 0, 0]),
			(1.234, 2, 2, false, [1, -1, 2, 3]),
			(1.23456, 1, 1, false, [1, -1, 2]),
			(1.23456, 5, 5, false, [1, -1, 2, 3, 4, 5, 6]),
			(1.23456, 4, 4, false, [1, -1, 2, 3, 4, 5]),

			// 6..9 ── Negative numbers ──
			(-0.0, 2, 2, false, [0, -1, 0, 0]),
			(-1.0, 2, 2, true, [1, -1, 0, 0]),
			(-12.345, 2, 2, true, [1, 2, -1, 3, 4]),
			(-0.0004, 3, 3, true, [0, -1, 0, 0, 0]),

			// 10..12 ── Numbers < 1 (leading zero before dot) ──
			(0.000, 3, 3, false, [0, -1, 0, 0, 0]),
			(0.00789, 4, 4, false, [0, -1, 0, 0, 7, 8]),
			(0.9999, 2, 2, false, [0, -1, 9, 9]),

			// 13..16 ── Precision = 0 (integer only, no dot) ──
			(123.456, 0, 0, false, [1, 2, 3]),
			(-987.6, 0, 0, true, [9, 8, 7]),
			(0.499, 0, 0, false, [0]),
			(0.5, 0, 0, false, [0]),

			// 17..19 ── Very large / very small numbers ──
			(1234567.89, 2, 2, false, [1, 2, 3, 4, 5, 6, 7, -1, 8, 8]),
			(1e-10, 5, 5, false, [0, -1, 0, 0, 0, 0, 0]),
			(999999.999, 3, 3, false, [9, 9, 9, 9, 9, 9, -1, 9, 9, 9]),

			// 20..22 ── Special floating-point values ──
			(double.NaN, 2, 0, false, [0]),
			(double.PositiveInfinity, 2, 0, false, [0]),
			(double.NegativeInfinity, 2, 0, true, [0]),

			// 23..26 ── Rounding at boundaries ──
			(0.994999, 3, 3, false, [0, -1, 9, 9, 4]),
			(0.995, 2, 2, false, [0, -1, 9, 9]),
			(9.996, 2, 2, false, [9, -1, 9, 9]),
			(1.999999, 5, 5, false, [1, -1, 9, 9, 9, 9, 9])
		};

		for (var index = 0; index < scenarios.Length; index++)
		{
			$"Scenario: {index}".Dump();
			var scenario = scenarios[index];
			var f = DrawingContextHelper.FormattedNumberSpan.Create(scenario.Value, scenario.Precision, buffer);
			var d = f.Digits.ToArray();
			AreEqual(scenario.DecimalPosition, f.DecimalPosition);
			AreEqual(scenario.HasSign, f.HasSign);
			AreEqual(scenario.Digits, f.Digits.ToArray(), () => string.Join(", ", d));
		}
	}

	#endregion
}