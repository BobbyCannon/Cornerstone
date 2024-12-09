#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Profiling;

public class CountdownTimer : Bindable
{
	#region Fields

	private readonly Timer _timer;

	#endregion

	#region Constructors

	public CountdownTimer(IDateTimeProvider dateTimeProvider, TimeSpan time, IDispatcher dispatcher) : base(dispatcher)
	{
		_timer = Timer.StartNewTimer(dateTimeProvider, dispatcher);

		Time = time;
	}

	#endregion

	#region Properties

	public TimeSpan Remaining => Time - _timer.Elapsed;

	public string RemainingLabel => $"{(int) Remaining.TotalMinutes:00}:{Remaining:ss}";

	public decimal RemainingPercent => _timer.Elapsed >= Time ? 0 : 100 - _timer.Elapsed.PercentOf(Time);

	public TimeSpan Time { get; }

	#endregion

	#region Methods

	public void Refresh()
	{
		OnPropertyChanged(nameof(Remaining));
		OnPropertyChanged(nameof(RemainingLabel));
		OnPropertyChanged(nameof(RemainingPercent));
	}

	#endregion
}