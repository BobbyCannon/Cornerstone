namespace Cornerstone.Presentation.ApplicationUpdate;

/// <summary>
/// Progress reported by <see cref="ApplicationUpdate.TryApply" />.
/// </summary>
public sealed class ApplicationUpdateProgress
{
	#region Constructors

	public ApplicationUpdateProgress(int percent, string status)
	{
		Percent = percent;
		Status = status;
	}

	#endregion

	#region Properties

	public int Percent { get; }

	public string Status { get; }

	#endregion
}