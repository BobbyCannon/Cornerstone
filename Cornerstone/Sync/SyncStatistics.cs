#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the communication statistics for a sync client.
/// </summary>
[SourceReflection]
public partial class SyncStatistics : CornerstoneObject<SyncStatistics>
{
	#region Properties

	/// <summary>
	/// Represents changes written (incoming) to this client.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int AppliedChanges { get; set; }

	/// <summary>
	/// Represents corrections written (incoming) to this client.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int AppliedCorrections { get; set; }

	/// <summary>
	/// Represents changes sent (outgoing) from this client.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Changes { get; set; }

	/// <summary>
	/// Represents corrections sent (outgoing) from this client.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Corrections { get; set; }

	/// <summary>
	/// Represents how many times the sync client had to process applied changes individually.
	/// This means at some point one of the synced items had issues saving so we have to process items
	/// individually so we can determine which item is having issues.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int IndividualProcessCount { get; set; }

	/// <summary>
	/// Returns true if the statistics are all zero.
	/// </summary>
	public bool IsReset => (AppliedChanges == 0) && (AppliedCorrections == 0) && (Changes == 0) && (Corrections == 0) && (IndividualProcessCount == 0);

	#endregion

	#region Methods

	/// <summary>
	/// Allows resetting of the sync statistics.
	/// </summary>
	public void Reset()
	{
		AppliedChanges = 0;
		AppliedCorrections = 0;
		Changes = 0;
		Corrections = 0;
		IndividualProcessCount = 0;
	}

	public override string ToString()
	{
		return $"C{Changes},C+{Corrections},A{AppliedChanges},A+{AppliedCorrections},I{IndividualProcessCount}";
	}

	#endregion
}