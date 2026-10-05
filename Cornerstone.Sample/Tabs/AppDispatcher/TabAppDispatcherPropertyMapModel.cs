#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs.AppDispatcher;

/// <summary>
/// Off-dispatcher model slice for TrackProperties / PropertyMapBinding demo.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class TabAppDispatcherPropertyMapModel : CornerstoneObject
{
	#region Properties

	public partial int Count { get; set; }

	public partial double Ratio { get; set; }

	public partial string Title { get; set; }

	#endregion
}