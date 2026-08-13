#region References

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
	}

	public ProfilerScopeModel(string name, double callsPerSecond, double averageTicks, long count)
	{
		Name = name ?? string.Empty;
		CallsPerSecond = callsPerSecond;
		AverageTicks = averageTicks;
		Count = count;
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
	public partial string Name { get; set; }

	#endregion
}
