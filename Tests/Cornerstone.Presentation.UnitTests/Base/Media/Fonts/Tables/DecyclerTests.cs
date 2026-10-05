#region References

using System;
using Cornerstone.Presentation.Media.Fonts.Tables;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts.Tables;

[TestClass]
public class DecyclerTests
{
	#region Methods

	[PresentationTestMethod]
	public void ConstructorAcceptsMaxDepthOfOne()
	{
		var decycler = new Decycler<int>(1);

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
		CornerstoneTest.AreEqual(1, decycler.MaxDepth);
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	public void ConstructorThrowsWhenMaxDepthIsLessThanOne(int maxDepth)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Decycler<int>(maxDepth));
	}

	[PresentationTestMethod]
	public void CopiedGuardCannotDoubleExit()
	{
		// CycleGuard is a copyable ref struct, so each copy carries its own _exited flag —
		// the idempotence of Dispose on one copy doesn't protect against the other copy
		// exiting again. Exit only returns depth budget for ids actually in the visited set.
		var decycler = new Decycler<int>(4);

		var guard = decycler.Enter(1);
		var copy = guard;

		guard.Dispose();
		copy.Dispose(); // second exit for id 1, via an un-exited copy.

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void DecyclerExceptionCarriesErrorCodeAndMessage()
	{
		var ex = new DecyclerException(DecyclerError.CycleDetected, "boom");

		CornerstoneTest.AreEqual(DecyclerError.CycleDetected, ex.Error);
		CornerstoneTest.AreEqual("boom", ex.Message);
	}

	[PresentationTestMethod]
	public void DepthLimitCheckRunsBeforeCycleCheck()
	{
		// When both conditions could apply (depth is exhausted AND the id is
		// already visited), the depth check fires first. This matters because
		// it means a misconfigured depth cap surfaces as a depth error rather
		// than masquerading as a cycle.
		var decycler = new Decycler<int>(1);

		using var guard = decycler.Enter(1);

		var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(1));

		CornerstoneTest.AreEqual(DecyclerError.DepthLimitExceeded, ex.Error);
	}

	[PresentationTestMethod]
	public void DisposingGuardRestoresCurrentDepth()
	{
		var decycler = new Decycler<int>(4);

		using (decycler.Enter(1))
		{
			CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
		}

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void EnterBeyondMaxDepthThrowsDepthLimitExceeded()
	{
		var decycler = new Decycler<int>(2);

		using var a = decycler.Enter(1);
		using var b = decycler.Enter(2);

		var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(3));

		CornerstoneTest.AreEqual(DecyclerError.DepthLimitExceeded, ex.Error);
	}

	[PresentationTestMethod]
	public void EnterIncrementsCurrentDepth()
	{
		var decycler = new Decycler<int>(4);

		using var guard = decycler.Enter(1);

		CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void FailedEnterDoesNotMutateState()
	{
		var decycler = new Decycler<int>(1);

		using var guard = decycler.Enter(1);

		Assert.Throws<DecyclerException>(() => decycler.Enter(2));

		// The failed Enter must not have incremented depth or registered the id.
		CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void GuardDisposeIsIdempotent()
	{
		var decycler = new Decycler<int>(4);

		var guard = decycler.Enter(1);

		guard.Dispose();
		guard.Dispose(); // second call must not double-decrement.

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);

		// Depth must not be negative; entering a new node still works.
		using var next = decycler.Enter(2);

		CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void MaxDepthPropertyReflectsConstructorArgument()
	{
		var decycler = new Decycler<int>(17);

		CornerstoneTest.AreEqual(17, decycler.MaxDepth);
	}

	[PresentationTestMethod]
	public void NestedEntersStackDepthAndUnwindInReverse()
	{
		var decycler = new Decycler<int>(4);

		using (decycler.Enter(1))
		{
			CornerstoneTest.AreEqual(1, decycler.CurrentDepth);

			using (decycler.Enter(2))
			{
				CornerstoneTest.AreEqual(2, decycler.CurrentDepth);

				using (decycler.Enter(3))
				{
					CornerstoneTest.AreEqual(3, decycler.CurrentDepth);
				}

				CornerstoneTest.AreEqual(2, decycler.CurrentDepth);
			}

			CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
		}

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void ReEnteringVisitedIdThrowsCycleDetected()
	{
		var decycler = new Decycler<int>(4);

		using var outer = decycler.Enter(1);

		var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(1));

		CornerstoneTest.AreEqual(DecyclerError.CycleDetected, ex.Error);
	}

	[PresentationTestMethod]
	public void ResetClearsVisitedAndDepth()
	{
		var decycler = new Decycler<int>(4);

		var guard = decycler.Enter(1);

		// Skip the disposal: simulate an abandoned traversal that needs to be
		// cleaned up by Reset (the validator path on the pool).
		_ = guard;

		decycler.Reset();

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);

		// The previously visited id must be enterable again after reset.
		using var fresh = decycler.Enter(1);

		CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void ResetIsSafeToCallOnEmptyDecycler()
	{
		var decycler = new Decycler<int>(4);

		decycler.Reset();
		decycler.Reset();

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void SameIdCanBeReEnteredAfterExit()
	{
		var decycler = new Decycler<int>(4);

		using (decycler.Enter(1))
		{
		}

		// The id is no longer in the visited set; re-entering must succeed.
		using var guard = decycler.Enter(1);

		CornerstoneTest.AreEqual(1, decycler.CurrentDepth);
	}

	[PresentationTestMethod]
	public void WorksWithOtherStructTypes()
	{
		// Decycler<T> is constrained to struct; the typical instantiations are
		// int (composite-glyph ids) and ushort (paint-graph glyph ids). Verify
		// ushort works end-to-end.
		var decycler = new Decycler<ushort>(4);

		using (decycler.Enter(1))
		{
			Assert.Throws<DecyclerException>(() => decycler.Enter(1));
		}

		CornerstoneTest.AreEqual(0, decycler.CurrentDepth);
	}

	#endregion
}