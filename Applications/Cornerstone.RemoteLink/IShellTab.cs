namespace Cornerstone.RemoteLink;

/// <summary>
/// Shared header surface for RemoteLink shell tabs.
/// </summary>
public interface IShellTab
{
	#region Properties

	/// <summary>
	/// Tab header text.
	/// </summary>
	string DisplayName { get; }

	#endregion
}
