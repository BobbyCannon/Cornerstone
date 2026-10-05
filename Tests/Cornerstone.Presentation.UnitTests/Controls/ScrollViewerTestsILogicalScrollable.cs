#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ScrollViewerTestsILogicalScrollable : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ExtentOffsetAndViewportShouldBeReadFromILogicalScrollable()
	{
		var scrollable = new TestScrollable
		{
			Extent = new Size(100, 100),
			Offset = new Vector(50, 50),
			Viewport = new Size(25, 25)
		};

		var target = CreateTarget(scrollable);

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

	private static FuncControlTemplate CreateScrollViewerTemplate()
	{
		return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
			new Panel
			{
				Children =
				{
					new ScrollContentPresenter
					{
						Name = "PART_ContentPresenter"
					}.RegisterInNameScope(scope)
				}
			});
	}

	private static ControlTheme CreateScrollViewerTheme()
	{
		return new ControlTheme(typeof(ScrollViewer))
		{
			Setters =
			{
				new Setter(TreeView.TemplateProperty, CreateScrollViewerTemplate())
			}
		};
	}

	private static ScrollViewer CreateTarget(object content)
	{
		var result = new ScrollViewer
		{
			Content = content
		};

		var root = new TestRoot
		{
			Resources =
			{
				{ typeof(ScrollViewer), CreateScrollViewerTheme() }
			},
			Child = result
		};

		root.LayoutManager.ExecuteInitialLayoutPass();
		return result;
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
		public bool IsLogicalScrollEnabled { get; } = true;

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