#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class PipsPagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrowKeysAtBoundariesShouldNotChangeIndex()
	{
		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 0,
			Orientation = Orientation.Horizontal
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left });
		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);

		target.SelectedPageIndex = 4;
		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
		CornerstoneTest.AreEqual(4, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void ClampingLogicWorks()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;

		target.SelectedPageIndex = 10;
		CornerstoneTest.AreEqual(4, target.SelectedPageIndex);

		target.SelectedPageIndex = -5;
		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void CornerstoneThemeShouldForwardCustomButtonThemes()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var previousTheme = new ControlTheme(typeof(Button));
		var nextTheme = new ControlTheme(typeof(Button));
		var simpleTheme = new CornerstoneTheme();

		var target = new PipsPager
		{
			NumberOfPages = 5,
			PreviousButtonTheme = previousTheme,
			NextButtonTheme = nextTheme
		};

		var root = new TestRoot
		{
			Child = target,
			Styles =
			{
				simpleTheme
			}
		};

		CornerstoneTest.IsTrue(simpleTheme.TryGetResource(typeof(PipsPager), ThemeVariant.Default, out var theme));
		target.Theme = CornerstoneTest.IsType<ControlTheme>(theme);

		target.ApplyTemplate();
		root.LayoutManager.ExecuteInitialLayoutPass();

		var previousButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PreviousButton");
		var nextButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_NextButton");

		CornerstoneTest.Same(previousTheme, previousButton.Theme);
		CornerstoneTest.Same(nextTheme, nextButton.Theme);
	}

	[PresentationTestMethod]
	public void DecreasingNumberOfPagesShouldUpdatePips()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;

		target.NumberOfPages = 3;

		CornerstoneTest.AreEqual(3, target.TemplateSettings.Pips.Count);
	}

	[PresentationTestMethod]
	public void DecreasingNumberOfPagesShouldUpdateSelectedPageIndex()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;
		target.SelectedPageIndex = 4;

		target.NumberOfPages = 3;

		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void EndKeyShouldNavigateToLastPage()
	{
		var target = new PipsPager
		{
			NumberOfPages = 10,
			SelectedPageIndex = 3,
			Orientation = Orientation.Horizontal
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End });

		CornerstoneTest.AreEqual(9, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void HomeEndKeysShouldWorkInVerticalOrientation()
	{
		var target = new PipsPager
		{
			NumberOfPages = 10,
			SelectedPageIndex = 5,
			Orientation = Orientation.Vertical
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });
		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End });
		CornerstoneTest.AreEqual(9, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void HomeKeyShouldNavigateToFirstPage()
	{
		var target = new PipsPager
		{
			NumberOfPages = 10,
			SelectedPageIndex = 7,
			Orientation = Orientation.Horizontal
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });

		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void HorizontalKeyboardNavigationShouldWork()
	{
		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 1,
			Orientation = Orientation.Horizontal
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left });
		CornerstoneTest.AreEqual(1, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void KeyboardNavigationShouldWork()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 1,
			Orientation = Orientation.Horizontal
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		target.RaiseEvent(new KeyEventArgs { Key = Key.Right, RoutedEvent = InputElement.KeyDownEvent });
		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);

		target.RaiseEvent(new KeyEventArgs { Key = Key.Left, RoutedEvent = InputElement.KeyDownEvent });
		CornerstoneTest.AreEqual(1, target.SelectedPageIndex);

		target.Orientation = Orientation.Vertical;

		target.RaiseEvent(new KeyEventArgs { Key = Key.Down, RoutedEvent = InputElement.KeyDownEvent });
		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);

		target.RaiseEvent(new KeyEventArgs { Key = Key.Up, RoutedEvent = InputElement.KeyDownEvent });
		CornerstoneTest.AreEqual(1, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void ManualButtonVisibilityShouldBeRespected()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 5,
			IsPreviousButtonVisible = false,
			IsNextButtonVisible = false,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		CornerstoneTest.IsFalse(target.IsPreviousButtonVisible);
		CornerstoneTest.IsFalse(target.IsNextButtonVisible);

		target.IsPreviousButtonVisible = true;
		target.IsNextButtonVisible = true;
		CornerstoneTest.IsTrue(target.IsPreviousButtonVisible);
		CornerstoneTest.IsTrue(target.IsNextButtonVisible);
	}

	[PresentationTestMethod]
	public void NavigationButtonsIsEnabledShouldUpdate()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 3,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var prevButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PreviousButton");
		var nextButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_NextButton");

		target.SelectedPageIndex = 0;
		CornerstoneTest.IsFalse(prevButton.IsEnabled);
		CornerstoneTest.IsTrue(nextButton.IsEnabled);

		target.SelectedPageIndex = 1;
		CornerstoneTest.IsTrue(prevButton.IsEnabled);
		CornerstoneTest.IsTrue(nextButton.IsEnabled);

		target.SelectedPageIndex = 2;
		CornerstoneTest.IsTrue(prevButton.IsEnabled);
		CornerstoneTest.IsFalse(nextButton.IsEnabled);
	}

	[PresentationTestMethod]
	public void NegativeNumberOfPagesAfterHavingPagesShouldCoerce()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;
		CornerstoneTest.AreEqual(5, target.TemplateSettings.Pips.Count);

		target.NumberOfPages = -1;

		CornerstoneTest.AreEqual(0, target.NumberOfPages);
		CornerstoneTest.AreEqual(0, target.TemplateSettings.Pips.Count);
	}

	[PresentationTestMethod]
	public void NegativeNumberOfPagesShouldBeCoercedToZero()
	{
		var target = new PipsPager();

		target.NumberOfPages = -5;

		CornerstoneTest.AreEqual(0, target.NumberOfPages);
		CornerstoneTest.AreEqual(0, target.TemplateSettings.Pips.Count);
	}

	[PresentationTestMethod]
	public void NextButtonAtLastPageShouldNotChangeIndex()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 3,
			SelectedPageIndex = 2,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var nextButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_NextButton");
		nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void NextButtonShouldIncrementIndex()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 1,
			IsNextButtonVisible = true,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var nextButton = target.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_NextButton");
		CornerstoneTest.IsNotNull(nextButton);

		nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void NumberOfPagesReductionShouldClampSelectedPageIndex()
	{
		var target = new PipsPager();
		target.NumberOfPages = 10;
		target.SelectedPageIndex = 8;

		target.NumberOfPages = 5;
		CornerstoneTest.AreEqual(4, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void NumberOfPagesShouldUpdatePips()
	{
		var target = new PipsPager();

		target.NumberOfPages = 5;

		CornerstoneTest.AreEqual(5, target.TemplateSettings.Pips.Count);
		CornerstoneTest.AreEqual(1, target.TemplateSettings.Pips[0]);
		CornerstoneTest.AreEqual(5, target.TemplateSettings.Pips[4]);
	}

	[PresentationTestMethod]
	public void NumberOfPagesToZeroShouldClampSelectedPageIndex()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;
		target.SelectedPageIndex = 3;

		target.NumberOfPages = 0;

		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
		CornerstoneTest.AreEqual(0, target.TemplateSettings.Pips.Count);
	}

	[PresentationTestMethod]
	public void NumberOfPagesZeroShouldClampIndex()
	{
		var target = new PipsPager();
		target.NumberOfPages = 0;
		target.SelectedPageIndex = 5;

		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void OrientationPseudoClassesShouldBeSet()
	{
		var target = new PipsPager();

		target.Orientation = Orientation.Horizontal;
		CornerstoneTest.IsTrue(target.Classes.Contains(":horizontal"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":vertical"));

		target.Orientation = Orientation.Vertical;
		CornerstoneTest.IsFalse(target.Classes.Contains(":horizontal"));
		CornerstoneTest.IsTrue(target.Classes.Contains(":vertical"));
	}

	[PresentationTestMethod]
	public void PagePseudoClassesShouldBeSet()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;

		target.SelectedPageIndex = 0;
		CornerstoneTest.IsTrue(target.Classes.Contains(":first-page"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":last-page"));

		target.SelectedPageIndex = 2;
		CornerstoneTest.IsFalse(target.Classes.Contains(":first-page"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":last-page"));

		target.SelectedPageIndex = 4;
		CornerstoneTest.IsFalse(target.Classes.Contains(":first-page"));
		CornerstoneTest.IsTrue(target.Classes.Contains(":last-page"));
	}

	[PresentationTestMethod]
	public void PagerSizeShouldUpdateBasedOnOrientationAndMaxVisiblePips()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 10,
			MaxVisiblePips = 5,
			Orientation = Orientation.Horizontal,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var pipsList = target.GetVisualDescendants().OfType<ListBox>().First(i => i.Name == "PART_PipsPagerList");

		CornerstoneTest.AreEqual(60, pipsList.Width);

		target.Orientation = Orientation.Vertical;
		CornerstoneTest.AreEqual(60, pipsList.Height);
	}

	[PresentationTestMethod]
	public void PreselectedIndexShouldBePreservedAfterTemplateApply()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 20,
			MaxVisiblePips = 5,
			SelectedPageIndex = 15,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		CornerstoneTest.AreEqual(15, target.SelectedPageIndex);
		CornerstoneTest.IsTrue(!target.Classes.Contains(":last-page"));
		CornerstoneTest.IsTrue(!target.Classes.Contains(":first-page"));
	}

	[PresentationTestMethod]
	public void PreselectedLastIndexShouldSetLastPagePseudoClass()
	{
		var target = new PipsPager
		{
			NumberOfPages = 10,
			SelectedPageIndex = 9
		};

		CornerstoneTest.AreEqual(9, target.SelectedPageIndex);
		CornerstoneTest.IsTrue(target.Classes.Contains(":last-page"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":first-page"));
	}

	[PresentationTestMethod]
	public void PreviousButtonAtFirstPageShouldNotChangeIndex()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 3,
			SelectedPageIndex = 0,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var prevButton = target.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PreviousButton");
		prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void PreviousButtonShouldDecrementIndex()
	{
		using var unittestApplication = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 3,
			IsPreviousButtonVisible = true,
			Template = GetTemplate()
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var prevButton = target.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Name == "PART_PreviousButton");
		CornerstoneTest.IsNotNull(prevButton);

		prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void RapidPageChangesShouldMaintainIntegrity()
	{
		var target = new PipsPager { NumberOfPages = 100 };
		var list = new List<int>();
		target.SelectedIndexChanged += (s, e) => list.Add(e.NewIndex);

		for (var i = 1; i <= 50; i++)
		{
			target.SelectedPageIndex = i;
		}

		CornerstoneTest.AreEqual(50, list.Count);
		CornerstoneTest.AreEqual(50, target.SelectedPageIndex);
		CornerstoneTest.AreEqual(50, list.Last());
	}

	[PresentationTestMethod]
	public void SelectedIndexChangedEventShouldHaveCorrectArgs()
	{
		var target = new PipsPager { NumberOfPages = 5, SelectedPageIndex = 1 };
		var oldIdx = -1;
		var newIdx = -1;
		target.SelectedIndexChanged += (s, e) =>
		{
			oldIdx = e.OldIndex;
			newIdx = e.NewIndex;
		};

		target.SelectedPageIndex = 3;
		CornerstoneTest.AreEqual(1, oldIdx);
		CornerstoneTest.AreEqual(3, newIdx);
	}

	[PresentationTestMethod]
	public void SelectedPageIndexChangeShouldRaiseEvent()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;
		var raised = false;
		target.SelectedIndexChanged += (s, e) => raised = true;

		target.SelectedPageIndex = 2;

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void SelectedPageIndexDefaultBindingModeShouldBeTwoWay()
	{
		CornerstoneTest.AreEqual(BindingMode.TwoWay, PipsPager.SelectedPageIndexProperty.GetMetadata(typeof(PipsPager)).DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void SelectedPageIndexShouldBeClampedToZero()
	{
		var target = new PipsPager();
		target.NumberOfPages = 5;

		target.SelectedPageIndex = -1;

		CornerstoneTest.AreEqual(0, target.SelectedPageIndex);
	}

	[PresentationTestMethod]
	public void TemplateSettingsShouldNotBeExternallySettable()
	{
		var target = new PipsPager();

		// TemplateSettings property should have a private setter (compile-time enforcement).
		// Verify the property is readable and initialized.
		CornerstoneTest.IsNotNull(target.TemplateSettings);
		CornerstoneTest.IsType<PipsPagerTemplateSettings>(target.TemplateSettings);
	}

	[PresentationTestMethod]
	public void VerticalKeyboardNavigationShouldWork()
	{
		var target = new PipsPager
		{
			NumberOfPages = 5,
			SelectedPageIndex = 1,
			Orientation = Orientation.Vertical
		};

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
		CornerstoneTest.AreEqual(2, target.SelectedPageIndex);

		target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up });
		CornerstoneTest.AreEqual(1, target.SelectedPageIndex);
	}

	private static FuncControlTemplate<PipsPager> GetTemplate()
	{
		return new FuncControlTemplate<PipsPager>((parent, scope) =>
		{
			return new StackPanel
			{
				Children =
				{
					new Button { Name = "PART_PreviousButton" }.RegisterInNameScope(scope),
					new ListBox { Name = "PART_PipsPagerList" }.RegisterInNameScope(scope),
					new Button { Name = "PART_NextButton" }.RegisterInNameScope(scope)
				}
			};
		});
	}

	#endregion
}