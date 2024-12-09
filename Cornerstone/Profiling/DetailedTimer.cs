#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Represents a timer with some additional descriptive details.
/// </summary>
public class DetailedTimer : Timer, IDetailedTimer
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
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public DetailedTimer(string name, IDetailedTimer parent, IDateTimeProvider timeProvider, IDispatcher dispatcher)
		: base(timeProvider, dispatcher)
	{
		Name = name;
		Parent = parent;
		Percent = 100;
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
	public IDetailedTimer Parent { get; }

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

	public static T Benchmark<T>(Func<T> action, out TimeSpan time)
	{
		var timer = StartNewTimer();
		var response = timer.Time(action);
		time = timer.Elapsed;
		return response;
	}

	/// <summary>
	/// Profile an action.
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
	/// Creates a timer and starts it running.
	/// </summary>
	/// <returns> The new timer that is currently running. </returns>
	public static DetailedTimer StartNewTimer(string name, IDateTimeProvider timeProvider = null, IDispatcher dispatcher = null)
	{
		var timer = new DetailedTimer(name, null, timeProvider, dispatcher);
		timer.Start();
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

	/// <inheritdoc cref="IDetailedTimer" />
	public override TimeSpan Stop()
	{
		var response = base.Stop();
		foreach (var child in Timers)
		{
			child.Stop();
		}
		return response;
	}

	public string ToDetailedString()
	{
		var builder = new TextBuilder();
		return ToDetailedString(builder);
	}

	public string ToDetailedString(TextBuilder builder)
	{
		RefreshPercent();
		ToDetailedString(this, builder);
		return builder.ToString();
	}

	public bool TryGetChild(string name, out DetailedTimer timer)
	{
		timer = Timers.FirstOrDefault(x => x.Name == name);
		return timer != null;
	}

	/// <summary>
	/// Create a timer with the provided name.
	/// </summary>
	/// <param name="name"> The name of the timer. </param>
	/// <returns> The new timer. </returns>
	protected virtual DetailedTimer CreateTimer(string name)
	{
		return new DetailedTimer(name, this, TimeProvider, GetDispatcher());
	}

	internal void RefreshPercent()
	{
		Percent = Parent == null ? 100 : Elapsed.PercentOf(Parent.Elapsed);

		foreach (var timer in Timers)
		{
			timer.RefreshPercent();
		}
	}

	internal static void ToDetailedString(IDetailedTimer timer, TextBuilder builder, int precision = 2)
	{
		var format = precision > 0 ? $"F{precision}" : "F2";
		builder.Append(timer.Percent.ToString(format));
		builder.Append("% ");
		builder.Append(timer.Elapsed.ToString("G"));
		builder.Append(": ");
		builder.AppendLine(timer.Name);

		if (!timer.Timers.Any())
		{
			return;
		}

		builder.PushIndent();
		var childTime = new TimeSpan();
		foreach (var child in timer.Timers)
		{
			childTime = childTime.Add(child.Elapsed);
			ToDetailedString(child, builder);
		}

		var remainder = timer.Elapsed - childTime;
		if (remainder > TimeSpan.Zero)
		{
			var percent = remainder.PercentOf(timer.Elapsed);
			builder.Append(percent.ToString(format));
			builder.Append("% ");
			builder.Append(remainder.ToString("G"));
			builder.Append(": ");
			builder.AppendLine("Remainder");
		}

		builder.PopIndent();
	}

	#endregion
}

public interface IDetailedTimer
{
	#region Properties

	/// <summary>
	/// The time elapsed for the timer.
	/// </summary>
	TimeSpan Elapsed { get; }

	/// <summary>
	/// The name of the timer.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Percent of the timer times relative to its parent.
	/// </summary>
	decimal Percent { get; }

	/// <summary>
	/// The internal timers for this timer.
	/// </summary>
	public SpeedyList<DetailedTimer> Timers { get; }

	#endregion
}