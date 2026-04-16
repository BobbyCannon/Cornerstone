#region References

using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class TextRangeOverlapsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void OverlapsNull()
	{
		var range = new TextRange(5, 10);
		IsFalse(range.Overlaps(null));
	}

	[TestMethod]
	public void OverlapsSameInstance()
	{
		var range = new TextRange(7, 14);
		IsTrue(range.Overlaps(range));
	}

	[TestMethod]
	public void OverlapsScenarios()
	{
		var scenarios = new (int thisStart, int thisEnd, int otherStart, int otherEnd, bool expected)[]
		{
			// ────────────────────────────────────────────────
			// Your original cases (kept as-is)
			// ────────────────────────────────────────────────
			(5, 10, 7, 12, true), // partial overlap right
			(5, 10, 2, 6, true), // partial overlap left
			(5, 10, 3, 12, true), // this completely inside other
			(5, 10, 6, 8, true), // other completely inside this
			(5, 10, 5, 10, true), // equal non-empty
			(5, 10, 4, 5, false), // touches left edge (exclusive → no overlap)
			(5, 10, 10, 15, false), // touches right edge (exclusive → no overlap)
			(5, 10, 0, 4, false), // completely before
			(5, 10, 11, 15, false), // completely after

			// ────────────────────────────────────────────────
			// Empty range cases — according to your latest preference
			// (empty overlaps non-empty if position is inside,
			//  empty overlaps empty only if same position)
			// ────────────────────────────────────────────────
			(10, 10, 5, 15, true), // empty caret inside non-empty
			(5, 15, 10, 10, true), // symmetric — non-empty contains caret
			(10, 10, 10, 10, true), // same empty position → should overlap (your request)
			(10, 10, 11, 11, false), // different empty positions → no
			(10, 10, 9, 10, false), // empty exactly at exclusive end → no overlap
			(9, 10, 10, 10, false), // symmetric — non-empty ends where empty starts
			(7, 7, 5, 10, true), // empty inside (left half)
			(10, 10, 10, 20, true), // empty at start of non-empty (start is inclusive)
			(15, 15, 10, 15, false), // empty exactly at exclusive end

			// ────────────────────────────────────────────────
			// Length-1 ranges (single character positions)
			// ────────────────────────────────────────────────
			(5, 6, 5, 6, true), // same single char
			(5, 6, 4, 5, false), // touches left
			(5, 6, 6, 7, false), // touches right
			(5, 6, 4, 6, true), // other contains this single char
			(4, 7, 5, 6, true), // this contains single-char other
			(5, 6, 5, 7, true), // partial overlap right

			// ────────────────────────────────────────────────
			// Negative offsets (if your system allows them)
			// ────────────────────────────────────────────────
			(-3, 2, -1, 4, true), // normal overlap crossing zero
			(-5, -2, -10, -3, true), // partial overlap left in negatives
			(-2, -2, -2, -2, true), // same empty negative position
			(-2, -2, -1, -1, false), // different empty negative positions
			(0, 0, -1, 1, true), // empty at 0 inside [-1,1)

			// ────────────────────────────────────────────────
			// Large / extreme values
			// ────────────────────────────────────────────────
			(0, 1000000, 500000, 500001, true), // tiny inside huge
			(int.MaxValue - 5, int.MaxValue, int.MaxValue - 3, int.MaxValue, true), // near int edge
			(int.MinValue, int.MinValue + 10, int.MinValue + 2, int.MinValue + 8, true),

			// ────────────────────────────────────────────────
			// More symmetric / reverse cases
			// ────────────────────────────────────────────────
			(7, 12, 5, 10, true), // symmetric to your first partial overlap
			(2, 6, 5, 10, true), // symmetric to second
			(10, 30, 20, 25, true), // large contains tiny
			(20, 25, 10, 30, true) // tiny inside large (already in original, kept)
		};

		foreach (var scenario in scenarios)
		{
			var thisRange = new TextRange(scenario.thisStart, scenario.thisEnd);
			var otherRange = new TextRange(scenario.otherStart, scenario.otherEnd);
			var actual = thisRange.Overlaps(otherRange);

			AreEqual(scenario.expected, actual, () =>
				$"Ranges [{scenario.thisStart},{scenario.thisEnd}) " +
				$"and [{scenario.otherStart},{scenario.otherEnd}) " +
				$"should {(scenario.expected ? "" : "NOT ")}overlap"
			);
		}
	}

	#endregion
}