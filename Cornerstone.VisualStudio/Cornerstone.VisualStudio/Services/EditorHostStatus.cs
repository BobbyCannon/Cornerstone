namespace Cornerstone.VisualStudio.Services;

internal sealed class EditorHostStatus
{
	public string State { get; set; }

	public string Detail { get; set; }

	public string Activity { get; set; }

	public int ProcessId { get; set; }

	public int Port { get; set; }
}
