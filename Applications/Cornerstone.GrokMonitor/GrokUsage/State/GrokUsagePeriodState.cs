#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.State;

/// <summary>
/// One selectable billing/usage period for the period dropdown (PeriodEnd exclusive).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GrokUsagePeriodState : CornerstoneObject, IGrokUsagePeriod, IUpdateable<IGrokUsagePeriod>
{
	#region Constructors

	public GrokUsagePeriodState()
	{
		DisplayName = string.Empty;
		PeriodType = string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Optional period title from discovery. The dashboard ViewModel formats the combo label.
	/// </summary>
	public partial string DisplayName { get; set; }

	/// <summary>
	/// True when this is the account's current billing period.
	/// </summary>
	public partial bool IsCurrent { get; set; }

	/// <summary>
	/// Exclusive end of the period.
	/// </summary>
	public partial DateTimeOffset PeriodEnd { get; set; }

	/// <summary>
	/// Inclusive start of the period.
	/// </summary>
	public partial DateTimeOffset PeriodStart { get; set; }

	/// <summary>
	/// Period type string; empty when unknown.
	/// </summary>
	public partial string PeriodType { get; set; }

	#endregion
}

/// <summary>
/// Shared billing-period contract for State and the period combo option.
/// Setters exist so UpdateWith can copy; the combo publishes SelectPeriod instead of writing State.
/// </summary>
public interface IGrokUsagePeriod
{
	#region Properties

	string DisplayName { get; set; }

	bool IsCurrent { get; set; }

	DateTimeOffset PeriodEnd { get; set; }

	DateTimeOffset PeriodStart { get; set; }

	string PeriodType { get; set; }

	#endregion
}