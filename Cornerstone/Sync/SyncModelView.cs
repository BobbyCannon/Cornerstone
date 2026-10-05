#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync model view for displaying sync models
/// </summary>
/// <remarks>
/// Pack properties 1-4 are in use.
/// </remarks>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class SyncModelView : SyncModel
{
}