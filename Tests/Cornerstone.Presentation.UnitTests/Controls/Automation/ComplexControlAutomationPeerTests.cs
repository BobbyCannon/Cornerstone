#region References

using System;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calendar = Cornerstone.Presentation.Controls.Calendar;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Automation;

[TestClass]
public class AutoCompleteBoxAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ControlTypeIsGroupAndClassNameIsAutoCompleteBox()
	{
		var target = new AutoCompleteBox();
		var peer = (AutoCompleteBoxAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType());
		CornerstoneTest.AreEqual(nameof(AutoCompleteBox), peer.GetClassName());
	}

	[PresentationTestMethod]
	public void CreatesAutoCompleteBoxAutomationPeer()
	{
		var target = new AutoCompleteBox();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsType<AutoCompleteBoxAutomationPeer>(peer);
	}

	[PresentationTestMethod]
	public void ExpandCollapseTracksIsDropDownOpen()
	{
		var target = new AutoCompleteBox
		{
			ItemsSource = new[] { "alpha" },
			Text = "a"
		};
		var peer = (IExpandCollapseProvider) ControlAutomationPeer.CreatePeerForElement(target);
		CornerstoneTest.IsTrue(peer.ShowsMenu);

		target.IsDropDownOpen = false;
		CornerstoneTest.IsFalse(target.IsDropDownOpen);

		peer.Expand();
		CornerstoneTest.IsTrue(target.IsDropDownOpen);
		CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

		peer.Collapse();
		CornerstoneTest.IsFalse(target.IsDropDownOpen);
		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
	}

	[PresentationTestMethod]
	public void ImplementsIExpandCollapseAndIValueProviders()
	{
		var target = new AutoCompleteBox();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsAssignableFrom<IExpandCollapseProvider>(peer);
		CornerstoneTest.IsAssignableFrom<IValueProvider>(peer);
		CornerstoneTest.IsFalse(peer is IInvokeProvider);
	}

	[PresentationTestMethod]
	public void PropertyChangeEventsRaiseForDropDownAndText()
	{
		var target = new AutoCompleteBox();
		var peer = (AutoCompleteBoxAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		AutomationPropertyChangedEventArgs expandCollapseChanged = null;
		AutomationPropertyChangedEventArgs valueChanged = null;
		peer.PropertyChanged += (_, e) =>
		{
			if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
			{
				expandCollapseChanged = e;
			}
			else if (e.Property == ValuePatternIdentifiers.ValueProperty)
			{
				valueChanged = e;
			}
		};

		target.IsDropDownOpen = true;
		CornerstoneTest.IsNotNull(expandCollapseChanged);
		CornerstoneTest.AreEqual(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, expandCollapseChanged!.Property);
		CornerstoneTest.AreEqual(ExpandCollapseState.Collapsed, expandCollapseChanged.OldValue);
		CornerstoneTest.AreEqual(ExpandCollapseState.Expanded, expandCollapseChanged.NewValue);

		target.Text = "query";
		CornerstoneTest.IsNotNull(valueChanged);
		CornerstoneTest.AreEqual(ValuePatternIdentifiers.ValueProperty, valueChanged!.Property);
		CornerstoneTest.AreEqual(string.Empty, valueChanged.OldValue);
		CornerstoneTest.AreEqual("query", valueChanged.NewValue);
	}

	[PresentationTestMethod]
	public void ValueProviderIsMutable()
	{
		var target = new AutoCompleteBox();
		var peer = (IValueProvider) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsFalse(peer.IsReadOnly);
	}

	[PresentationTestMethod]
	public void ValueTracksAndSetsText()
	{
		var target = new AutoCompleteBox { Text = "one" };
		var peer = (IValueProvider) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual("one", peer.Value);
		peer.SetValue("two");
		CornerstoneTest.AreEqual("two", target.Text);
		CornerstoneTest.AreEqual("two", peer.Value);
	}

	#endregion
}

