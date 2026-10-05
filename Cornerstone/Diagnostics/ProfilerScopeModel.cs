#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Diagnostics;

/// <summary>
/// One profiler scope sample for diagnostics projection.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.Updateable, ["*"])]
public partial class ProfilerScopeModel : CornerstoneObject
{
	#region Constructors

	public ProfilerScopeModel()
	{
		Name = string.Empty;
		AverageTicks = 0;
		CallsPerSecond = 0;
		Count = 0;
		Elapsed = TimeSpan.Zero;
		Percent = 0;
		TotalTicks = 0;
	}

	public ProfilerScopeModel(string name, double callsPerSecond, double averageTicks, long count)
		: this()
	{
		Name = name ?? string.Empty;
		CallsPerSecond = callsPerSecond;
		AverageTicks = averageTicks;
		Count = count;
	}

	public ProfilerScopeModel(string name, long count, long totalTicks, double percent)
		: this()
	{
		Name = name ?? string.Empty;
		Count = count;
		TotalTicks = totalTicks;
		Elapsed = TimeSpan.FromTicks(totalTicks < 0 ? 0 : totalTicks);
		Percent = percent;
		AverageTicks = count <= 0 ? 0 : (double) totalTicks / count;
	}

	#endregion

	#region Properties

	[Notify]
	public partial double AverageTicks { get; set; }

	[Notify]
	public partial double CallsPerSecond { get; set; }

	[Notify]
	public partial long Count { get; set; }

	[Notify]
	public partial TimeSpan Elapsed { get; set; }

	[Notify]
	public partial string Name { get; set; }

	[Notify]
	public partial double Percent { get; set; }

	[Notify]
	public partial long TotalTicks { get; set; }

	#endregion
}
