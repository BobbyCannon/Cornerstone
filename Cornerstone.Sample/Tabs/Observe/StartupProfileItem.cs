#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Profiling;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs.Observe;

/// <summary>
/// Hierarchical row for the Profiler startup tree.
/// </summary>
[Notifiable(["*"])]
[SourceReflection]
public partial class StartupProfileItem : SpeedyTree<StartupProfileItem>
{
	#region Properties

	public partial string Elapsed { get; set; }

	public partial string Name { get; set; }

	public partial string Percent { get; set; }

	#endregion

	#region Methods

	public static IEnumerable<StartupProfileItem> FromProfiler(StartupProfiler profiler, StartupProfileDetail detail)
	{
		if (profiler is not { IsCompleted: true, Root: not null })
		{
			yield break;
		}

		var root = FromSample(profiler.Root, profiler.Root.Elapsed, StartupProfiler.ThresholdFor(detail));
		if (root != null)
		{
			yield return root;
		}
	}

	public static StartupProfileItem FromSample(StartupSample sample, TimeSpan parentElapsed, TimeSpan minimumElapsed)
	{
		if (!StartupProfiler.KeepSample(sample, minimumElapsed))
		{
			return null;
		}

		var item = new StartupProfileItem
		{
			Name = sample.Name,
			Elapsed = FormatElapsed(sample.Elapsed),
			Percent = FormatPercent(sample.Elapsed, parentElapsed),
			IsExpanded = true
		};

		foreach (var child in sample.Children)
		{
			var childItem = FromSample(child, sample.Elapsed, minimumElapsed);
			if (childItem != null)
			{
				item.Children.Add(childItem);
			}
		}

		return item;
	}

	private static string FormatElapsed(TimeSpan elapsed)
	{
		return $"{elapsed.TotalMilliseconds:0.0} ms";
	}

	private static string FormatPercent(TimeSpan elapsed, TimeSpan parentElapsed)
	{
		if (parentElapsed.Ticks <= 0)
		{
			return "100.0%";
		}

		var percent = (100.0 * elapsed.Ticks) / parentElapsed.Ticks;
		return $"{percent:0.0}%";
	}

	#endregion
}