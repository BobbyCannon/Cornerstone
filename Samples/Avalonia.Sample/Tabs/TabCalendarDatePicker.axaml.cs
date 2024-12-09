#region References

using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabCalendarDatePicker : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "CalendarDatePicker";

	#endregion

	#region Constructors

	// Constructor.

	public TabCalendarDatePicker()
	{
		InitializeComponent();

		//--------------------------------------------------
		// Add blackout dates to example.
		//--------------------------------------------------

		var blackoutCalendarDatePicker = this.Get<CalendarDatePicker>("BlackoutCalendarDatePicker");
		blackoutCalendarDatePicker.TemplateApplied += (s, e) => { blackoutCalendarDatePicker.BlackoutDates?.AddDatesInPast(); };
	}

	#endregion
}