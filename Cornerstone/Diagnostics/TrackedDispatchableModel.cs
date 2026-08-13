#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Diagnostics;

/// <summary>
/// One tracked AppDispatcher root as seen by diagnostics capture.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.Updateable, ["*"])]
public partial class TrackedDispatchableModel : CornerstoneObject
{
	#region Constructors

	public TrackedDispatchableModel()
	{
		Name = string.Empty;
	}

	public TrackedDispatchableModel(string name, bool isAttached, bool hasModelChanges)
	{
		Name = name ?? string.Empty;
		IsAttached = isAttached;
		HasModelChanges = hasModelChanges;
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool HasModelChanges { get; set; }

	[Notify]
	public partial bool IsAttached { get; set; }

	[Notify]
	public partial string Name { get; set; }

	#endregion
}
