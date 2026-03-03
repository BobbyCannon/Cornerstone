namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// Options for configuring MarkdownSharp.
/// </summary>
public class MarkdownOptions
{
	#region Properties

	/// <summary>
	/// when true, asterisks may be used for intra-word emphasis
	/// this does nothing if StrictBoldItalic is false
	/// </summary>
	public bool AsteriskIntraWordEmphasis { get; set; }

	/// <summary>
	/// when true, (most) bare plain URLs are auto-hyperlinked
	/// WARNING: this is a significant deviation from the markdown spec
	/// </summary>
	public bool AutoHyperlink { get; set; }

	/// <summary>
	/// when true, RETURN becomes a literal newline
	/// WARNING: this is a significant deviation from the markdown spec
	/// </summary>
	public bool AutoNewlines { get; set; }

	/// <summary>
	/// use ">" for HTML output, or " />" for XHTML output
	/// </summary>
	public string EmptyElementSuffix { get; set; }

	/// <summary>
	/// when false, email addresses will never be auto-linked
	/// WARNING: this is a significant deviation from the markdown spec
	/// </summary>
	public bool LinkEmails { get; set; }

	/// <summary>
	/// when true, bold and italic require non-word characters on either side
	/// WARNING: this is a significant deviation from the markdown spec
	/// </summary>
	public bool StrictBoldItalic { get; set; }

	#endregion
}