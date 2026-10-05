namespace Cornerstone.VisualStudio.Protocol
{
	/// <summary>
	/// Matches a reply to the request that produced it. The editor host answers
	/// whichever call finishes first, so a slow completion cannot be paired with Enter.
	/// </summary>
	public interface IEditorCall
	{
		int RequestId { get; set; }
	}
}
