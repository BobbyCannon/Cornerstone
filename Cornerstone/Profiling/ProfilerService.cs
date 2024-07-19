#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Class for profiling actions
/// </summary>
public class ProfilerService : DetailedTimer
{
	#region Constructors

	/// <summary>
	/// Instantiate an instance of the provider service.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public ProfilerService(IDispatcher dispatcher) : base("Profile Service", null, null, dispatcher)
	{
	}

	#endregion

	#region Methods

	public static T Benchmark<T>(Func<T> action, out TimeSpan time)
	{
		var timer = StartNewTimer();
		var response = timer.Time(action);
		time = timer.Elapsed;
		return response;
	}

	/// <inheritdoc />
	protected override DetailedTimer CreateTimer(string name)
	{
		return new DetailedTimer(name, GetDispatcher());
	}

	#endregion
}