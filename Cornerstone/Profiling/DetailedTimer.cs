#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Represents a timer with some additional descriptive details.
/// </summary>
public class DetailedTimer : Timer
{
	#region Constructors

	/// <summary>
	/// Initialize the timer.
	/// </summary>
	/// <param name="name"> The name of the timer. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public DetailedTimer(string name, IDispatcher dispatcher) : this(name, null, null, dispatcher)
	{
	}

	/// <summary>
	/// Initialize the timer.
	/// </summary>
	/// <param name="name"> The name of the timer. </param>
	/// <param name="parent"> The parent timer. </param>
	/// <param name="timeService"> An optional TimeService instead of DateTime. Defaults to new instance of TimeService (DateTime). </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public DetailedTimer(string name, DetailedTimer parent, ITimeProvider timeService, IDispatcher dispatcher)
		: base(timeService, dispatcher)
	{
		Name = name;
		Parent = parent;
		Percent = -1;
		Timers = new SpeedyList<DetailedTimer>(dispatcher);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The name of the timer.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The parent timer.
	/// </summary>
	public DetailedTimer Parent { get; }

	/// <summary>
	/// Percent of the timer times relative to its parent.
	/// </summary>
	public decimal Percent { get; private set; }

	/// <summary>
	/// The internal timers for this timer.
	/// </summary>
	public SpeedyList<DetailedTimer> Timers { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Profile a action.
	/// </summary>
	/// <param name="name"> The name of timer. </param>
	/// <param name="action"> The action to profile. </param>
	/// <returns> The completed timer. </returns>
	public DetailedTimer Profile(string name, Action action)
	{
		var timer = new DetailedTimer(name, this, null, GetDispatcher());
		try
		{
			Timers.Add(timer);
			timer.Start();
			action();
		}
		finally
		{
			timer.Stop();
		}
		return timer;
	}

	/// <summary>
	/// Start a timer for the profiler service.
	/// </summary>
	/// <param name="name"> The name of the timer. </param>
	/// <returns> The new active timer. </returns>
	public DetailedTimer StartTimer(string name)
	{
		var timer = CreateTimer(name);
		Timers.Add(timer);
		timer.Start();
		return timer;
	}

	/// <inheritdoc />
	public override TimeSpan Stop()
	{
		var response = base.Stop();
		RefreshPercent();
		return response;
	}

	/// <summary>
	/// Create a timer with the provided name.
	/// </summary>
	/// <param name="name"> The name of the timer. </param>
	/// <returns> The new timer. </returns>
	protected virtual DetailedTimer CreateTimer(string name)
	{
		return new DetailedTimer(name, this, null, GetDispatcher());
	}

	private void RefreshPercent()
	{
		Percent = Parent == null ? 100 : Elapsed.PercentOf(Parent.Elapsed);

		foreach (var timer in Timers)
		{
			timer.RefreshPercent();
		}
	}

	#endregion
}