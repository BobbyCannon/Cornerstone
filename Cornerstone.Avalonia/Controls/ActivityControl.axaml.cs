#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class ActivityControl : CornerstoneTemplatedControl
{
	#region Fields

	public static readonly StyledProperty<IList<ActivityItem>> ItemsSourceProperty;

	public static readonly DirectProperty<ActivityControl, IList<string>> MonthsProperty;

	#endregion

	#region Constructors

	public ActivityControl()
	{
		ItemsSource = new List<ActivityItem>();
	}

	static ActivityControl()
	{
		MonthsProperty = AvaloniaProperty.RegisterDirect<ActivityControl, IList<string>>(nameof(Months), x => x.Months);
		ItemsSourceProperty = AvaloniaProperty.Register<ActivityControl, IList<ActivityItem>>(nameof(ItemsSource));
	}

	#endregion

	#region Properties

	public IList<ActivityItem> ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public IList<string> Months => GetMonths(ItemsSource.LastOrDefault());

	#endregion

	#region Methods

	private IList<string> GetMonths(ActivityItem last)
	{
		var date = last?.Date ?? DateTime.Today;

		return Enumerable.Range(0, 13)
			.Select(i => date.AddMonths(-i))
			.Select(x => x.ToString("MMM"))
			.Reverse()
			.ToArray();
	}

	#endregion
}