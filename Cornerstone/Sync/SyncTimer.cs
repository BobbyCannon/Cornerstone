#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a timer for tracking a sync session.
/// </summary>
public class SyncTimer : AverageTimer
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	public SyncTimer() : base(10, null, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the class.
	/// </summary>
	/// <param name="limit"> Optional limit of syncs to average. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SyncTimer(int limit, IDateTimeProvider timeProvider, IDispatcher dispatcher) : base(limit, timeProvider, dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Tracks the number of cancelled syncs.
	/// </summary>
	public int CancelledSyncs { get; set; }

	/// <summary>
	/// Tracks the number of failed syncs.
	/// </summary>
	public int FailedSyncs { get; set; }

	/// <summary>
	/// Tracks the number of successful syncs.
	/// </summary>
	public int SuccessfulSyncs { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the SyncTimer with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	public virtual bool UpdateWith(SyncTimer update)
	{
		return UpdateWith(update, IncludeExcludeSettings.Empty);
	}

	/// <summary>
	/// Update the SyncTimer with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public virtual bool UpdateWith(SyncTimer update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			CancelledSyncs = update.CancelledSyncs;
			FailedSyncs = update.FailedSyncs;
			SuccessfulSyncs = update.SuccessfulSyncs;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(CancelledSyncs)), x => x.CancelledSyncs = update.CancelledSyncs);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(FailedSyncs)), x => x.FailedSyncs = update.FailedSyncs);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SuccessfulSyncs)), x => x.SuccessfulSyncs = update.SuccessfulSyncs);
		}

		return base.UpdateWith(update, settings);
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			SyncTimer value => UpdateWith(value, settings),
			AverageTimer value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}