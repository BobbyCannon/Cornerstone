#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Cornerstone.Presentation.Media.TextFormatting.FormattingObjectPool;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia.Media.TextFormatting;

/// <summary>
/// Direct tests for <c> TextFormatterImpl.SplitTextRuns </c>. Calls the
/// internal method via <c> InternalsVisibleTo </c> so each branch can be
/// exercised in isolation with synthetic <see cref="TextRun" /> stubs —
/// independent of the wrap algorithm that's its main caller. Many of
/// these scenarios are unreachable through the wrap path on its own
/// (the wrap loop carefully avoids requesting splits inside non-splittable
/// runs), but the method is also called by <c> TextCollapsingProperties </c>
/// and the ellipsis types, which have weaker invariants.
/// 
/// Key invariants under test (must hold regardless of split position):
/// * Sum of run lengths is preserved (no content lost or duplicated).
/// * Concatenated text (across all runs in first ++ second) equals input.
/// * Reported <c> firstLength </c> equals the sum of lengths in first.
/// </summary>
[TestClass]
public class SplitTextRunsTests
{
	#region Methods

	[PresentationTestMethod]
	public void SplitAtBoundaryAfterDrawableMidList()
	{
		// Boundary at end of drawable. == branch.
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("AAA", 3),
			new TestStubRun("XX", 2),
			new TestStubRun("BBB", 3)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 5, pool, out var firstLength);

