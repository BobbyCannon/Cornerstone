namespace Cornerstone.GrokMonitor;

/// <summary>
/// Shared header surface for Grok Monitor shell tabs (home dashboards and Settings).
/// </summary>
public interface IShellTab
{
	#region Properties

	/// <summary>
	/// Tab header text.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// Optional tooltip (e.g. home path); empty when not applicable.
	/// </summary>
	string Path { get; }

	#endregion
}