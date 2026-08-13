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
public partial class GrokUsagePeriodState : CornerstoneObject
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
	/// UI label for the combo box.
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