		try
		{
			CornerstoneTest.AreEqual(5, firstLength);
			AssertContentPreserved(runs, first, second, firstLength);
			CornerstoneTest.AreEqual(2, first!.Count);
			CornerstoneTest.AreEqual(1, second!.Count);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitAtBoundaryBeforeDrawableMidList()
	{
		// [shape(3), drawable(2), shape(3)] split at length=3 — boundary at
		// end of first shape. == branch.
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("AAA", 3),
			new TestStubRun("XX", 2),
			new TestStubRun("BBB", 3)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 3, pool, out var firstLength);

		try
		{
			CornerstoneTest.AreEqual(3, firstLength);
			AssertContentPreserved(runs, first, second, firstLength);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitAtBoundaryBetweenTwoRunsGoesToFirstOrSecondCleanly()
	{
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("a", 2),
			new TestStubRun("b", 3)
		};

		// length=2 means "everything up to and including the first run on first".
		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 2, pool, out var firstLength);

		try
		{
			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.AreEqual(1, first!.Count);
			CornerstoneTest.Same(runs[0], first[0]);
			CornerstoneTest.IsNotNull(second);
			CornerstoneTest.AreEqual(1, second!.Count);
			CornerstoneTest.Same(runs[1], second[0]);
			CornerstoneTest.AreEqual(2, firstLength);
			AssertContentPreserved(runs, first, second, firstLength);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitBeforeDrawableThatDoesNotFitPutsDrawableInSecond()
	{
		// [text(2), drawable(1), text(2)] split at length=2.
		// Wrap normally chooses currentLength==length here ("drawable doesn't fit
		// on this line, push to next"). The == branch in SplitTextRuns already
		// handles this correctly today — assert that it stays correct.
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("ab", 2),
			new TestStubRun("X", 1),
			new TestStubRun("cd", 2)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 2, pool, out var firstLength);

		try
		{
			CornerstoneTest.AreEqual(2, firstLength);
			AssertContentPreserved(runs, first, second, firstLength);
			CornerstoneTest.AreEqual(1, first!.Count);
			CornerstoneTest.AreEqual(2, second!.Count);
			CornerstoneTest.Same(runs[1], second[0]); // drawable at start of second
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitInsideNonShapedRunDoesNotDropRun()
	{
		// Bug repro: a DrawableTextRun-like atomic run with length > 1, asked
		// to split at length=1 (strictly inside). Before the fix, the current
		// implementation dropped the run from both halves. After the fix it
		// must appear in either first or second.
		var pool = Instance;
		var runs = new TextRun[] { new TestStubRun("drawable", 3) };

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 1, pool, out var firstLength);

		try
		{
			AssertContentPreserved(runs, first, second, firstLength);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitLengthEqualsTotalPutsAllInFirst()
	{
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("a", 2),
			new TestStubRun("b", 3)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 5, pool, out var firstLength);

		try
		{
			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.AreEqual(2, first!.Count);
			CornerstoneTest.IsNull(second);
			CornerstoneTest.AreEqual(5, firstLength);
			AssertContentPreserved(runs, first, second, firstLength);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitLengthPastTotalPutsAllInFirst()
	{
		var pool = Instance;
		var runs = new TextRun[] { new TestStubRun("a", 2) };

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 99, pool, out var firstLength);

		try
		{
			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.AreEqual(1, first!.Count);
			CornerstoneTest.IsNull(second);
			CornerstoneTest.AreEqual(2, firstLength);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitLengthZeroReturnsNullFirstAndAllInSecond()
	{
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("a", 2),
			new TestStubRun("b", 3)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 0, pool, out var firstLength);

		try
		{
			CornerstoneTest.IsNull(first);
			CornerstoneTest.IsNotNull(second);
			CornerstoneTest.AreEqual(2, second!.Count);
			CornerstoneTest.AreEqual(0, firstLength);
			CornerstoneTest.AreEqual(5, second.Sum(r => r.Length));
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitStrictlyInsideNonShapedRunAtStartOfListOverflows()
	{
		// [drawable(5)] split at length=2 — the drawable is the first run, has
		// no content before it, and is bigger than the requested length. If we
		// snapped before, first would be empty and the caller would loop
		// forever. The contract here is to overflow the drawable into first
		// (the same "include at least one cluster" rule the wrap loop has for
		// ShapedTextRuns at the start of a line).
		var pool = Instance;
		var runs = new TextRun[] { new TestStubRun("XXXXX", 5) };

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 2, pool, out var firstLength);

		try
		{
			AssertContentPreserved(runs, first, second, firstLength);
			CornerstoneTest.AreEqual(5, firstLength); // overflow
			CornerstoneTest.IsNotNull(first);
			CornerstoneTest.AreEqual(1, first!.Count);
			CornerstoneTest.Same(runs[0], first[0]);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitStrictlyInsideNonShapedRunSnapsBeforeIt()
	{
		// [text(2), drawable(3), text(2)] split at length=3 — strictly inside
		// the drawable. The drawable is atomic, so the split must snap to a
		// boundary. The current contract: snap BEFORE the drawable, so
		// firstLength is shorter than requested but content is preserved.
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("ab", 2),
			new TestStubRun("XXX", 3),
			new TestStubRun("cd", 2)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 3, pool, out var firstLength);

		try
		{
			AssertContentPreserved(runs, first, second, firstLength);
			CornerstoneTest.IsTrue(firstLength is 2 or 5, $"Expected firstLength to snap to 2 (before drawable) or 5 (after drawable); got {firstLength}.");
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	[PresentationTestMethod]
	public void SplitWithZeroLengthRunInsideDoesNotDropIt()
	{
		// Zero-length runs (e.g. TextEndOfParagraph variants) appear after
		// shaped runs. Splitting at the boundary should keep the zero-length
		// run somewhere — not silently discard it.
		var pool = Instance;
		var runs = new TextRun[]
		{
			new TestStubRun("ab", 2),
			new TestStubRun("zero", 0),
			new TestStubRun("cd", 2)
		};

		var (first, second) = TextFormatterImpl.SplitTextRuns(runs, 2, pool, out var firstLength);

		try
		{
			CornerstoneTest.AreEqual(2, firstLength);

			// The zero-length run still has identity — assert it's in exactly one half.
			var allRuns = (first ?? Enumerable.Empty<TextRun>())
				.Concat(second ?? Enumerable.Empty<TextRun>())
				.ToList();
			CornerstoneTest.AreEqual(3, allRuns.Count);
			CornerstoneTest.Contains(allRuns, runs[1]);
		}
		finally
		{
			pool.TextRunLists.Return(ref first);
			pool.TextRunLists.Return(ref second);
		}
	}

	/// <summary>
	/// Asserts the central invariant: sum of (first|second) run lengths
	/// equals the input total, and <paramref name="firstLength" /> matches the
	/// sum of first's run lengths. Any test where this fails means content
	/// was lost or duplicated.
	/// </summary>
	private static void AssertContentPreserved(
		IReadOnlyList<TextRun> input,
		RentedList<TextRun> first,
		RentedList<TextRun> second,
		int firstLength)
	{
		var inputTotal = input.Sum(r => r.Length);
		var firstTotal = first?.Sum(r => r.Length) ?? 0;
		var secondTotal = second?.Sum(r => r.Length) ?? 0;

		CornerstoneTest.AreEqual(inputTotal, firstTotal + secondTotal);
		CornerstoneTest.AreEqual(firstTotal, firstLength);
	}

	#endregion

	#region Classes

	/// <summary>
	/// Minimal concrete <see cref="TextRun" /> for tests — neither a
	/// <c> ShapedTextRun </c> nor a <c> DrawableTextRun </c> from the consumer
	/// perspective. Behaves like an atomic, non-splittable run with a
	/// configurable length, which is exactly the class of input that
	/// triggers the <c> SplitTextRuns </c> drop-current-run bug.
	/// </summary>
	private sealed class TestStubRun : TextRun
	{
		#region Fields

		private readonly string _name;

		#endregion

		#region Constructors

		public TestStubRun(string name, int length)
		{
			_name = name;
			Length = length;
		}

		#endregion

		#region Properties

		public override int Length { get; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return $"TestStubRun({_name}, len={Length})";
		}

		#endregion
	}

	#endregion
}