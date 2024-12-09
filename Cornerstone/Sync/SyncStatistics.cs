#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the communication statistics for a sync client.
/// </summary>
public class SyncStatistics : Bindable<SyncStatistics>
{
	#region Constructors

	/// <summary>
	/// Instantiate an instance of a sync statistics.
	/// </summary>
	public SyncStatistics()
	{
	}

	/// <summary>
	/// Instantiate an instance of a sync statistics.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SyncStatistics(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Represents changes written (incoming) to this client.
	/// </summary>
	public int AppliedChanges { get; set; }

	/// <summary>
	/// Represents corrections written (incoming) to this client.
	/// </summary>
	public int AppliedCorrections { get; set; }

	/// <summary>
	/// Represents changes sent (outgoing) from this client.
	/// </summary>
	public int Changes { get; set; }

	/// <summary>
	/// Represents corrections sent (outgoing) from this client.
	/// </summary>
	public int Corrections { get; set; }

	/// <summary>
	/// Represents how many times the sync client had to process applied changes individually.
	/// This means at some point one of the synced items had issues saving so we have to process items
	/// individually so we can determine which item is having issues.
	/// </summary>
	public int IndividualProcessCount { get; set; }

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

	/// <summary>
	/// Update the SyncStatistics with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(SyncStatistics update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			AppliedChanges = update.AppliedChanges;
			AppliedCorrections = update.AppliedCorrections;
			Changes = update.Changes;
			Corrections = update.Corrections;
			IndividualProcessCount = update.IndividualProcessCount;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(AppliedChanges)), x => x.AppliedChanges = update.AppliedChanges);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(AppliedCorrections)), x => x.AppliedCorrections = update.AppliedCorrections);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Changes)), x => x.Changes = update.Changes);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Corrections)), x => x.Corrections = update.Corrections);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(IndividualProcessCount)), x => x.IndividualProcessCount = update.IndividualProcessCount);
		}

		return true;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Changes},{Corrections},{AppliedChanges},{AppliedCorrections},{IndividualProcessCount}";
	}

	#endregion
}