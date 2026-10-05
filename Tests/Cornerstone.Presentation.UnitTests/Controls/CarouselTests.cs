#region References

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CarouselTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanMoveForwardBackForward()
	{
		using var app = Start();
		var items = new[] { "foo", "bar" };
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		target.SelectedIndex = 1;
		Layout(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		target.SelectedIndex = 0;
		Layout(target);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.SelectedIndex = 1;
		Layout(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void CanMoveForwardBackForwardWithControlItems()
	{
		// Issue #11119
		using var app = Start();
		var items = new[] { new Canvas(), new Canvas() };
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		target.SelectedIndex = 1;
		Layout(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);

		target.SelectedIndex = 0;
		Layout(target);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.SelectedIndex = 1;
		target.PropertyChanged += (s, e) =>
		{
			if (e.Property == Carousel.SelectedIndexProperty)
			{
			}
		};
		Layout(target);

		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void DownArrowNavigatesToNextWithVerticalPageSlide()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			PageTransition = new PageSlide(TimeSpan.FromMilliseconds(100), PageSlide.SlideAxis.Vertical)
		};

		Prepare(target);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void EndNavigatesToLastItem()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" }
		};

		Prepare(target);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End });
		CornerstoneTest.AreEqual(2, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void FirstItemShouldBeSelectedByDefault()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[]
			{
				"Foo",
				"Bar"
			}
		};

		Prepare(target);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("Foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void HomeNavigatesToFirstItem()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			SelectedIndex = 2
		};

		Prepare(target);
		Layout(target);
		CornerstoneTest.AreEqual(2, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void LeftArrowWrapsWithWrapSelection()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			PageTransition = new PageSlide(TimeSpan.FromMilliseconds(100), PageSlide.SlideAxis.Horizontal),
			WrapSelection = true
		};

		Prepare(target);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left });
		CornerstoneTest.AreEqual(2, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void LogicalChildShouldBeSelectedItem()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[]
			{
				"Foo",
				"Bar"
			}
		};

		Prepare(target);

		CornerstoneTest.Single(target.GetRealizedContainers());

		var child = GetContainerTextBlock(target.GetRealizedContainers().Single());

		CornerstoneTest.AreEqual("Foo", child.Text);
	}

	[PresentationTestMethod]
	public void RightArrowNavigatesToNextWithHorizontalPageSlide()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			PageTransition = new PageSlide(TimeSpan.FromMilliseconds(100), PageSlide.SlideAxis.Horizontal)
		};

		Prepare(target);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
		CornerstoneTest.AreEqual(1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexChangesToNoneWhenItemsAssignedNull()
	{
		using var app = Start();
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		CornerstoneTest.AreEqual(1, target.GetRealizedContainers().Count());

		var child = GetContainerTextBlock(target.GetRealizedContainers().First());

		CornerstoneTest.AreEqual("Foo", child.Text);

		target.ItemsSource = null;
		Layout(target);

		CornerstoneTest.Empty(target.GetRealizedContainers());
		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
	}

	[PresentationTestMethod]
	public void SelectedIndexIsMaintainedCarouselCreatedWithNonZeroSelectedIndex()
	{
		using var app = Start();
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items,
			SelectedIndex = 2
		};

		Prepare(target);

		CornerstoneTest.AreEqual("FooBar", target.SelectedItem);

		var child = GetContainerTextBlock(target.GetRealizedContainers().LastOrDefault());

		CornerstoneTest.AreEqual("FooBar", child.Text);
	}

	[PresentationTestMethod]
	public void SelectedItemChangesToFirstItemIfSelectedItemIsRemovedFromMiddle()
	{
		using var app = Start();
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		target.SelectedIndex = 1;

		items.RemoveAt(1);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.AreEqual("Foo", target.SelectedItem);
	}

	[PresentationTestMethod]
	public void SelectedItemChangesToFirstItemWhenItemAdded()
	{
		using var app = Start();
		var items = new ObservableCollection<string>();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		CornerstoneTest.AreEqual(-1, target.SelectedIndex);
		CornerstoneTest.Empty(target.GetRealizedContainers());

		items.Add("Foo");
		Layout(target);

		CornerstoneTest.AreEqual(0, target.SelectedIndex);
		CornerstoneTest.Single(target.GetRealizedContainers());
	}

	[PresentationTestMethod]
	public void SelectedItemChangesToFirstItemWhenItemsPropertyChanges()
	{
		using var app = Start();
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		CornerstoneTest.Single(target.GetRealizedContainers());

		var child = GetContainerTextBlock(target.GetRealizedContainers().Single());

		CornerstoneTest.AreEqual("Foo", child.Text);

		var newItems = items.ToList();
		newItems.RemoveAt(0);
		Layout(target);

		target.ItemsSource = newItems;
		Layout(target);

		child = GetContainerTextBlock(target.GetRealizedContainers().Single());

		CornerstoneTest.AreEqual("Bar", child.Text);
	}

	[PresentationTestMethod]
	public void SelectedItemChangesToNextFirstItemWhenItemRemovedFromBegginingOfList()
	{
		using var app = Start();
		var items = new ObservableCollection<string>
		{
			"Foo",
			"Bar",
			"FooBar"
		};

		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = items
		};

		Prepare(target);

		var child = GetContainerTextBlock(target.GetRealizedContainers().First());

		CornerstoneTest.AreEqual("Foo", child.Text);

		items.RemoveAt(0);
		Layout(target);

		child = GetContainerTextBlock(target.GetRealizedContainers().First());

		CornerstoneTest.IsType<TextBlock>(child);
		CornerstoneTest.AreEqual("Bar", child.Text);
	}

	[PresentationTestMethod]
	public void SelectedItemValidation()
	{
		using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
		{
			var target = new Carousel
			{
				Template = CarouselTemplate()
			};

			Prepare(target);

			var exception = new InvalidCastException("failed validation");
			var textObservable =
				new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
					BindingErrorType.DataValidationError));
			target.Bind(ComboBox.SelectedItemProperty, textObservable);

			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
			CornerstoneTest.AreEqual([exception], DataValidationErrors.GetErrors(target));
		}
	}

	[PresentationTestMethod]
	public void ViewportFractionCoercesInvalidValuesToOne()
	{
		using var app = Start();
		var target = new Carousel();

		target.ViewportFraction = 0;
		CornerstoneTest.AreEqual(1d, target.ViewportFraction);

		target.ViewportFraction = double.NaN;
		CornerstoneTest.AreEqual(1d, target.ViewportFraction);
	}

	[PresentationTestMethod]
	public void ViewportFractionDefaultsToOne()
	{
		using var app = Start();
		var target = new Carousel();

		CornerstoneTest.AreEqual(1d, target.ViewportFraction);
	}

	[PresentationTestMethod]
	public void WrongAxisArrowIsIgnored()
	{
		using var app = Start();
		var target = new Carousel
		{
			Template = CarouselTemplate(),
			ItemsSource = new[] { "Foo", "Bar", "Baz" },
			PageTransition = new PageSlide(TimeSpan.FromMilliseconds(100), PageSlide.SlideAxis.Horizontal)
		};

		Prepare(target);
		CornerstoneTest.AreEqual(0, target.SelectedIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
		CornerstoneTest.AreEqual(0, target.SelectedIndex);
	}

	private static IControlTemplate CarouselTemplate()
	{
		return new FuncControlTemplate((c, ns) =>
			new ScrollViewer
			{
				Name = "PART_ScrollViewer",
				Template = ScrollViewerTemplate(),
				HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
				VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
				Content = new ItemsPresenter
				{
					Name = "PART_ItemsPresenter",
					[~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty]
				}.RegisterInNameScope(ns)
			}.RegisterInNameScope(ns));
	}

	private static TextBlock GetContainerTextBlock(object control)
	{
		var contentPresenter = CornerstoneTest.IsType<ContentPresenter>(control);
		return CornerstoneTest.IsType<TextBlock>(contentPresenter.Child);
	}

	private static void Layout(Carousel target)
	{
		target.GetLayoutManager()?.ExecuteLayoutPass();
	}

	private static void Prepare(Carousel target)
	{
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
	}

	private static FuncControlTemplate ScrollViewerTemplate()
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

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
	}

	#endregion

	#region Classes

	[TestClass]
	public class WrapSelectionTests : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void NextDoesNotLoopWhenWrapSelectionIsFalse()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var target = new Carousel
			{
				Template = CarouselTemplate(),
				ItemsSource = items,
				WrapSelection = false,
				SelectedIndex = 2
			};

			Prepare(target);

			target.Next();
			Layout(target);

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
		}

		[PresentationTestMethod]
		public void NextLoopsWhenWrapSelectionIsTrue()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var target = new Carousel
			{
				Template = CarouselTemplate(),
				ItemsSource = items,
				WrapSelection = true,
				SelectedIndex = 2
			};

			Prepare(target);

			target.Next();
			Layout(target);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}

		[PresentationTestMethod]
		public void PreviousDoesNotLoopWhenWrapSelectionIsFalse()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var target = new Carousel
			{
				Template = CarouselTemplate(),
				ItemsSource = items,
				WrapSelection = false,
				SelectedIndex = 0
			};

			Prepare(target);

			target.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(0, target.SelectedIndex);
		}

		[PresentationTestMethod]
		public void PreviousLoopsWhenWrapSelectionIsTrue()
		{
			using var app = Start();
			var items = new[] { "foo", "bar", "baz" };
			var target = new Carousel
			{
				Template = CarouselTemplate(),
				ItemsSource = items,
				WrapSelection = true,
				SelectedIndex = 0
			};

			Prepare(target);

			target.Previous();
			Layout(target);

			CornerstoneTest.AreEqual(2, target.SelectedIndex);
		}

		#endregion
	}

	#endregion
}