#region References

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangesInvalidateArrangedControl()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = control.Arranged = false;

		control.InvalidateArrange();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(control.Measured);
		CornerstoneTest.IsTrue(control.Arranged);
	}

	[PresentationTestMethod]
	public void ArrangesRootWithDesiredSize()
	{
		var root = new LayoutTestRoot
		{
			Width = 100,
			Height = 100
		};

		var arrangeSize = default(Size);

		root.DoArrangeOverride = (_, s) =>
		{
			arrangeSize = s;
			return s;
		};

		root.LayoutManager.ExecuteInitialLayoutPass();
		CornerstoneTest.AreEqual(new Size(100, 100), arrangeSize);

		root.Width = 120;

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(new Size(120, 100), arrangeSize);
	}

	[PresentationTestMethod]
	public void CallingExecuteLayoutPassFromExecuteInitialLayoutPassDoesNotBreakMeasure()
	{
		// Test for issue #3550.
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };
		var count = 0;

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = false;

		control.DoMeasureOverride = (l, s) =>
		{
			if (count++ == 0)
			{
				control.InvalidateMeasure();
				root.LayoutManager.ExecuteLayoutPass();
				return new Size(100, 100);
			}
			return new Size(200, 200);
		};

		root.InvalidateMeasure();
		control.InvalidateMeasure();
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(new Size(200, 200), control.Bounds.Size);
		CornerstoneTest.AreEqual(new Size(200, 200), control.DesiredSize);
	}

	[PresentationTestMethod]
	public void ChildCanInvalidateParentMeasureDuringArrange()
	{
		// Issue #11015.
		//
		// - Child invalidates parent measure in arrange pass
		// - Parent is added to measure & arrange queues
		// - Arrange pass dequeues parent
		// - Measure is not valid so parent is not arranged
		// - Parent is measured
		// - Parent has been dequeued from arrange queue so no arrange is performed
		var child = new LayoutTestControl();
		var parent = new LayoutTestControl { Child = child };
		var root = new LayoutTestRoot { Child = parent };

		root.LayoutManager.ExecuteInitialLayoutPass();

		child.DoArrangeOverride = (_, s) =>
		{
			parent.InvalidateMeasure();
			return s;
		};

		child.InvalidateMeasure();
		parent.InvalidateMeasure();

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsTrue(child.IsArrangeValid);
		CornerstoneTest.IsTrue(parent.IsMeasureValid);
		CornerstoneTest.IsTrue(parent.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void DoesntMeasureAndArrangeInvalidateMeasuredControlWhenAncestorIsNotVisible()
	{
		var control = new LayoutTestControl();
		var parent = new Decorator { Child = control };
		var root = new LayoutTestRoot { Child = parent };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = control.Arranged = false;

		parent.IsVisible = false;
		control.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(control.Measured);
		CornerstoneTest.IsFalse(control.Arranged);
	}

	[PresentationTestMethod]
	public void DoesntMeasureAndArrangeInvalidateMeasuredControlWhenTopLevelIsNotVisible()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control, IsVisible = false };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = control.Arranged = false;

		control.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(control.Measured);
		CornerstoneTest.IsFalse(control.Arranged);
	}

	[PresentationTestMethod]
	public void DoesntMeasureNonInvalidatedRoot()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		root.Measured = root.Arranged = false;
		control.Measured = control.Arranged = false;

		control.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(root.Measured);
		CornerstoneTest.IsFalse(root.Arranged);
		CornerstoneTest.IsTrue(control.Measured);
		CornerstoneTest.IsTrue(control.Arranged);
	}

	[PresentationTestMethod]
	public void DoesntMeasureRemovedControl()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = control.Arranged = false;

		control.InvalidateMeasure();
		root.Child = null;
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsFalse(control.Measured);
		CornerstoneTest.IsFalse(control.Arranged);
	}

	[PresentationTestMethod]
	public void GrandparentCanInvalidateRootMeasureDuringArrange()
	{
		// Issue #11161.
		var child = new LayoutTestControl();
		var parent = new LayoutTestControl { Child = child };
		var grandparent = new LayoutTestControl { Child = parent };
		var root = new LayoutTestRoot { Child = grandparent };

		root.LayoutManager.ExecuteInitialLayoutPass();

		grandparent.DoArrangeOverride = (_, s) =>
		{
			root.InvalidateMeasure();
			return s;
		};
		grandparent.CallBaseArrange = true;

		child.InvalidateMeasure();
		grandparent.InvalidateMeasure();

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsTrue(child.IsArrangeValid);
		CornerstoneTest.IsTrue(parent.IsMeasureValid);
		CornerstoneTest.IsTrue(parent.IsArrangeValid);
		CornerstoneTest.IsTrue(grandparent.IsMeasureValid);
		CornerstoneTest.IsTrue(grandparent.IsArrangeValid);
		CornerstoneTest.IsTrue(root.IsMeasureValid);
		CornerstoneTest.IsTrue(root.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void GreatGrandparentCanInvalidateGrandparentMeasureDuringArrange()
	{
		// Issue #7706 (second part: scrollbar gets stuck)
		var child = new LayoutTestControl();
		var parent = new LayoutTestControl { Child = child };
		var grandparent = new LayoutTestControl { Child = parent };
		var greatGrandparent = new LayoutTestControl { Child = grandparent };
		var root = new LayoutTestRoot { Child = greatGrandparent };

		root.LayoutManager.ExecuteInitialLayoutPass();

		greatGrandparent.DoArrangeOverride = (_, s) =>
		{
			grandparent.InvalidateMeasure();
			return s;
		};

		child.InvalidateArrange();
		greatGrandparent.InvalidateArrange();

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsTrue(child.IsArrangeValid);
		CornerstoneTest.IsTrue(parent.IsMeasureValid);
		CornerstoneTest.IsTrue(parent.IsArrangeValid);
		CornerstoneTest.IsTrue(greatGrandparent.IsMeasureValid);
		CornerstoneTest.IsTrue(greatGrandparent.IsArrangeValid);
		CornerstoneTest.IsTrue(root.IsMeasureValid);
		CornerstoneTest.IsTrue(root.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void InvalidatingChildRemeasuresParent()
	{
		Border border;
		StackPanel panel;

		var root = new LayoutTestRoot
		{
			Child = panel = new StackPanel
			{
				Children =
				{
					(border = new Border())
				}
			}
		};

		root.LayoutManager.ExecuteInitialLayoutPass();
		CornerstoneTest.AreEqual(new Size(0, 0), root.DesiredSize);

		border.Width = 100;
		border.Height = 100;

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(new Size(100, 100), panel.DesiredSize);
	}

	[PresentationTestMethod]
	public void LayoutManagerExecuteLayoutPassShouldClearQueuedLayoutPasses()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		var layoutCount = 0;
		root.LayoutUpdated += (_, _) => layoutCount++;

		root.LayoutManager.InvalidateArrange(control);
		root.LayoutManager.ExecuteInitialLayoutPass();

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Render, CancellationToken.None);

		CornerstoneTest.AreEqual(1, layoutCount);
	}

	[PresentationTestMethod]
	public void LayoutManagerShouldPreventInfiniteLoopOnArrange()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Arranged = false;

		var cnt = 0;
		var maxcnt = 100;
		control.DoArrangeOverride = (l, s) =>
		{
			//emulate a problem in the logic of a control that triggers
			//invalidate measure during arrange
			//it can lead to infinity loop in layoutmanager
			if (++cnt < maxcnt)
			{
				control.InvalidateArrange();
			}

			return new Size(100, 100);
		};

		control.InvalidateArrange();

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(cnt < 100);
	}

	[PresentationTestMethod]
	public void LayoutManagerShouldPreventInfiniteLoopOnMeasure()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = false;

		var cnt = 0;
		var maxcnt = 100;
		control.DoMeasureOverride = (l, s) =>
		{
			//emulate a problem in the logic of a control that triggers
			//invalidate measure during measure
			//it can lead to an infinite loop in layoutmanager
			if (++cnt < maxcnt)
			{
				control.InvalidateMeasure();
			}

			return new Size(100, 100);
		};

		control.InvalidateMeasure();

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(cnt < 100);
	}

	[PresentationTestMethod]
	public void LayoutManagerShouldProperlyArrangeVisualsEvenWhenThereAreIssuesWithPreviousArranged()
	{
		var nonArrageableTargets = Enumerable.Range(1, 10).Select(_ => new LayoutTestControl()).ToArray();
		var targets = Enumerable.Range(1, 10).Select(_ => new LayoutTestControl()).ToArray();

		StackPanel panel;

		var root = new LayoutTestRoot
		{
			Child = panel = new StackPanel()
		};

		panel.Children.AddRange(nonArrageableTargets);
		panel.Children.AddRange(targets);

		root.LayoutManager.ExecuteInitialLayoutPass();

		foreach (var c in panel.Children.OfType<LayoutTestControl>())
		{
			c.Measured = c.Arranged = false;
			c.InvalidateMeasure();
		}

		foreach (var c in nonArrageableTargets)
		{
			c.DoArrangeOverride = (l, s) =>
			{
				//emulate a problem in the logic of a control that triggers
				//invalidate measure during arrange
				c.InvalidateMeasure();
				return new Size(100, 100);
			};
		}

		root.LayoutManager.ExecuteLayoutPass();

		//although nonArrageableTargets has rubbish logic and can't be measured/arranged properly
		//layoutmanager should process properly other visuals
		CornerstoneTest.All(targets, c => CornerstoneTest.IsTrue(c.Arranged));
	}

	[PresentationTestMethod]
	public void LayoutManagerShouldRecoverFromInfiniteLoopOnMeasure()
	{
		// Test for issue #3041.
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = false;

		control.DoMeasureOverride = (l, s) =>
		{
			control.InvalidateMeasure();
			return new Size(100, 100);
		};

		control.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		// This is the important part: running a second layout pass in which we exceed the maximum
		// retries causes LayoutQueue<T>.Info.Count to exceed _maxEnqueueCountPerLoop.
		root.LayoutManager.ExecuteLayoutPass();

		control.Measured = false;
		control.DoMeasureOverride = null;

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(control.Measured);
		CornerstoneTest.IsTrue(control.IsMeasureValid);
	}

	[PresentationTestMethod]
	public void LaysOutDescendentsThatWereInvalidatedWhileAncestorWasNotVisible()
	{
		// Issue #11076
		var control = new LayoutTestControl();
		var parent = new Decorator { Child = control };
		var grandparent = new Decorator { Child = parent };
		var root = new LayoutTestRoot { Child = grandparent };

		root.LayoutManager.ExecuteInitialLayoutPass();

		grandparent.IsVisible = false;
		control.InvalidateMeasure();
		root.LayoutManager.ExecuteInitialLayoutPass();

		grandparent.IsVisible = true;

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(control.IsMeasureValid);
		CornerstoneTest.IsTrue(control.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void MeasuresAndArrangesInvalidateMeasuredControl()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot { Child = control };

		root.LayoutManager.ExecuteInitialLayoutPass();
		control.Measured = control.Arranged = false;

		control.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(control.Measured);
		CornerstoneTest.IsTrue(control.Arranged);
	}

	[PresentationTestMethod]
	public void MeasuresInCorrectOrder()
	{
		LayoutTestControl control1;
		LayoutTestControl control2;
		var root = new LayoutTestRoot
		{
			Child = control1 = new LayoutTestControl
			{
				Child = control2 = new LayoutTestControl()
			}
		};

		var order = new List<Layoutable>();

		Size MeasureOverride(Layoutable control, Size size)
		{
			order.Add(control);
			return new Size(10, 10);
		}

		root.DoMeasureOverride = MeasureOverride;
		control1.DoMeasureOverride = MeasureOverride;
		control2.DoMeasureOverride = MeasureOverride;
		root.LayoutManager.ExecuteInitialLayoutPass();

		control2.InvalidateMeasure();
		control1.InvalidateMeasure();
		root.InvalidateMeasure();

		order.Clear();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Layoutable[] { root, control1, control2 }, order);
	}

	[PresentationTestMethod]
	public void MeasuresParentOfNewlyAddedControl()
	{
		var control = new LayoutTestControl();
		var root = new LayoutTestRoot();

		root.LayoutManager.ExecuteInitialLayoutPass();
		root.Child = control;
		root.Measured = root.Arranged = false;

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(root.Measured);
		CornerstoneTest.IsTrue(root.Arranged);
		CornerstoneTest.IsTrue(control.Measured);
		CornerstoneTest.IsTrue(control.Arranged);
	}

	[PresentationTestMethod]
	public void MeasuresRootAndGrandparentInCorrectOrder()
	{
		LayoutTestControl control1;
		LayoutTestControl control2;
		var root = new LayoutTestRoot
		{
			Child = control1 = new LayoutTestControl
			{
				Child = control2 = new LayoutTestControl()
			}
		};

		var order = new List<Layoutable>();

		Size MeasureOverride(Layoutable control, Size size)
		{
			order.Add(control);
			return new Size(10, 10);
		}

		root.DoMeasureOverride = MeasureOverride;
		control1.DoMeasureOverride = MeasureOverride;
		control2.DoMeasureOverride = MeasureOverride;
		root.LayoutManager.ExecuteInitialLayoutPass();

		control2.InvalidateMeasure();
		root.InvalidateMeasure();

		order.Clear();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Layoutable[] { root, control2 }, order);
	}

	[PresentationTestMethod]
	public void MeasuresRootWithInfinity()
	{
		var root = new LayoutTestRoot();
		var availableSize = default(Size);

		root.DoMeasureOverride = (_, s) =>
		{
			availableSize = s;
			return new Size(100, 100);
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(Size.Infinity, availableSize);
	}

	#endregion
}