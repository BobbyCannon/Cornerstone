namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Markup error from either preview protocol. Field layout matches both
/// Avalonia and Cornerstone ExceptionDetails.
/// </summary>
public sealed class PreviewExceptionDetails
{
	public string ExceptionType { get; set; }

	public int? LineNumber { get; set; }

	public int? LinePosition { get; set; }

	public string Message { get; set; }
}
