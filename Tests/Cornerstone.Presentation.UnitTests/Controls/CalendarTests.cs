#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calendar = Cornerstone.Presentation.Controls.Calendar;
using Pointer = Cornerstone.Presentation.Input.Pointer;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CalendarTests : ScopedTestBase
{
	#region Fields

	/// <summary>
	/// The days added to the SelectedDates collection.
	/// </summary>
	private IList<object> _selectedDatesChangedAddedDays;

	/// <summary>
	/// The number of times the SelectedDatesChanged event has been fired.
	/// </summary>
	private int _selectedDatesChangedCount;

	/// <summary>
	/// The days removed from the SelectedDates collection.
	/// </summary>
	private IList<object> _selectedDatesChangedRemovedDays;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingBlackoutDatesContainingSelectedDateShouldThrow()
	{
		var calendar = new Calendar();
		calendar.SelectedDate = DateTime.Today.AddDays(5);

		CornerstoneTest.Throws<ArgumentOutOfRangeException>(() => calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10))));
	}

	[PresentationTestMethod]
	public void AllowTapRangeSelectionShouldDisableTapToSelectRange()
	{
		var calendar = new Calendar();
		CornerstoneTest.IsTrue(calendar.AllowTapRangeSelection); // Default should be true

		calendar.AllowTapRangeSelection = false;
		CornerstoneTest.IsFalse(calendar.AllowTapRangeSelection);
	}

	[PresentationTestMethod]
	public void CalendarItemShouldResetMouseDownFlagOnDetachFromVisualTree()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;

		var calendarItem = new CalendarItem();
		calendarItem.Owner = calendar;

		// Attach CalendarItem to a visual tree
		var root = new TestRoot(calendarItem);

		// Create a day button and simulate mouse left button down,
		// which sets the internal _isMouseLeftButtonDown flag to true.
		var date1 = new DateTime(2024, 1, 15);
		var dayButton1 = new CalendarDayButton { DataContext = date1 };

		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		var props = new PointerPointProperties(RawInputModifiers.LeftMouseButton,
			PointerUpdateKind.LeftButtonPressed);
		var pressArgs = new PointerPressedEventArgs(dayButton1, pointer, root,
			default, 0, props, KeyModifiers.None);

		calendarItem.Cell_MouseLeftButtonDown(dayButton1, pressArgs);

		// date1 should now be selected
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
		CornerstoneTest.AreEqual(date1, calendar.SelectedDates[0]);

		// Detach CalendarItem from visual tree (simulates popup closing
		// during date selection without a PointerReleased event).
		root.Child = null;

		// Create a different day button and simulate mouse enter.
		// Before the fix, _isMouseLeftButtonDown would still be true,
		// causing hover to auto-select dates.
		var date2 = new DateTime(2024, 1, 20);
		var dayButton2 = new CalendarDayButton { DataContext = date2 };

		calendarItem.Cell_MouseEntered(dayButton2, null!);

		// The selected date should NOT have changed to date2,
		// because the mouse-down flag was reset when detaching.
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
		CornerstoneTest.AreEqual(date1, calendar.SelectedDates[0]);
	}

	[PresentationTestMethod]
	public void CalendarItemShouldResetYearViewMouseDownFlagOnDetachFromVisualTree()
	{
		var calendar = new Calendar();
		var calendarItem = new CalendarItem();
		calendarItem.Owner = calendar;

		// Attach CalendarItem to a visual tree
		var root = new TestRoot(calendarItem);

		// Use reflection to set the _isMouseLeftButtonDownYearView flag,
		// since Month_CalendarButtonMouseDown is private.
		var field = typeof(CalendarItem).GetField("_isMouseLeftButtonDownYearView",
			BindingFlags.NonPublic | BindingFlags.Instance);
		CornerstoneTest.IsNotNull(field);
		field!.SetValue(calendarItem, true);
		CornerstoneTest.IsTrue((bool) field.GetValue(calendarItem)!);

		// Detach CalendarItem from visual tree
		root.Child = null;

		// Verify the flag was reset
		CornerstoneTest.IsFalse((bool) field.GetValue(calendarItem)!);
	}

	// ------------------------------------------------------------------------
	//  Property change refresh tests – ensure the control updates its
	//  visual state when week‑number related properties are changed.
	// ------------------------------------------------------------------------
	[PresentationTestMethod]
	public void ChangingIsWeekNumberVisibleTogglesHasWeekNumbersPseudoClass()
	{
		var calendar = CreateTestCalendar();

		calendar.ApplyTemplate();
		calendar.DisplayMode = CalendarMode.Month;
		calendar.IsWeekNumberVisible = false;

		// Grab the CalendarItem from the visual tree
		var calendarItem = calendar.GetVisualDescendants()
			.OfType<CalendarItem>()
			.FirstOrDefault();
		CornerstoneTest.IsNotNull(calendarItem);

		calendarItem.ApplyTemplate();

		// Assert pseudo-class is NOT set
		CornerstoneTest.IsFalse(calendarItem.Classes.Contains(":hasweeknumbers"));

		// Turn on week numbers
		calendar.IsWeekNumberVisible = true;

		// Assert pseudo-class IS set
		CornerstoneTest.IsTrue(calendarItem.Classes.Contains(":hasweeknumbers"));
	}

	[PresentationTestMethod]
	public void ChangingWeekNumberRuleRefreshesWeekNumberLabels()
	{
		var calendar = CreateTestCalendar();

		// Apply template so that the internal CalendarItem is created
		calendar.ApplyTemplate();

		calendar.DisplayMode = CalendarMode.Month;
		calendar.IsWeekNumberVisible = true;

		// Use ISO‑rule (FirstFourDayWeek) for the test
		calendar.WeekNumberRule = CalendarWeekRule.FirstFourDayWeek;
		calendar.FirstDayOfWeek = DayOfWeek.Monday;
		calendar.DisplayDate = new DateTime(2021, 01, 04); // First Monday of 2021

		// Grab the CalendarItem from the visual tree
		var calendarItem = calendar.GetVisualDescendants()
			.OfType<CalendarItem>()
			.FirstOrDefault();
		CornerstoneTest.IsNotNull(calendarItem);

		calendarItem.ApplyTemplate();

		// The first week‑number label should display “1” for the first week of 2021
		var weekLabelsGrid = calendarItem.GetVisualDescendants()
			.OfType<Grid>()
			.FirstOrDefault(x => x.Name == "PART_ElementWeekNumberLabels");
		CornerstoneTest.IsNotNull(weekLabelsGrid);
		var firstLabel = weekLabelsGrid.Children.OfType<ContentControl>().First(x => Grid.GetRow(x) == 2);
		CornerstoneTest.AreEqual(1, firstLabel.Content);

		// Change the rule to use FirstDay (which for 2021‑01‑04 would be week 2)
		calendar.WeekNumberRule = CalendarWeekRule.FirstDay;

		// Force a layout pass – in unit‑tests this is enough to trigger the update
		calendar.InvalidateMeasure();
		calendar.UpdateLayout();

		// After the rule change the first visible week number should now be “2”
		firstLabel = weekLabelsGrid.Children.OfType<ContentControl>().First(x => Grid.GetRow(x) == 2);
		CornerstoneTest.AreEqual(2, firstLabel.Content);
	}

	[PresentationTestMethod]
	public void DisplayDateChangedShouldFireWhenDisplayDateSet()
	{
		var handled = false;
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;
		calendar.DisplayDateChanged += delegate { handled = true; };
		var value = new DateTime(2000, 10, 10);
		calendar.DisplayDate = value;
		CornerstoneTest.IsTrue(handled);
	}

	[PresentationTestMethod]
	public void DisplayDateRangeEndWillContainSelectedDate()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;

		calendar.SelectedDate = DateTime.MaxValue;
		CornerstoneTest.IsTrue(CompareDates((DateTime) calendar.SelectedDate, DateTime.MaxValue));

		calendar.DisplayDateEnd = DateTime.MaxValue.AddDays(-1);
		CornerstoneTest.IsTrue(CompareDates((DateTime) calendar.DisplayDateEnd, DateTime.MaxValue));
	}

	[PresentationTestMethod]
	public void DisplayDateStartEndShouldConstrainDisplayDate()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;
		calendar.DisplayDateStart = new DateTime(2005, 12, 30);

		var value = new DateTime(2005, 12, 15);
		calendar.DisplayDate = value;
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDate, calendar.DisplayDateStart.Value));

		value = new DateTime(2005, 12, 30);
		calendar.DisplayDate = value;
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDate, value));

		value = DateTime.MaxValue;
		calendar.DisplayDate = value;
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDate, value));

		calendar.DisplayDateEnd = new DateTime(2010, 12, 30);
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDate, calendar.DisplayDateEnd.Value));
	}

	// ------------------------------------------------------------------------
	//  ISO‑week fallback tests – verify that GetWeekOfYear returns the expected
	//  week numbers for edge‑case dates.
	// ------------------------------------------------------------------------
	[PresentationTestMethod]

	// 31 Dec 2018 is part of ISO‑week 1 of 2019
	[DataRow(2018, 12, 31, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]

	// 1 Jan 2020 is also ISO‑week 1 (Monday)
	[DataRow(2020, 01, 01, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]

	// 29 Dec 2014 (Monday) should be week 1 of 2015 (ISO)
	[DataRow(2014, 12, 29, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]

	// 30 Dec 2019 (Monday) is week 1 of 2020 (ISO)
	[DataRow(2019, 12, 30, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]
	public void GetWeekOfYearIsoWeekFallbackReturnsCorrectWeek(
		int year, int month, int day,
		CalendarWeekRule rule,
		DayOfWeek firstDay,
		int expectedWeek)
	{
		var _calendar = new GregorianCalendar();
		var date = new DateTime(year, month, day);

		// DateTimeHelper is the internal helper used by Calendar.
		// It falls back to ISO‑week calculation when the culture’s
		// CalendarWeekRule does not produce a valid week.
		var week = DateTimeHelper.GetWeekOfYear(date, rule, firstDay, _calendar);
		CornerstoneTest.AreEqual(expectedWeek, week);
	}

	[PresentationTestMethod]

	// ISO 8601: week 1 of 2023 starts on Monday 2 Jan 2023
	[DataRow(2023, 1, 2, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]

	// 2022-12-31 is still in ISO week 52 of 2022
	[DataRow(2022, 12, 31, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 52)]

	// .NET bug: 2018-12-31 is a Monday and is ISO week 1 of 2019, not week 53 of 2018
	[DataRow(2018, 12, 31, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday, 1)]

	// US rule: week 1 always starts on Jan 1
	[DataRow(2023, 1, 1, CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 1)]
	[DataRow(2023, 12, 31, CalendarWeekRule.FirstDay, DayOfWeek.Sunday, 53)]
	public void GetWeekOfYearReturnsCorrectWeekNumber(
		int year, int month, int day,
		CalendarWeekRule rule,
		DayOfWeek firstDayOfWeek,
		int expectedWeek)
	{
		var _calendar = new GregorianCalendar();
		var date = new DateTime(year, month, day);
		var week = DateTimeHelper.GetWeekOfYear(date, rule, firstDayOfWeek, _calendar);
		CornerstoneTest.AreEqual(expectedWeek, week);
	}

	[PresentationTestMethod]
	public void IsWeekNumberVisibleCanBeSet()
	{
		var calendar = new Calendar();
		calendar.IsWeekNumberVisible = true;
		CornerstoneTest.IsTrue(calendar.IsWeekNumberVisible);
	}

	// --- Week number tests ---

	[PresentationTestMethod]
	public void IsWeekNumberVisibleDefaultsToFalse()
	{
		var calendar = new Calendar();
		CornerstoneTest.IsFalse(calendar.IsWeekNumberVisible);
	}

	[PresentationTestMethod]
	public void SelectedDatesChangedShouldFireWhenSelectedDateSet()
	{
		var handled = false;
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;
		calendar.SelectedDatesChanged += delegate { handled = true; };
		var value = new DateTime(2000, 10, 10);
		calendar.SelectedDate = value;
		CornerstoneTest.IsTrue(handled);
	}

	[PresentationTestMethod]
	public void SettingDisplayDateEndShouldAlterDispalyDateAndDisplayDateStart()
	{
		var calendar = new Calendar();
		var value = new DateTime(2000, 1, 30);

		calendar.DisplayDate = value;
		calendar.DisplayDateEnd = value;
		calendar.DisplayDateStart = value;
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDateStart.Value, value));
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDateEnd.Value, value));

		value = value.AddMonths(2);
		calendar.DisplayDateStart = value;
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDateStart.Value, value));
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDateEnd.Value, value));
		CornerstoneTest.IsTrue(CompareDates(calendar.DisplayDate, value));
	}

	[PresentationTestMethod]
	public void SettingSelectedDateToBlackoutDateShouldThrow()
	{
		var calendar = new Calendar();
		calendar.BlackoutDates.AddDatesInPast();

		CornerstoneTest.Throws<ArgumentOutOfRangeException>(() => calendar.SelectedDate = DateTime.Today.AddDays(-1));
	}

	[PresentationTestMethod]
	public void SettingSelectedDateToBlackoutDateShouldThrowRange()
	{
		var calendar = new Calendar();
		calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10)));

		calendar.SelectedDate = DateTime.Today.AddDays(-1);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(-1)));
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, calendar.SelectedDates[0]));

		calendar.SelectedDate = DateTime.Today.AddDays(11);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(11)));
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, calendar.SelectedDates[0]));

		CornerstoneTest.Throws<ArgumentOutOfRangeException>(() => calendar.SelectedDate = DateTime.Today.AddDays(5));
	}

	[PresentationTestMethod]
	public void SingleDateSelectionBehavior()
	{
		ResetSelectedDatesChanged();
		var calendar = new Calendar();
		calendar.SelectedDatesChanged += OnSelectedDatesChanged;
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;
		calendar.SelectedDate = DateTime.Today;
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 1);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDates[0], DateTime.Today));
		CornerstoneTest.IsTrue(_selectedDatesChangedCount == 1);
		CornerstoneTest.IsNotNull(_selectedDatesChangedAddedDays);
		CornerstoneTest.IsTrue(_selectedDatesChangedAddedDays.Count == 1);
		CornerstoneTest.IsNotNull(_selectedDatesChangedRemovedDays);
		CornerstoneTest.IsTrue(_selectedDatesChangedRemovedDays.Count == 0);
		ResetSelectedDatesChanged();

		calendar.SelectedDate = DateTime.Today;
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 1);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDates[0], DateTime.Today));
		CornerstoneTest.IsTrue(_selectedDatesChangedCount == 0);

		calendar.ClearValue(Calendar.SelectedDateProperty);

		calendar.SelectionMode = CalendarSelectionMode.None;
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 0);
		CornerstoneTest.IsNull(calendar.SelectedDate);

		calendar.SelectionMode = CalendarSelectionMode.SingleDate;

		calendar.SelectedDates.Add(DateTime.Today.AddDays(1));
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(1)));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 1);

		CornerstoneTest.Throws<InvalidOperationException>(() => calendar.SelectedDates.Add(DateTime.Today.AddDays(2)));
	}

	[PresentationTestMethod]
	public void SingleRangeSelectionBehavior()
	{
		ResetSelectedDatesChanged();
		var calendar = new Calendar();
		calendar.SelectedDatesChanged += OnSelectedDatesChanged;
		calendar.SelectionMode = CalendarSelectionMode.SingleRange;
		calendar.SelectedDate = DateTime.Today;
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 1);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDates[0], DateTime.Today));
		CornerstoneTest.IsTrue(_selectedDatesChangedCount == 1);
		CornerstoneTest.IsNotNull(_selectedDatesChangedAddedDays);
		CornerstoneTest.IsTrue(_selectedDatesChangedAddedDays.Count == 1);
		CornerstoneTest.IsNotNull(_selectedDatesChangedRemovedDays);
		CornerstoneTest.IsTrue(_selectedDatesChangedRemovedDays.Count == 0);
		ResetSelectedDatesChanged();

		calendar.SelectedDates.Clear();
		CornerstoneTest.IsNull(calendar.SelectedDate);
		ResetSelectedDatesChanged();

		calendar.SelectedDates.AddRange(DateTime.Today, DateTime.Today.AddDays(10));
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 11);
		ResetSelectedDatesChanged();

		calendar.SelectedDates.AddRange(DateTime.Today, DateTime.Today.AddDays(10));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 11);
		CornerstoneTest.IsTrue(_selectedDatesChangedCount == 0);
		ResetSelectedDatesChanged();

		calendar.SelectedDates.AddRange(DateTime.Today.AddDays(-20), DateTime.Today);
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(-20)));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 21);
		CornerstoneTest.IsTrue(_selectedDatesChangedCount == 1);
		CornerstoneTest.IsTrue(_selectedDatesChangedAddedDays.Count == 21);
		CornerstoneTest.IsTrue(_selectedDatesChangedRemovedDays.Count == 11);
		ResetSelectedDatesChanged();

		calendar.SelectedDates.Add(DateTime.Today.AddDays(100));
		CornerstoneTest.IsTrue(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(100)));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Count == 1);
	}

	[PresentationTestMethod]
	public void TapRangeSelectionShouldHandleBlackoutDates()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleRange;
		calendar.AllowTapRangeSelection = true;

		var startDate = new DateTime(2023, 10, 10);
		var blackoutDate = new DateTime(2023, 10, 12);
		var endDate = new DateTime(2023, 10, 15);

		// Add blackout date in the middle
		calendar.BlackoutDates.Add(new CalendarDateRange(blackoutDate, blackoutDate));

		// First tap
		calendar.ProcessTapRangeSelection(startDate);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);

		// Second tap should restart selection due to blackout date
		calendar.ProcessTapRangeSelection(endDate);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(endDate));
		CornerstoneTest.IsFalse(calendar.SelectedDates.Contains(startDate));
	}

	[PresentationTestMethod]
	public void TapRangeSelectionShouldHandleReverseOrderDates()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleRange;
		calendar.AllowTapRangeSelection = true;

		var laterDate = new DateTime(2023, 10, 15);
		var earlierDate = new DateTime(2023, 10, 10);

		// First tap on later date
		calendar.ProcessTapRangeSelection(laterDate);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);

		// Second tap on earlier date should still create correct range
		calendar.ProcessTapRangeSelection(earlierDate);
		CornerstoneTest.AreEqual(6, calendar.SelectedDates.Count);
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(earlierDate));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(laterDate));
	}

	[PresentationTestMethod]
	public void TapRangeSelectionShouldNotWorkInSingleDateMode()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleDate;
		calendar.AllowTapRangeSelection = true;

		var date = new DateTime(2023, 10, 10);
		var result = calendar.ProcessTapRangeSelection(date);
		CornerstoneTest.IsFalse(result); // Should not handle tap range selection
	}

	[PresentationTestMethod]
	public void TapRangeSelectionShouldWorkInSingleRangeMode()
	{
		var calendar = new Calendar();
		calendar.SelectionMode = CalendarSelectionMode.SingleRange;
		calendar.AllowTapRangeSelection = true;

		var startDate = new DateTime(2023, 10, 10);
		var endDate = new DateTime(2023, 10, 15);

		// First tap should select start date
		var firstTapResult = calendar.ProcessTapRangeSelection(startDate);
		CornerstoneTest.IsTrue(firstTapResult);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(startDate));

		// Second tap should complete the range
		var secondTapResult = calendar.ProcessTapRangeSelection(endDate);
		CornerstoneTest.IsTrue(secondTapResult);
		CornerstoneTest.AreEqual(6, calendar.SelectedDates.Count); // 5 days inclusive
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(startDate));
		CornerstoneTest.IsTrue(calendar.SelectedDates.Contains(endDate));
	}

	[PresentationTestMethod]
	public void WeekNumberRuleCanBeSet()
	{
		var calendar = new Calendar();
		calendar.WeekNumberRule = CalendarWeekRule.FirstFourDayWeek;
		CornerstoneTest.AreEqual(CalendarWeekRule.FirstFourDayWeek, calendar.WeekNumberRule);
	}

	[PresentationTestMethod]
	public void WeekNumberRuleDefaultsToCultureCalendarWeekRule()
	{
		var calendar = new Calendar();
		CornerstoneTest.IsType<CalendarWeekRule>(calendar.WeekNumberRule);
	}

	private static bool CompareDates(DateTime first, DateTime second)
	{
		return (first.Year == second.Year) &&
			(first.Month == second.Month) &&
			(first.Day == second.Day);
	}

	private static Calendar CreateTestCalendar()
	{
		var cal = new Calendar();
		var template = new FuncControlTemplate<Calendar>((c, scope) => new Panel
		{
			Name = "PART_Root",
			Children =
			{
				new CalendarItem
				{
					Name = "PART_CalendarItem",
					Owner = c,
					DayTitleTemplate = new FuncTemplate<Control>(() => new TextBlock()),
					Template = new FuncControlTemplate<CalendarItem>((_, itemScope) => new Grid
					{
						Children =
						{
							new Grid { Name = "PART_MonthView" }.RegisterInNameScope(itemScope),
							new Grid { Name = "PART_YearView" }.RegisterInNameScope(itemScope),
							new Grid { Name = "PART_ElementWeekNumberLabels" }.RegisterInNameScope(itemScope)
						}
					})
				}.RegisterInNameScope(scope)
			}
		}.RegisterInNameScope(scope));
		cal.Template = template;

		return cal;
	}

	/// <summary>
	/// Handle the SelectedDatesChanged event.
	/// </summary>
	/// <param name="sender"> The calendar. </param>
	/// <param name="e"> Event arguments. </param>
	private void OnSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectedDatesChangedAddedDays =
			e.AddedItems
				.Cast<object>()
				.ToList();
		_selectedDatesChangedRemovedDays =
			e.RemovedItems
				.Cast<object>()
				.ToList();
		_selectedDatesChangedCount++;
	}

	/// <summary>
	/// Clear the variables used to track the SelectedDatesChanged event.
	/// </summary>
	private void ResetSelectedDatesChanged()
	{
		if (_selectedDatesChangedAddedDays != null)
		{
			_selectedDatesChangedAddedDays.Clear();
		}

		if (_selectedDatesChangedRemovedDays != null)
		{
			_selectedDatesChangedRemovedDays.Clear();
		}

		_selectedDatesChangedCount = 0;
	}

	#endregion
}