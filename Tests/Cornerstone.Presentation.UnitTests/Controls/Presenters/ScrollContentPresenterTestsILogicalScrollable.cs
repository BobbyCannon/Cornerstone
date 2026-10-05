#region References

using System;
using System.Reactive.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public class ScrollContentPresenterTestsILogicalScrollable : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeShouldNotOffsetILogicalScrollableBounds()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50),
			Viewport = new Size(25, 25)
		};

		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), scrollable.Bounds);
	}

	[PresentationTestMethod]
	public void ArrangeShouldNotSetViewportAndExtentWithILogicalScrollable()
	{
		var target = new ScrollContentPresenter
		{
			Content = new TestScrollable()
		};

		var changed = false;

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => changed = true);
		target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => changed = true);

		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.IsFalse(changed);
	}

	[PresentationTestMethod]
	public void ArrangeShouldOffsetILogicalScrollableBoundsWhenLogicalScrollDisabled()
	{
		var scrollable = new TestScrollable
		{
			IsLogicalScrollEnabled = false
		};

		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = scrollable
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		target.Offset = new Vector(25, 25);

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(-25, -25, 150, 150), scrollable.Bounds);
	}

	[PresentationTestMethod]
	public void ChangingContentShouldUpdateState()
	{
		var logicalScrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50),
			Viewport = new Size(25, 25)
		};

		var nonLogicalScrollable = new TestScrollable
		{
			IsLogicalScrollEnabled = false
		};

		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = logicalScrollable
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(logicalScrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(logicalScrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(logicalScrollable.Viewport, target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), logicalScrollable.Bounds);

		target.Content = nonLogicalScrollable;
		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(150, 150), target.Extent);
		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
		CornerstoneTest.AreEqual(new Size(100, 100), target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 150, 150), nonLogicalScrollable.Bounds);

		target.Content = logicalScrollable;
		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(logicalScrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(logicalScrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(logicalScrollable.Viewport, target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), logicalScrollable.Bounds);
	}

	[PresentationTestMethod]
	public void ExtentOffsetAndViewportShouldBeReadFromILogicalScrollable()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50),
			Viewport = new Size(25, 25)
		};

		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();

		CornerstoneTest.AreEqual(scrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(scrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(scrollable.Viewport, target.Viewport);

		scrollable.Extent = new Size(200, 200);
		scrollable.Offset = new Vector(100, 100);
		scrollable.Viewport = new Size(50, 50);

		CornerstoneTest.AreEqual(scrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(scrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(scrollable.Viewport, target.Viewport);
	}

	[PresentationTestMethod]
	public void InvalidateScrollShouldBeClearedWhenRemovedFromContent()
	{
		var scrollable = new TestScrollable();
		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();
		target.Content = null;
		target.UpdateChild();

		CornerstoneTest.IsFalse(scrollable.HasScrollInvalidatedSubscriber);
	}

	[PresentationTestMethod]
	public void InvalidateScrollShouldBeSetWhenSetAsContent()
	{
		var scrollable = new TestScrollable();
		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();

		CornerstoneTest.IsTrue(scrollable.HasScrollInvalidatedSubscriber);
	}

	[PresentationTestMethod]
	public void MeasureShouldPassUnchangedBoundsToILogicalScrollable()
	{
		var scrollable = new TestScrollable();
		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(100, 100), scrollable.AvailableSize);
	}

	[PresentationTestMethod]
	public void OffsetShouldBeWrittenToILogicalScrollable()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50)
		};

		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.UpdateChild();
		target.Offset = new Vector(25, 25);

		CornerstoneTest.AreEqual(target.Offset, scrollable.Offset);
	}

	[PresentationTestMethod]
	public void OffsetShouldNotBeWrittenToILogicalScrollableAfterRemoval()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50)
		};

		var target = new ScrollContentPresenter
		{
			Content = scrollable
		};

		target.Content = null;
		target.Offset = new Vector(25, 25);

		CornerstoneTest.AreEqual(new Vector(50, 50), scrollable.Offset);
	}

	[PresentationTestMethod]
	public void ShouldSetILogicalScrolableCanHorizontallyScroll()
	{
		var logicalScrollable = new TestScrollable();
		var target = new ScrollContentPresenter { Content = logicalScrollable };

		target.UpdateChild();
		CornerstoneTest.IsFalse(logicalScrollable.CanHorizontallyScroll);
		target.CanHorizontallyScroll = true;
		CornerstoneTest.IsTrue(logicalScrollable.CanHorizontallyScroll);
	}

	[PresentationTestMethod]
	public void ShouldSetILogicalScrolableCanVerticallyScroll()
	{
		var logicalScrollable = new TestScrollable();
		var target = new ScrollContentPresenter { Content = logicalScrollable };

		target.UpdateChild();
		CornerstoneTest.IsFalse(logicalScrollable.CanVerticallyScroll);
		target.CanVerticallyScroll = true;
		CornerstoneTest.IsTrue(logicalScrollable.CanVerticallyScroll);
	}

	[PresentationTestMethod]
	public void TogglingIsLogicalScrollEnabledShouldUpdateState()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50),
			Viewport = new Size(25, 25)
		};

		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = scrollable
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(scrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(scrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(scrollable.Viewport, target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), scrollable.Bounds);

		scrollable.IsLogicalScrollEnabled = false;
		scrollable.RaiseScrollInvalidated(EventArgs.Empty);
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(150, 150), target.Extent);
		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
		CornerstoneTest.AreEqual(new Size(100, 100), target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 150, 150), scrollable.Bounds);

		scrollable.IsLogicalScrollEnabled = true;
		scrollable.RaiseScrollInvalidated(EventArgs.Empty);
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(scrollable.Extent, target.Extent);
		CornerstoneTest.AreEqual(scrollable.Offset, target.Offset);
		CornerstoneTest.AreEqual(scrollable.Viewport, target.Viewport);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), scrollable.Bounds);
	}

	#endregion

	#region Classes

	private class TestScrollable : Control, ILogicalScrollable
	{
		#region Fields

		private Size _extent;
		private Vector _offset;
		private EventHandler _scrollInvalidated;
		private Size _viewport;

		#endregion

		#region Properties

		public Size AvailableSize { get; private set; }

		public bool CanHorizontallyScroll { get; set; }
		public bool CanVerticallyScroll { get; set; }

		public Size Extent
		{
			get => _extent;
			set
			{
				_extent = value;
				_scrollInvalidated?.Invoke(this, EventArgs.Empty);
			}
		}

		public bool HasScrollInvalidatedSubscriber => _scrollInvalidated != null;
		public bool IsLogicalScrollEnabled { get; set; } = true;

		public Vector Offset
		{
			get => _offset;
			set
			{
				_offset = value;
				_scrollInvalidated?.Invoke(this, EventArgs.Empty);
			}
		}

		public Size PageScrollSize => new(double.PositiveInfinity, Viewport.Height);

		public Size ScrollSize => new(double.PositiveInfinity, 1);

		public Size Viewport
		{
			get => _viewport;
			set
			{
				_viewport = value;
				_scrollInvalidated?.Invoke(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Methods

		public bool BringIntoView(Control target, Rect targetRect)
		{
			throw new NotImplementedException();
		}

		public Control GetControlInDirection(NavigationDirection direction, Control from)
		{
			throw new NotImplementedException();
		}

		public void RaiseScrollInvalidated(EventArgs e)
		{
			_scrollInvalidated?.Invoke(this, e);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			AvailableSize = availableSize;
			return new Size(150, 150);
		}

		#endregion

		#region Events

		public event EventHandler ScrollInvalidated
		{
			add => _scrollInvalidated += value;
			remove => _scrollInvalidated -= value;
		}

		#endregion
	}

	#endregion
}