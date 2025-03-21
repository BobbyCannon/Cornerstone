#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Logging;

public class TrackerPathConfigurationEntity : SyncEntity<int>
{
	#region Constructors

	[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
	public TrackerPathConfigurationEntity()
	{
		Paths = new List<TrackerPathEntity>();
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public string CompletedOnName { get; set; }

	public string DataName { get; set; }

	/// <inheritdoc />
	public override int Id { get; set; }

	public string Name01 { get; set; }

	public string Name02 { get; set; }

	public string Name03 { get; set; }

	public string Name04 { get; set; }

	public string Name05 { get; set; }

	public string Name06 { get; set; }

	public string Name07 { get; set; }

	public string Name08 { get; set; }

	public string Name09 { get; set; }

	public string PathName { get; set; }

	public virtual ICollection<TrackerPathEntity> Paths { get; set; }

	public string PathType { get; set; }

	public bool IsException { get; set; }

	public string StartedOnName { get; set; }

	public TrackerPathValueDataType Type01 { get; set; }

	public TrackerPathValueDataType Type02 { get; set; }

	public TrackerPathValueDataType Type03 { get; set; }

	public TrackerPathValueDataType Type04 { get; set; }

	public TrackerPathValueDataType Type05 { get; set; }

	public TrackerPathValueDataType Type06 { get; set; }

	public TrackerPathValueDataType Type07 { get; set; }

	public TrackerPathValueDataType Type08 { get; set; }

	public TrackerPathValueDataType Type09 { get; set; }

	#endregion
}