[TestClass]
public class CalendarAutomationPeerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(CalendarSelectionMode.SingleDate, false)]
	[DataRow(CalendarSelectionMode.SingleRange, true)]
	[DataRow(CalendarSelectionMode.MultipleRange, true)]
	[DataRow(CalendarSelectionMode.None, false)]
	public void CanSelectMultipleReflectsSelectionMode(CalendarSelectionMode selectionMode, bool canSelectMultiple)
	{
		var target = new Calendar { SelectionMode = selectionMode };
		var peer = (ISelectionProvider) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(canSelectMultiple, peer.CanSelectMultiple);
		CornerstoneTest.IsFalse(peer.IsSelectionRequired);
	}

	[PresentationTestMethod]
	public void ControlTypeIsCalendarAndClassNameIsCalendar()
	{
		var target = new Calendar();
		var peer = (CalendarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.AreEqual(AutomationControlType.Calendar, peer.GetAutomationControlType());
		CornerstoneTest.AreEqual(nameof(Calendar), peer.GetClassName());
	}

	[PresentationTestMethod]
	public void CreatesCalendarAutomationPeer()
	{
		var target = new Calendar();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsType<CalendarAutomationPeer>(peer);
	}

	[PresentationTestMethod]
	public void GetSelectionReturnsEmptyWhenDayButtonNotRealized()
	{
		var target = new Calendar { SelectionMode = CalendarSelectionMode.SingleDate, SelectedDate = new DateTime(2026, 5, 7) };
		var peer = (CalendarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		var selection = peer.GetSelection();

		CornerstoneTest.Empty(selection);
	}

	[PresentationTestMethod]
	public void GetSelectionReturnsRealizedDayButtonPeers()
	{
		var selectedDate = new DateTime(2026, 5, 7);
		var target = new Calendar
		{
			SelectionMode = CalendarSelectionMode.SingleDate,
			DisplayDate = new DateTime(2026, 5, 1),
			SelectedDate = selectedDate
		};
		var monthView = new Grid();
		var calendarItem = new CalendarItem
		{
			Owner = target,
			MonthView = monthView
		};
		target.Root = new Panel { Children = { calendarItem } };

		for (var i = 0; i < Calendar.ColumnsPerMonth; i++)
		{
			monthView.Children.Add(new TextBlock());
		}

		for (var i = 0; i < ((Calendar.RowsPerMonth * Calendar.ColumnsPerMonth) - Calendar.ColumnsPerMonth); i++)
		{
			monthView.Children.Add(new CalendarDayButton
			{
				Owner = target,
				DataContext = i == 0 ? selectedDate : selectedDate.AddDays(i + 1)
			});
		}

		var peer = (CalendarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);
		var selection = peer.GetSelection();

		CornerstoneTest.Single(selection);
	}

	[PresentationTestMethod]
	public void ImplementsISelectionAndIValueProviders()
	{
		var target = new Calendar();
		var peer = ControlAutomationPeer.CreatePeerForElement(target);

		CornerstoneTest.IsAssignableFrom<ISelectionProvider>(peer);
		CornerstoneTest.IsAssignableFrom<IValueProvider>(peer);
	}

	[PresentationTestMethod]
	public void SelectionEventsIncludeSelectionAndValueProperties()
	{
		var target = new Calendar { SelectionMode = CalendarSelectionMode.SingleDate };
		var peer = (CalendarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

		AutomationPropertyChangedEventArgs selectionChanged = null;
		AutomationPropertyChangedEventArgs valueChanged = null;
		peer.PropertyChanged += (_, e) =>
		{
			if (e.Property == SelectionPatternIdentifiers.SelectionProperty)
			{
				selectionChanged = e;
			}
			else if (e.Property == ValuePatternIdentifiers.ValueProperty)
			{
				valueChanged = e;
			}
		};

		target.SelectedDate = new DateTime(2010, 1, 1);

		CornerstoneTest.IsNotNull(selectionChanged);
		CornerstoneTest.IsNotNull(valueChanged);
		CornerstoneTest.AreEqual(SelectionPatternIdentifiers.SelectionProperty, selectionChanged!.Property);
		CornerstoneTest.AreEqual(ValuePatternIdentifiers.ValueProperty, valueChanged!.Property);
	}

	[PresentationTestMethod]
	public void SetValueThrowsNotSupported()
	{
		var peer = (IValueProvider) ControlAutomationPeer.CreatePeerForElement(new Calendar());

		Assert.Throws<NotSupportedException>(() => peer.SetValue("2026-01-01"));
	}

	[PresentationTestMethod]
	public void ValueIsReadOnly()
	{
		var peer = (IValueProvider) ControlAutomationPeer.CreatePeerForElement(new Calendar());

		CornerstoneTest.IsTrue(peer.IsReadOnly);
	}

	[PresentationTestMethod]
	public void ValueJoinsSelectedDatesWithCurrentCulture()
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

			var selectedDates = new[] { new DateTime(2026, 5, 7), new DateTime(2026, 5, 8) };
			var target = new Calendar
			{
				SelectionMode = CalendarSelectionMode.MultipleRange
			};
			var peer = (CalendarAutomationPeer) ControlAutomationPeer.CreatePeerForElement(target);

			foreach (var date in selectedDates)
			{
				target.SelectedDates.Add(date);
			}

			CornerstoneTest.AreEqual(string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, selectedDates.Select(x => x.ToString(CultureInfo.CurrentCulture))), peer.Value);
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	#endregion
}