namespace Cornerstone.Presentation.ApplicationUpdate;

/// <summary>
/// Outcome of <see cref="ApplicationUpdate.TryApply" />.
/// </summary>
public enum ApplicationUpdateResult
{
	/// <summary>
	/// The source directory was copied onto the destination.
	/// </summary>
	Success = 0,

	/// <summary>
	/// No destination path was provided.
	/// </summary>
	DestinationMissing = 1,

	/// <summary>
	/// The destination is a drive root or another protected folder.
	/// </summary>
	ProtectedDestination = 2,

	/// <summary>
	/// The destination has files and does not contain this application.
	/// </summary>
	DestinationNotApplication = 3,

	/// <summary>
	/// The destination has more extra files and folders than <see cref="ApplicationUpdate.MaximumRemovals" />.
	/// </summary>
	TooManyRemovals = 4,

	/// <summary>
	/// The copy failed.
	/// </summary>
	Failed = 5
}