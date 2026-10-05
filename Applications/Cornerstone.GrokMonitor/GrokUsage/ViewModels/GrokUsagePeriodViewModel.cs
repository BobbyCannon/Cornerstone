#region References

using System;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage.State;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.ViewModels;

/// <summary>
/// Presentation item for the billing-period combo box (not a State type).
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GrokUsagePeriodViewModel : ViewModel, IGrokUsagePeriod, IUpdateable<IGrokUsagePeriod>
{
	#region Constructors

	public GrokUsagePeriodViewModel()
	{
		DisplayName = string.Empty;
		PeriodType = string.Empty;
	}

	#endregion

	#region Properties

	[Notify]
	public partial string DisplayName { get; set; }

	[Notify]
	public partial bool IsCurrent { get; set; }

	[Notify]
	public partial DateTimeOffset PeriodEnd { get; set; }

	[Notify]
	public partial DateTimeOffset PeriodStart { get; set; }

	[Notify]
	public partial string PeriodType { get; set; }

	#endregion
}