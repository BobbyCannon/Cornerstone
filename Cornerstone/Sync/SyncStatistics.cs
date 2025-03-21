#region References

using Cornerstone.Data;
using Cornerstone.Presentation;

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

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Changes},{Corrections},{AppliedChanges},{AppliedCorrections},{IndividualProcessCount}";
	}

	/// <summary>
	/// Update the SyncStatistics with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(SyncStatistics update, IncludeExcludeSettings settings)
	{
		// Code Generated - UpdateWith

		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** This code has been auto generated, do not edit this. ******

		UpdateProperty(AppliedChanges, update.AppliedChanges, settings.ShouldProcessProperty(nameof(AppliedChanges)), x => AppliedChanges = x);
		UpdateProperty(AppliedCorrections, update.AppliedCorrections, settings.ShouldProcessProperty(nameof(AppliedCorrections)), x => AppliedCorrections = x);
		UpdateProperty(Changes, update.Changes, settings.ShouldProcessProperty(nameof(Changes)), x => Changes = x);
		UpdateProperty(Corrections, update.Corrections, settings.ShouldProcessProperty(nameof(Corrections)), x => Corrections = x);
		UpdateProperty(IndividualProcessCount, update.IndividualProcessCount, settings.ShouldProcessProperty(nameof(IndividualProcessCount)), x => IndividualProcessCount = x);

		// Code Generated - /UpdateWith

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			SyncStatistics value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}