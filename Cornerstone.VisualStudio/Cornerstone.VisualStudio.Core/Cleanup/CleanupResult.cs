namespace Cornerstone.VisualStudio.Core.Cleanup;

/// <summary>
/// Result of <see cref="CleanupPipeline.Clean" />.
/// </summary>
public sealed class CleanupResult
{
	#region Constructors

	private CleanupResult(CleanupOutcome outcome, string text, string message, bool structuralApplied)
	{
		Outcome = outcome;
		Text = text ?? string.Empty;
		Message = message;
		StructuralApplied = structuralApplied;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when the returned <see cref="Text" /> differs from the input.
	/// </summary>
	public bool HasTextChange { get; private set; }

	public string Message { get; }

	public CleanupOutcome Outcome { get; }

	public bool StructuralApplied { get; }

	public string Text { get; }

	#endregion

	#region Methods

	public static CleanupResult CreateChanged(string text, bool structuralApplied, string message = null)
	{
		return new CleanupResult(CleanupOutcome.Changed, text, message, structuralApplied)
		{
			HasTextChange = true
		};
	}

	public static CleanupResult CreateError(string originalText, string message)
	{
		return new CleanupResult(CleanupOutcome.Error, originalText, message, false)
		{
			HasTextChange = false
		};
	}

	public static CleanupResult CreateSkipped(string originalText, string message)
	{
		return new CleanupResult(CleanupOutcome.Skipped, originalText, message, false)
		{
			HasTextChange = false
		};
	}

	public static CleanupResult CreateStructuralSkipped(string text, string originalText, string message)
	{
		var changed = !string.Equals(text, originalText, System.StringComparison.Ordinal);
		return new CleanupResult(
			changed ? CleanupOutcome.Changed : CleanupOutcome.StructuralSkipped,
			text,
			message,
			false)
		{
			HasTextChange = changed
		};
	}

	public static CleanupResult CreateUnchanged(string text, bool structuralApplied, string message = null)
	{
		return new CleanupResult(CleanupOutcome.Unchanged, text, message, structuralApplied)
		{
			HasTextChange = false
		};
	}

	#endregion
}
