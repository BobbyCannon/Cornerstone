#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.State;

/// <summary>
/// App-wide Grok usage state: configured homes and selection filter.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
public partial class GrokUsageState : CornerstoneObject
{
	#region Constructors

	[DependencyInjectionConstructor]
	public GrokUsageState()
	{
		Homes = [];
		LastError = string.Empty;
		IsViewLive = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when a real (non-synthetic) plan period template is available for synthetic weeks.
	/// </summary>
	public bool HasPlanPeriodTemplate =>
		(PlanPeriodStart != default)
		&& (PlanPeriodEnd != default)
		&& (PlanPeriodEnd > PlanPeriodStart);

	/// <summary>
	/// Discovered Grok homes (~/.grok, ~/.grok-work, …). Re-scanned on EnsureHomes / refresh.
	/// </summary>
	public SpeedyList<GrokHomeUsageState> Homes { get; }

	/// <summary>
	/// When true, the GrokUsage view clock follows wall time (slider at live end).
	/// When false, <see cref="ViewAsOf" /> is used as "now" for refresh/projection.
	/// </summary>
	public partial bool IsViewLive { get; set; }

	/// <summary>
	/// Last global error not tied to a single home refresh.
	/// </summary>
	public partial string LastError { get; set; }

	public static StringComparison PathComparison =>
		OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

	/// <summary>
	/// Last known real billing period end (exclusive), usually from the primary grok home.
	/// Used to phase other homes' synthetic weeks to the same plan reset time.
	/// Default means unknown.
	/// </summary>
	public partial DateTimeOffset PlanPeriodEnd { get; set; }

	/// <summary>
	/// Last known real billing period start, usually from the primary grok home.
	/// Used to phase other homes' synthetic weeks to the same plan reset time.
	/// Default means unknown.
	/// </summary>
	public partial DateTimeOffset PlanPeriodStart { get; set; }

	/// <summary>
	/// Currently focused home id; <see cref="Guid.Empty" /> when none.
	/// </summary>
	public partial Guid SelectedHomeId { get; set; }

	/// <summary>
	/// Lower bound for inference timestamps; default means no filter.
	/// </summary>
	public partial DateTimeOffset SinceUtc { get; set; }

	/// <summary>
	/// Simulated "now" for the usage dashboard when <see cref="IsViewLive" /> is false.
	/// Default is ignored while live.
	/// </summary>
	public partial DateTimeOffset ViewAsOf { get; set; }

	#endregion

	#region Methods

	public GrokHomeUsageState FindById(Guid id)
	{
		return Homes.FirstOrDefault(x => x.Id == id);
	}

	public GrokHomeUsageState FindByPath(string path)
	{
		var normalized = NormalizePath(path);
		if (string.IsNullOrEmpty(normalized))
		{
			return null;
		}

		return Homes.FirstOrDefault(x =>
			string.Equals(NormalizePath(x.Path), normalized, PathComparison));
	}

	public GrokHomeUsageState FindSelected()
	{
		return FindById(SelectedHomeId) ?? Homes.FirstOrDefault();
	}

	public static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		try
		{
			return Path.GetFullPath(path.Trim());
		}
		catch
		{
			return path.Trim();
		}
	}

	#endregion
}