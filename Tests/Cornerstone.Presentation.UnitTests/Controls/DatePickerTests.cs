#region References

using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DatePickerTests : ScopedTestBase
{
	#region Properties

	private static TestServices FocusServices =>
		TestServices.MockThreadingInterface.With(
			fontManagerImpl: new HeadlessFontManagerStub(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager());

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			fontManagerImpl: new HeadlessFontManagerStub(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			renderInterface: new HeadlessPlatformRenderInterface());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void DayVisibleFalseShouldHideDay()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = new DatePicker
			{
				Template = CreateTemplate(),
				DayVisible = false
			};
			datePicker.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var desc = datePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button
			TextBlock dayText = null;

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			for (var i = 0; i < container.Children.Count; i++)
			{
				if (container.Children[i] is TextBlock tb && (tb.Name == "PART_DayTextBlock"))
				{
					dayText = tb;
					break;
				}
			}

			CornerstoneTest.IsNotNull(dayText);
			CornerstoneTest.IsFalse(dayText.IsVisible);
			CornerstoneTest.AreEqual(3, container.ColumnDefinitions.Count);
		}
	}

	[PresentationTestMethod]
	public void MonthVisibleFalseShouldHideMonth()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = new DatePicker
			{
				Template = CreateTemplate(),
				MonthVisible = false
			};
			datePicker.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var desc = datePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button
			TextBlock monthText = null;

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			for (var i = 0; i < container.Children.Count; i++)
			{
				if (container.Children[i] is TextBlock tb && (tb.Name == "PART_MonthTextBlock"))
				{
					monthText = tb;
					break;
				}
			}

			CornerstoneTest.IsNotNull(monthText);
			CornerstoneTest.IsFalse(monthText.IsVisible);
			CornerstoneTest.AreEqual(3, container.ColumnDefinitions.Count);
		}
	}

	[PresentationTestMethod]
	public void SelectedDateChangedShouldFireWhenSelectedDateSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var handled = false;
			var datePicker = new DatePicker();
			datePicker.SelectedDateChanged += (s, e) => { handled = true; };
			var value = new DateTimeOffset(2000, 10, 10, 0, 0, 0, TimeSpan.Zero);
			datePicker.SelectedDate = value;
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.IsTrue(handled);
		}
	}

	[PresentationTestMethod]
	public void SelectedDateEnableDataValidation()
	{
		var handled = false;
		var datePicker = new DatePicker();

		datePicker.SelectedDateChanged += (s, e) =>
		{
			var minDateTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
			var maxDateTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);

			if (e.NewDate < minDateTime)
			{
				throw new DataValidationException($"dateTime is less than {minDateTime}");
			}
			if (e.NewDate > maxDateTime)
			{
				throw new DataValidationException($"dateTime is over {maxDateTime}");
			}

			handled = true;
		};

		// dateTime is less than
		Assert.Throws<DataValidationException>(() => datePicker.SelectedDate = new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero));

		// dateTime is over
		Assert.Throws<DataValidationException>(() => datePicker.SelectedDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero));

		var exception = new DataValidationException("failed validation");
		var observable =
			new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
				BindingErrorType.DataValidationError));
		datePicker.Bind(DatePicker.SelectedDateProperty, observable);

		CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(datePicker));

		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
		datePicker.SelectedDate = new DateTimeOffset(2005, 5, 10, 11, 12, 13, TimeSpan.Zero);
		CornerstoneTest.IsTrue(handled);
	}

	[PresentationTestMethod]
	public void SelectedDatenullShouldUsePlaceholders()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = new DatePicker
			{
				Template = CreateTemplate(),
				YearVisible = false
			};
			datePicker.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var desc = datePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button
			TextBlock yearText = null;
			TextBlock monthText = null;
			TextBlock dayText = null;

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			for (var i = 0; i < container.Children.Count; i++)
			{
				if (container.Children[i] is TextBlock tb && (tb.Name == "PART_YearTextBlock"))
				{
					yearText = tb;
				}
				else if (container.Children[i] is TextBlock tb1 && (tb1.Name == "PART_MonthTextBlock"))
				{
					monthText = tb1;
				}
				else if (container.Children[i] is TextBlock tb2 && (tb2.Name == "PART_DayTextBlock"))
				{
					dayText = tb2;
				}
			}

			CornerstoneTest.IsNotNull(dayText);
			CornerstoneTest.IsNotNull(monthText);
			CornerstoneTest.IsNotNull(yearText);

			var value = new DateTimeOffset(2000, 10, 10, 0, 0, 0, TimeSpan.Zero);
			datePicker.SelectedDate = value;

			CornerstoneTest.IsNotNull(dayText.Text);
			CornerstoneTest.IsNotNull(monthText.Text);
			CornerstoneTest.IsNotNull(yearText.Text);
			CornerstoneTest.IsFalse(datePicker.Classes.Contains(":hasnodate"));

			datePicker.SelectedDate = null;

			CornerstoneTest.IsNull(dayText.Text);
			CornerstoneTest.IsNull(monthText.Text);
			CornerstoneTest.IsNull(yearText.Text);
			CornerstoneTest.IsTrue(datePicker.Classes.Contains(":hasnodate"));
		}
	}

	[PresentationTestMethod]
	[DataRow("PART_DaySelector")]
	[DataRow("PART_MonthSelector")]
	[DataRow("PART_YearSelector")]
	public void SelectorScrollDownShouldWork(string selectorName)
	{
		TestSelectorScrolling(selectorName, panel => panel.ScrollDown());
	}

	[PresentationTestMethod]
	[DataRow("PART_DaySelector")]
	[DataRow("PART_MonthSelector")]
	[DataRow("PART_YearSelector")]
	public void SelectorScrollUpShouldWork(string selectorName)
	{
		TestSelectorScrolling(selectorName, panel => panel.ScrollUp());
	}

	[PresentationTestMethod]
	public void SetInitialFocusShouldFocusDaySelectorForDayFirstLocale()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			// en-GB uses dd/MM/yyyy — day appears first in the short date pattern
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

			using (UnitTestApplication.Start(FocusServices))
			{
				var presenter = new DatePickerPresenter { Template = CreatePickerTemplate() };
				var root = new TestRoot(presenter);
				root.LayoutManager.ExecuteInitialLayoutPass();

				// Trigger InitPicker again now that the visual tree is fully connected,
				// so SetInitialFocus can successfully call Focus().
				presenter.Date = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

				var daySelector = presenter
					.GetVisualDescendants()
					.OfType<DateTimePickerPanel>()
					.First(p => p.Name == "PART_DaySelector");

				CornerstoneTest.Same(daySelector, root.FocusManager.GetFocusedElement());
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[PresentationTestMethod]
	public void VerticalContentAlignmentDefaultIsStretch()
	{
		var datePicker = new DatePicker();
		CornerstoneTest.AreEqual(VerticalAlignment.Stretch, datePicker.VerticalContentAlignment);
	}

	[PresentationTestMethod]
	[DataRow(VerticalAlignment.Top)]
	[DataRow(VerticalAlignment.Center)]
	[DataRow(VerticalAlignment.Bottom)]
	[DataRow(VerticalAlignment.Stretch)]
	public void VerticalContentAlignmentRoundTrips(VerticalAlignment value)
	{
		var datePicker = new DatePicker { VerticalContentAlignment = value };
		CornerstoneTest.AreEqual(value, datePicker.VerticalContentAlignment);
	}

	[PresentationTestMethod]
	public void YearVisibleFalseShouldHideYear()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = new DatePicker
			{
				Template = CreateTemplate(),
				YearVisible = false
			};
			datePicker.ApplyTemplate();
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var desc = datePicker.GetVisualDescendants();
			CornerstoneTest.IsTrue(desc.Count() > 1); //Should be layoutroot grid & button
			TextBlock yearText = null;

			var button = CornerstoneTest.IsAssignableFrom<Button>(desc.ElementAt(1));
			var container = CornerstoneTest.IsAssignableFrom<Grid>(button.Content);

			for (var i = 0; i < container.Children.Count; i++)
			{
				if (container.Children[i] is TextBlock tb && (tb.Name == "PART_YearTextBlock"))
				{
					yearText = tb;
					break;
				}
			}

			CornerstoneTest.IsNotNull(yearText);
			CornerstoneTest.IsFalse(yearText.IsVisible);
			CornerstoneTest.AreEqual(3, container.ColumnDefinitions.Count);
		}
	}

	private static IControlTemplate CreatePickerTemplate()
	{
		return new FuncControlTemplate((_, scope) =>
		{
			var dayHost = new Panel
			{
				Name = "PART_DayHost"
			}.RegisterInNameScope(scope);

			var daySelector = new DateTimePickerPanel
			{
				Name = "PART_DaySelector",
				PanelType = DateTimePickerPanelType.Day,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var monthHost = new Panel
			{
				Name = "PART_MonthHost"
			}.RegisterInNameScope(scope);

			var monthSelector = new DateTimePickerPanel
			{
				Name = "PART_MonthSelector",
				PanelType = DateTimePickerPanelType.Month,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var yearHost = new Panel
			{
				Name = "PART_YearHost"
			}.RegisterInNameScope(scope);

			var yearSelector = new DateTimePickerPanel
			{
				Name = "PART_YearSelector",
				PanelType = DateTimePickerPanelType.Year,
				ShouldLoop = true
			}.RegisterInNameScope(scope);

			var acceptButton = new Button
			{
				Name = "PART_AcceptButton"
			}.RegisterInNameScope(scope);

			var pickerContainer = new Grid
			{
				Name = "PART_PickerContainer"
			}.RegisterInNameScope(scope);

			var firstSpacer = new Rectangle
			{
				Name = "PART_FirstSpacer"
			}.RegisterInNameScope(scope);

			var secondSpacer = new Rectangle
			{
				Name = "PART_SecondSpacer"
			}.RegisterInNameScope(scope);

			var contentPanel = new Panel();
			contentPanel.Children.AddRange([
				dayHost, daySelector, monthHost, monthSelector, yearHost, yearSelector,
				acceptButton, pickerContainer, firstSpacer, secondSpacer
			]);
			return contentPanel;
		});
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate((control, scope) =>
		{
			var layoutRoot = new Grid
			{
				Name = "LayoutRoot"
			}.RegisterInNameScope(scope);

			//Skip contentpresenter
			var flyoutButton = new Button
			{
				Name = "PART_FlyoutButton"
			}.RegisterInNameScope(scope);
			var contentGrid = new Grid
			{
				Name = "PART_ButtonContentGrid"
			}.RegisterInNameScope(scope);
			var dayText = new TextBlock
			{
				Name = "PART_DayTextBlock"
			}.RegisterInNameScope(scope);
			var monthText = new TextBlock
			{
				Name = "PART_MonthTextBlock"
			}.RegisterInNameScope(scope);
			var yearText = new TextBlock
			{
				Name = "PART_YearTextBlock"
			}.RegisterInNameScope(scope);
			var firstSpacer = new Rectangle
			{
				Name = "PART_FirstSpacer"
			}.RegisterInNameScope(scope);
			var secondSpacer = new Rectangle
			{
				Name = "PART_SecondSpacer"
			}.RegisterInNameScope(scope);
			var thirdSpacer = new Rectangle
			{
				Name = "PART_ThirdSpacer"
			}.RegisterInNameScope(scope);

			contentGrid.Children.AddRange(new Control[] { dayText, monthText, yearText, firstSpacer, secondSpacer, thirdSpacer });
			flyoutButton.Content = contentGrid;
			layoutRoot.Children.Add(flyoutButton);
			return layoutRoot;
		});
	}

	private static void TestSelectorScrolling(string selectorName, Action<DateTimePickerPanel> scroll)
	{
		using var app = UnitTestApplication.Start(Services);

		var presenter = new DatePickerPresenter { Template = CreatePickerTemplate() };
		presenter.ApplyTemplate();
		presenter.Measure(new Size(1000, 1000));

		var panel = presenter
			.GetVisualDescendants()
			.OfType<DateTimePickerPanel>()
			.FirstOrDefault(panel => panel.Name == selectorName);

		CornerstoneTest.IsNotNull(panel);

		var previousOffset = panel.Offset;
		scroll(panel);
		CornerstoneTest.AreNotEqual(previousOffset, panel.Offset);
	}

	#endregion
}