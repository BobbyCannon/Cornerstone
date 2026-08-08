#region References

using System;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Raised when the user activates a markdown link in <see cref="MarkdownView" />.
/// Set <see cref="Handled" /> to true to prevent default host behavior.
/// </summary>
public sealed class MarkdownLinkClickedEventArgs : EventArgs
{
	#region Constructors

	public MarkdownLinkClickedEventArgs(string href, string text)
	{
		Href = href ?? string.Empty;
		Text = text ?? string.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, the host has handled the link (e.g. navigated or opened a browser).
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Raw destination from the markdown (may be relative, fragment-only, or absolute URL).
	/// </summary>
	public string Href { get; }

	/// <summary>
	/// Visible link text.
	/// </summary>
	public string Text { get; }

	#endregion
}