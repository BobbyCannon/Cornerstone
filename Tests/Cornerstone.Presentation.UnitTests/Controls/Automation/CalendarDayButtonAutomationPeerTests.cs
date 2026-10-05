#region References

using System;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class CalendarDayButtonAutomationPeerTests : ScopedTestBase
{
	#region Fields

	private static readonly DateTime Date1 = new(2026, 6, 5);
	private static readonly DateTime Date2 = new(2026, 6, 10);

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddToSelectionAppendsInMultipleRangeMode()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
		calendar.SelectedDates.Add(Date2);

		provider.AddToSelection();

		CornerstoneTest.AreEqual(2, calendar.SelectedDates.Count);
		CornerstoneTest.Contains(calendar.SelectedDates, Date1);
		CornerstoneTest.Contains(calendar.SelectedDates, Date2);
	}

	[PresentationTestMethod]
	public void AddToSelectionIsNoOpForBlackoutDay()
	{
		var (calendar, dayButton, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
		dayButton.IsBlackout = true;

		provider.AddToSelection();

		CornerstoneTest.Empty(calendar.SelectedDates);
	}

	[PresentationTestMethod]
	public void AddToSelectionIsNoOpWhenSelectionModeNone()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

		provider.AddToSelection();

		CornerstoneTest.Empty(calendar.SelectedDates);
	}

	[PresentationTestMethod]
	public void AddToSelectionReplacesSelectionInSingleDateMode()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
		calendar.SelectedDate = Date2;

		provider.AddToSelection();

		CornerstoneTest.AreEqual(Date1, calendar.SelectedDate);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
	}

	[PresentationTestMethod]
	public void CreatesCalendarDayButtonAutomationPeer()
	{
		var peer = ControlAutomationPeer.CreatePeerForElement(new CalendarDayButton());

		CornerstoneTest.IsType<CalendarDayButtonAutomationPeer>(peer);
		CornerstoneTest.IsAssignableFrom<ISelectionItemProvider>(peer);
		CornerstoneTest.IsAssignableFrom<IInvokeProvider>(peer);
	}

	[PresentationTestMethod]
	public void IsSelectedReflectsOwnerState()
	{
		var (_, dayButton, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

		CornerstoneTest.IsFalse(provider.IsSelected);
		dayButton.IsSelected = true;
		CornerstoneTest.IsTrue(provider.IsSelected);
	}

	[PresentationTestMethod]
	public void RemoveFromSelectionIsNoOpWhenSelectionModeNone()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

		provider.RemoveFromSelection();

		CornerstoneTest.Empty(calendar.SelectedDates);
	}

	[PresentationTestMethod]
	public void RemoveFromSelectionRemovesDate()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
		calendar.SelectedDates.Add(Date1);
		calendar.SelectedDates.Add(Date2);

		provider.RemoveFromSelection();

		CornerstoneTest.AreEqual(Date2, CornerstoneTest.Single(calendar.SelectedDates));
	}

	[PresentationTestMethod]
	public void SelectIsNoOpForBlackoutDay()
	{
		var (calendar, dayButton, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
		dayButton.IsBlackout = true;

		provider.Select();

		CornerstoneTest.IsNull(calendar.SelectedDate);
	}

	[PresentationTestMethod]
	public void SelectIsNoOpWhenSelectionModeNone()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

		provider.Select();

		CornerstoneTest.IsNull(calendar.SelectedDate);
		CornerstoneTest.Empty(calendar.SelectedDates);
	}

	[PresentationTestMethod]
	public void SelectReplacesExistingSelectionInSingleDateMode()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
		calendar.SelectedDate = Date2;

		provider.Select();

		CornerstoneTest.AreEqual(Date1, calendar.SelectedDate);
		CornerstoneTest.AreEqual(1, calendar.SelectedDates.Count);
	}

	[PresentationTestMethod]
	public void SelectSetsSelectedDate()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

		provider.Select();

		CornerstoneTest.AreEqual(Date1, calendar.SelectedDate);
	}

	[PresentationTestMethod]
	public void SelectionContainerIsCalendarPeer()
	{
		var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

		var container = provider.SelectionContainer;

		CornerstoneTest.IsNotNull(container);
		CornerstoneTest.Same(ControlAutomationPeer.CreatePeerForElement(calendar), container);
	}

	private static (Calendar, CalendarDayButton, ISelectionItemProvider) CreateTarget(
		CalendarSelectionMode mode, DateTime? date = null)
	{
		var calendar = new Calendar { SelectionMode = mode };
		var dayButton = new CalendarDayButton { Owner = calendar, DataContext = date ?? Date1 };
		var peer = ControlAutomationPeer.CreatePeerForElement(dayButton);
		return (calendar, dayButton, CornerstoneTest.IsAssignableFrom<ISelectionItemProvider>(peer));
	}

	#endregion
}