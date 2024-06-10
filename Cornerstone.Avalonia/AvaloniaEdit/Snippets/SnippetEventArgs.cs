#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Provides information about the event that occured during use of snippets.
/// </summary>
public class SnippetEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Creates a new SnippetEventArgs object, with a DeactivateReason.
	/// </summary>
	public SnippetEventArgs(DeactivateReason reason)
	{
		Reason = reason;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the reason for deactivation.
	/// </summary>
	public DeactivateReason Reason { get; }

	#endregion
}

/// <summary>
/// Describes the reason for deactivation of a <see cref="SnippetElement" />.
/// </summary>
public enum DeactivateReason
{
	/// <summary>
	/// Unknown reason.
	/// </summary>
	Unknown,

	/// <summary>
	/// Snippet was deleted.
	/// </summary>
	Deleted,

	/// <summary>
	/// There are no active elements in the snippet.
	/// </summary>
	NoActiveElements,

	/// <summary>
	/// The SnippetInputHandler was detached.
	/// </summary>
	InputHandlerDetached,

	/// <summary>
	/// Return was pressed by the user.
	/// </summary>
	ReturnPressed,

	/// <summary>
	/// Escape was pressed by the user.
	/// </summary>
	EscapePressed
}