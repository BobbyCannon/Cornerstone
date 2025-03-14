#region References

using Avalonia.Controls;
using Cornerstone.Avalonia;

#endregion

namespace Cornerstone.Sample.Tabs;

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