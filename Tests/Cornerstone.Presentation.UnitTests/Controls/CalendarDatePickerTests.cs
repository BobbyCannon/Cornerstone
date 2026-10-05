#region References

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Calendar = Cornerstone.Presentation.Controls.Calendar;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CalendarDatePickerTests : ScopedTestBase
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
			standardCursorFactory: new StubCursorFactory());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingBlackoutDatesContainingSelectedDateShouldThrow()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = CreateControl();
			datePicker.SelectedDate = DateTime.Today.AddDays(5);

			CornerstoneTest.Throws<ArgumentOutOfRangeException>(() => datePicker.BlackoutDates!.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10))));
		}
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundCanBeSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var control = CreateControl();
			control.PlaceholderText = "Select date";
			control.PlaceholderForeground = Brushes.Purple;

			CornerstoneTest.AreEqual(Brushes.Purple, control.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void ProgrammaticFocusShouldMoveFocusToTextBox()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var datePicker = new CalendarDatePicker { Template = CreateTemplate() };
			var root = new TestRoot(datePicker);
			root.LayoutManager.ExecuteInitialLayoutPass();

			datePicker.Focus();

			CornerstoneTest.Same(GetTextBox(datePicker), root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void SelectedDateChangedShouldFireWhenSelectedDateSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var handled = false;
			var datePicker = CreateControl();
			datePicker.SelectedDateChanged += (s, e) => { handled = true; };
			var value = new DateTime(2000, 10, 10);
			datePicker.SelectedDate = value;
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.IsTrue(handled);
		}
	}

	[PresentationTestMethod]
	public void SettingDateManuallyUsesTextConverter()
	{
		CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = CreateControl();
			datePicker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
			datePicker.CustomDateFormatString = "dd.MM.yyyy";
			datePicker.TextConverter = new CalendarDatePickerTextConverter();
			var tb = GetTextBox(datePicker);

			datePicker.SelectedDate = new DateTime(2024, 2, 13);

			// DateTimeToString called async so need to let that complete before testing value
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);
			CornerstoneTest.AreEqual("2024-02-13", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate!.Value, new DateTime(2024, 2, 13)));

			// null input results in empty string for text
			datePicker.SelectedDate = null;

			CornerstoneTest.AreEqual("", datePicker.Text);
			CornerstoneTest.IsNull(datePicker.SelectedDate);
		}
	}

	[PresentationTestMethod]
	public void SettingDateManuallyWithCustomDateFormatStringShouldBeAccepted()
	{
		CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = CreateControl();
			datePicker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
			datePicker.CustomDateFormatString = "dd.MM.yyyy";

			var tb = GetTextBox(datePicker);

			tb.Clear();
			RaiseTextEvent(tb, "17.10.2024");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

			CornerstoneTest.AreEqual("17.10.2024", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate!.Value, new DateTime(2024, 10, 17)));

			tb.Clear();
			RaiseTextEvent(tb, "12.10.2024");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

			CornerstoneTest.AreEqual("12.10.2024", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate.Value, new DateTime(2024, 10, 12)));
		}
	}

	[PresentationTestMethod]
	public void SettingDateStringManuallyCanAcceptMultipleFormats()
	{
		CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = CreateControl();
			datePicker.SelectedDateFormat = CalendarDatePickerFormat.Custom;
			datePicker.CustomDateFormatString = "dd.MM.yyyy";
			datePicker.TextConverter = new CalendarDatePickerTextConverter();
			var tb = GetTextBox(datePicker);

			// parser can work with same format as CustomDateFormatString (but TextConverter must handle it)
			tb.Clear();
			RaiseTextEvent(tb, "17.10.2024");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);
			CornerstoneTest.AreEqual("2024-10-17", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate!.Value, new DateTime(2024, 10, 17)));

			// can also handle parsing other formats that the user enters, too
			tb.Clear();
			RaiseTextEvent(tb, "2024-02-13");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

			CornerstoneTest.AreEqual("2024-02-13", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate.Value, new DateTime(2024, 2, 13)));

			tb.Clear();
			RaiseTextEvent(tb, "04 22 2026");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

			CornerstoneTest.AreEqual("2026-04-22", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate.Value, new DateTime(2026, 4, 22)));

			// invalid input results in going back to last known (valid) date
			tb.Clear();
			RaiseTextEvent(tb, "Not A Valid Date");
			RaiseKeyEvent(tb, Key.Enter, KeyModifiers.None);

			CornerstoneTest.AreEqual("2026-04-22", datePicker.Text);
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate.Value, new DateTime(2026, 4, 22)));
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedDateToBlackoutDateShouldThrow()
	{
		using (UnitTestApplication.Start(Services))
		{
			var datePicker = CreateControl();
			CornerstoneTest.IsNotNull(datePicker.BlackoutDates);
			datePicker.BlackoutDates.AddDatesInPast();

			var goodValue = DateTime.Today.AddDays(1);
			datePicker.SelectedDate = goodValue;
			CornerstoneTest.IsTrue(CompareDates(datePicker.SelectedDate.Value, goodValue));

			var badValue = DateTime.Today.AddDays(-1);
			CornerstoneTest.Throws<ArgumentOutOfRangeException>(() => datePicker.SelectedDate = badValue);
		}
	}

	[PresentationTestMethod]
	public void TabFocusShouldMoveFocusToTextBox()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var datePicker = new CalendarDatePicker { Template = CreateTemplate() };
			var root = new TestRoot(datePicker);
			root.LayoutManager.ExecuteInitialLayoutPass();

			datePicker.Focus(NavigationMethod.Tab);

			CornerstoneTest.Same(GetTextBox(datePicker), root.FocusManager.GetFocusedElement());
		}
	}

	private static bool CompareDates(DateTime first, DateTime second)
	{
		return (first.Year == second.Year) &&
			(first.Month == second.Month) &&
			(first.Day == second.Day);
	}

	private static CalendarDatePicker CreateControl()
	{
		var datePicker =
			new CalendarDatePicker
			{
				Template = CreateTemplate()
			};

		datePicker.ApplyTemplate();
		return datePicker;
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<CalendarDatePicker>((control, scope) =>
		{
			var textBox =
				new TextBox
				{
					Name = "PART_TextBox"
				}.RegisterInNameScope(scope);
			var button =
				new Button
				{
					Name = "PART_Button"
				}.RegisterInNameScope(scope);
			var calendar =
				new Calendar
				{
					Name = "PART_Calendar",
					[!Calendar.SelectedDateProperty] = control[!CalendarDatePicker.SelectedDateProperty],
					[!Calendar.DisplayDateProperty] = control[!CalendarDatePicker.DisplayDateProperty],
					[!Calendar.DisplayDateStartProperty] = control[!CalendarDatePicker.DisplayDateStartProperty],
					[!Calendar.DisplayDateEndProperty] = control[!CalendarDatePicker.DisplayDateEndProperty]
				}.RegisterInNameScope(scope);
			var popup =
				new Popup
				{
					Name = "PART_Popup"
				}.RegisterInNameScope(scope);

			var panel = new Panel();
			panel.Children.Add(textBox);
			panel.Children.Add(button);
			panel.Children.Add(popup);
			panel.Children.Add(calendar);

			return panel;
		});
	}

	private TextBox GetTextBox(CalendarDatePicker control)
	{
		return control.GetTemplateDescendants()
			.OfType<TextBox>()
			.First();
	}

	private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
	{
		textBox.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			KeyModifiers = inputModifiers,
			Key = key
		});
	}

	private static void RaiseTextEvent(TextBox textBox, string text)
	{
		textBox.RaiseEvent(new TextInputEventArgs
		{
			RoutedEvent = InputElement.TextInputEvent,
			Text = text
		});
	}

	#endregion

	#region Classes

	private class CalendarDatePickerTextConverter : IValueConverter
	{
		#region Methods

		// date to text
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime d)
			{
				return d.ToString("yyyy-MM-dd"); // always return a single format (for this test)
			}
			return PresentationProperty.UnsetValue;
		}

		// text to date
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var str = value?.ToString();
			if (str == null)
			{
				return PresentationProperty.UnsetValue;
			}

			// allow for a few different date formats
			string[] formats = ["yyyy-MM-dd", "MM dd yyyy", "dd.MM.yyyy"];
			if (DateTime.TryParseExact(str, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
			{
				return dateValue;
			}
			return PresentationProperty.UnsetValue;
		}

		#endregion
	}

	#endregion
}