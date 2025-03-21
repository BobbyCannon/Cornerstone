#region References

using System;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;
// This class is public because it can be used as a base class for custom links.

/// <summary>
/// Detects hyperlinks and makes them clickable.
/// </summary>
/// <remarks>
/// This element generator can be easily enabled and configured using the
/// <see cref="TextEditorSettings" />.
/// </remarks>
public class LinkElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
{
	#region Fields

	// a link starts with a protocol (or just with www), followed by 0 or more 'link characters', followed by a link end character
	// (this allows accepting punctuation inside links but not at the end)
	internal static readonly Regex DefaultLinkRegex = new(@"\b(https?://|ftp://|www\.)[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]");

	// try to detect email addresses
	internal static readonly Regex DefaultMailRegex = new(@"\b[\w\d\.\-]+\@[\w\d\.\-]+\.[a-z]{2,6}\b");

	private readonly Regex _linkRegex;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new LinkElementGenerator.
	/// </summary>
	public LinkElementGenerator()
	{
		_linkRegex = DefaultLinkRegex;
		RequireControlModifierForClick = true;
	}

	/// <summary>
	/// Creates a new LinkElementGenerator using the specified regex.
	/// </summary>
	protected LinkElementGenerator(Regex regex) : this()
	{
		_linkRegex = regex ?? throw new ArgumentNullException(nameof(regex));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether the user needs to press Control to click the link.
	/// The default value is true.
	/// </summary>
	public bool RequireControlModifierForClick { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override VisualLineElement ConstructElement(int offset)
	{
		var m = GetMatch(offset, out var matchOffset);
		if (m.Success && (matchOffset == offset))
		{
			return ConstructElementFromMatch(m);
		}
		return null;
	}

	/// <inheritdoc />
	public override int GetFirstInterestedOffset(int startOffset)
	{
		GetMatch(startOffset, out var matchOffset);
		return matchOffset;
	}

	/// <summary>
	/// Constructs a VisualLineElement that replaces the matched text.
	/// The default implementation will create a <see cref="VisualLineLinkText" />
	/// based on the URI provided by <see cref="GetUriFromMatch" />.
	/// </summary>
	protected virtual VisualLineElement ConstructElementFromMatch(Match m)
	{
		var uri = GetUriFromMatch(m);
		if (uri == null)
		{
			return null;
		}
		var linkText = new VisualLineLinkText(CurrentContext.VisualLine, m.Length)
		{
			NavigateUri = uri,
			RequireControlModifierForClick = RequireControlModifierForClick
		};
		return linkText;
	}

	/// <summary>
	/// Fetches the URI from the regex match. Returns null if the URI format is invalid.
	/// </summary>
	protected virtual Uri GetUriFromMatch(Match match)
	{
		var targetUrl = match.Value;
		if (targetUrl.StartsWith("www.", StringComparison.Ordinal))
		{
			targetUrl = "http://" + targetUrl;
		}
		return Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute) ? new Uri(targetUrl) : null;
	}

	void IBuiltinElementGenerator.FetchOptions(TextEditorSettings settings)
	{
		RequireControlModifierForClick = settings.RequireControlModifierForHyperlinkClick;
	}

	private Match GetMatch(int startOffset, out int matchOffset)
	{
		var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndIndex;
		var relevantText = CurrentContext.GetText(startOffset, endOffset - startOffset);
		var m = _linkRegex.Match(relevantText.Text, relevantText.Offset, relevantText.Count);
		matchOffset = m.Success ? (m.Index - relevantText.Offset) + startOffset : -1;
		return m;
	}

	#endregion
}

// This class is internal because it does not need to be accessed by the user - it can be configured using TextEditorOptions.

/// <summary>
/// Detects e-mail addresses and makes them clickable.
/// </summary>
/// <remarks>
/// This element generator can be easily enabled and configured using the
/// <see cref="TextEditorSettings" />.
/// </remarks>
internal sealed class MailLinkElementGenerator : LinkElementGenerator
{
	#region Constructors

	/// <summary>
	/// Creates a new MailLinkElementGenerator.
	/// </summary>
	public MailLinkElementGenerator()
		: base(DefaultMailRegex)
	{
	}

	#endregion

	#region Methods

	protected override Uri GetUriFromMatch(Match match)
	{
		var targetUrl = "mailto:" + match.Value;
		return Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute) ? new Uri(targetUrl) : null;
	}

	#endregion
}