#region References

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

#endregion

namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// Markdown is a text-to-HTML conversion tool for web writers.
/// Markdown allows you to write using an easy-to-read, easy-to-write plain text format,
/// then convert it to structurally valid XHTML (or HTML).
/// </summary>
public class MarkdownService
{
	#region Constants

	/// <summary>
	/// temporarily replaces "://" where auto-linking shouldn't happen
	/// </summary>
	private const string AutoLinkPreventionMarker = "\x1AP";

	private const string CharEndingUrl = "[-A-Z0-9+&@#/%=~_|\\[\\])]";
	private const string CharInsideUrl = @"[-A-Z0-9+&@#/%?=~_|\[\]\(\)!:,\.;" + "\x1a]";
	private const string MarkerOl = @"\d+[.]";
	private const string MarkerUl = "[*+-]";

	/// <summary>
	/// maximum nested depth of [] and () supported by the transform; implementation detail
	/// </summary>
	private const int NestDepth = 6;

	/// <summary>
	/// Tabs are automatically converted to spaces as part of the transform
	/// this constant determines how "wide" those tabs become in spaces
	/// </summary>
	private const int TabWidth = 4;

	#endregion

	#region Fields

	private static readonly Regex _amps;
	private static readonly Regex _anchorInline;
	private static readonly Regex _anchorRef;
	private static readonly Regex _anchorRefShortcut;
	private static readonly Regex _angles;
	private static readonly Regex _autoEmailBare;
	private static readonly Regex _autolinkBare;
	private static readonly Regex _backslashEscapes;
	private static readonly Dictionary<string, string> _backslashEscapeTable;
	private static readonly Regex _blockquote;
	private static readonly Regex _blocksHtml;
	private static readonly Regex _bold;
	private static readonly Regex _codeBlock;
	private static readonly Regex _codeEncoder;
	private static readonly Regex _codeSpan;
	private static readonly Regex _endCharRegex;
	private static readonly Dictionary<string, string> _escapeTable;
	private static readonly Regex _headerAtx;
	private static readonly Regex _headerSetext;
	private static readonly Regex _horizontalRules;
	private static readonly Regex _htmlBlockHash;
	private readonly Dictionary<string, string> _htmlBlocks;
	private static readonly Regex _htmlTokens;
	private static readonly Regex _imagesInline;
	private static readonly Regex _imagesRef;
	private static readonly Dictionary<string, string> _invertedEscapeTable;
	private static readonly Regex _italic;
	private static readonly Regex _leadingWhitespace;
	private static readonly Regex _linkDef;
	private static readonly Regex _linkEmail;
	private int _listLevel;
	private static readonly Regex _listNested;
	private static readonly Regex _listTopLevel;
	private static string _nestedBracketsPattern;
	private static string _nestedParensPattern;
	private static readonly Regex _newlinesLeadingTrailing;
	private static readonly Regex _newlinesMultiple;
	private static readonly Regex _outDent;
	private static readonly Regex _semiStrictBold;
	private static readonly Regex _semiStrictItalic;
	private static readonly Regex _strictBold;
	private static readonly Regex _strictItalic;
	private readonly Dictionary<string, string> _titles;
	private static readonly Regex _unescapes;
	private readonly Dictionary<string, string> _urls;
	private static readonly string _wholeList;

	#endregion

	#region Constructors

	/// <summary>
	/// Create a new Markdown instance using default options
	/// </summary>
	public MarkdownService() : this(false)
	{
	}

	/// <summary>
	/// Create a new Markdown instance and optionally load options from a configuration
	/// file. There they should be stored in the appSettings section, available options are:
	/// Markdown.StrictBoldItalic (true/false)
	/// Markdown.EmptyElementSuffix (">" or " />" without the quotes)
	/// Markdown.LinkEmails (true/false)
	/// Markdown.AutoNewLines (true/false)
	/// Markdown.AutoHyperlink (true/false)
	/// Markdown.AsteriskIntraWordEmphasis (true/false)
	/// </summary>
	public MarkdownService(bool loadOptionsFromConfigFile)
	{
		_htmlBlocks = new();
		_titles = new();
		_urls = new();
		EmptyElementSuffix = " />";
		LinkEmails = true;
		if (!loadOptionsFromConfigFile)
		{
			return;
		}

		AutoHyperlink = true;
		AutoNewLines = true;
		EmptyElementSuffix = "";
		LinkEmails = true;
		StrictBoldItalic = true;
		AsteriskIntraWordEmphasis = true;
	}

	/// <summary>
	/// Create a new Markdown instance and set the options from the MarkdownOptions object.
	/// </summary>
	public MarkdownService(MarkdownOptions options)
	{
		_htmlBlocks = new();
		_titles = new();
		_urls = new();
		EmptyElementSuffix = " />";
		LinkEmails = true;
		AutoHyperlink = options.AutoHyperlink;
		AutoNewLines = options.AutoNewlines;
		if (!string.IsNullOrEmpty(options.EmptyElementSuffix))
		{
			EmptyElementSuffix = options.EmptyElementSuffix;
		}
		LinkEmails = options.LinkEmails;
		StrictBoldItalic = options.StrictBoldItalic;
		AsteriskIntraWordEmphasis = options.AsteriskIntraWordEmphasis;
	}

	/// <summary>
	/// In the static constructor we'll initialize what stays the same across all transforms.
	/// </summary>
	static MarkdownService()
	{
		// Table of hash values for escaped characters:
		_escapeTable = new Dictionary<string, string>();
		_htmlBlockHash = new("\x1AH\\d+H", RegexOptions.Compiled);
		_leadingWhitespace = new("^[ ]*", RegexOptions.Compiled);
		_newlinesLeadingTrailing = new(@"^\n+|\n+\z", RegexOptions.Compiled);
		_newlinesMultiple = new(@"\n{2,}", RegexOptions.Compiled);
		_linkDef = new($@"
                        ^[ ]{{0,{TabWidth - 1}}}\[([^\[\]]+)\]:  # id = $1
                          [ ]*
                          \n?                   # maybe *one* newline
                          [ ]*
                        <?(\S+?)>?              # url = $2
                          [ ]*
                          \n?                   # maybe one newline
                          [ ]*
                        (?:
                            (?<=\s)             # lookbehind for whitespace
                            [""(]
                            (.+?)               # title = $3
                            ["")]
                            [ ]*
                        )?                      # title is optional
                        (?:\n+|\Z)", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		_blocksHtml = new(GetBlockPattern(), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
		_htmlTokens = new(@"
            (<!--(?:|(?:[^>-]|-[^>])(?:[^-]|-[^-])*)-->)|        # match <!-- foo -->
            (<\?.*?\?>)|                 # match <?foo?> " +
			RepeatString(@" 
            (<[A-Za-z\/!$](?:[^<>]|", NestDepth - 1) + @" 
            (<[A-Za-z\/!$](?:[^<>]"
			+ RepeatString(")*>)", NestDepth) +
			" # match <tag> and </tag>",
			RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		_anchorRef = new($@"
            (                               # wrap whole match in $1
                \[
                    ({GetNestedBracketsPattern()})                   # link text = $2
                \]

                [ ]?                        # one optional space
                (?:\n[ ]*)?                 # one optional newline followed by spaces

                \[
                    (.*?)                   # id = $3
                \]
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_anchorInline = new($@"
                (                           # wrap whole match in $1
                    \[
                        ({GetNestedBracketsPattern()})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({GetNestedParensPattern()})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])           # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_anchorRefShortcut = new(@"
            (                               # wrap whole match in $1
              \[
                 ([^\[\]]+)                 # link text = $2; can't contain [ or ]
              \]
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_imagesRef = new(@"
                    (               # wrap whole match in $1
                    !\[
                        (.*?)       # alt text = $2
                    \]

                    [ ]?            # one optional space
                    (?:\n[ ]*)?     # one optional newline followed by spaces

                    \[
                        (.*?)       # id = $3
                    \]

                    )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_imagesInline = new($@"
              (                     # wrap whole match in $1
                !\[
                    (.*?)           # alt text = $2
                \]
                \s?                 # one optional whitespace character
                \(                  # literal paren
                    [ ]*
                    ({GetNestedParensPattern()})           # href = $3
                    [ ]*
                    (               # $4
                    (['""])       # quote char = $5
                    (.*?)           # title = $6
                    \5              # matching quote
                    [ ]*
                    )?              # title is optional
                \)
              )",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_headerAtx = new(@"
				^(\#{1,6})  # $1 = string of #'s
				[ ]*
				(.+?)       # $2 = Header text (non-greedy)
				[ ]*
				(\{         # $3 = Optional opening { for attributes (start of group)
					[^\}]*  # Anything except } (attributes content)
				\})?        # Optional closing }
				[ ]*
				\#*         # optional closing #'s (not counted)
				\n+",
			RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_headerSetext = new(@"
                ^(.+?)
                [ ]*
                \n
                (=+|-+)     # $1 = string of ='s or -'s
                [ ]*
                \n+",
			RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_horizontalRules = new(@"
            ^[ ]{0,3}         # Leading space
                ([-*_])       # $1: First marker
                (?>           # Repeated marker group
                    [ ]{0,2}  # Zero, one, or two spaces.
                    \1        # Marker character
                ){2,}         # Group repeated at least twice
                [ ]*          # Trailing spaces
                $             # End of line.
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_wholeList = string.Format(@"
            (                               # $1 = whole list
              (                             # $2
                [ ]{{0,{1}}}
                ({0})                       # $3 = first list item marker
                [ ]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ ]*
                    {0}[ ]+
                  )
              )
            )", $"(?:{MarkerUl}|{MarkerOl})", TabWidth - 1);
		_listNested = new("^" + _wholeList,
			RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_listTopLevel = new(@"(?:(?<=\n\n)|\A\n?)" + _wholeList,
			RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_codeBlock = new(string.Format(@"
                    (?:\n\n|\A\n?)
                    (                        # $1 = the code block -- one or more lines, starting with a space
                    (?:
                        (?:[ ]{{{0}}})       # Lines must start with a tab-width of spaces
                        .*\n+
                    )+
                    )
                    ((?=^[ ]{{0,{0}}}[^ \t\n])|\Z) # Lookahead for non-space at line-start, or end of doc",
			TabWidth), RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
		_blockquote = new(@"
            (                           # Wrap whole match in $1
                (
                ^[ ]*>[ ]?              # '>' at the start of a line
                    .+\n                # rest of the first line
                (.+\n)*                 # subsequent consecutive lines
                \n*                     # blanks
                )+
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
		_bold = new(@"(\*\*|__) (?=\S) (.+?[*_]*) (?<=\S) \1",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
		_codeSpan = new(@"
                    (?<![\\`])   # Character before opening ` can't be a backslash or backtick
                    (`+)      # $1 = Opening run of `
                    (?!`)     # and no more backticks -- match the full run
                    (.+?)     # $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
		_italic = new(@"(\*|_) (?=\S) (.+?) (?<=\S) \1",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
		_semiStrictBold = new(@"(?=.[*_]|[*_])(^|(?=\W__|(?!\*)[\W_]\*\*|\w\*\*\w).)(\*\*|__)(?!\2)(?=\S)((?:|.*?(?!\2).)(?=\S_|\w|\S\*\*(?:[\W_]|$)).)(?=__(?:\W|$)|\*\*(?:[^*]|$))\2",
			RegexOptions.Singleline | RegexOptions.Compiled);
		_semiStrictItalic = new(@"(?=.[*_]|[*_])(^|(?=\W_|(?!\*)(?:[\W_]\*|\D\*(?=\w)\D)).)(\*|_)(?!\2\2\2)(?=\S)((?:(?!\2).)*?(?=[^\s_]_|(?=\w)\D\*\D|[^\s*]\*(?:[\W_]|$)).)(?=_(?:\W|$)|\*(?:[^*]|$))\2",
			RegexOptions.Singleline | RegexOptions.Compiled);
		_strictBold = new(@"(^|[\W_])(?:(?!\1)|(?=^))(\*|_)\2(?=\S)(.*?\S)\2\2(?!\2)(?=[\W_]|$)",
			RegexOptions.Singleline | RegexOptions.Compiled);
		_strictItalic = new(@"(^|[\W_])(?:(?!\1)|(?=^))(\*|_)(?=\S)((?:(?!\2).)*?\S)\2(?!\2)(?=[\W_]|$)",
			RegexOptions.Singleline | RegexOptions.Compiled);
		_autolinkBare = new(@"(<|="")?\b(https?|ftp)(://" + CharInsideUrl + "*" + CharEndingUrl + ")(?=$|\\W)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		_endCharRegex = new(CharEndingUrl, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		_autoEmailBare = new(@"(<|="")?(?:mailto:)?([-.\w]+\@[-a-z0-9]+(\.[-a-z0-9]+)*\.[a-z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		_linkEmail = new(@"<
                      (?:mailto:)?
                      (
                        [-.\w]+
                        \@
                        [-a-z0-9]+(\.[-a-z0-9]+)*\.[a-z]+
                      )
                      >", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		_outDent = new("^[ ]{1," + TabWidth + "}", RegexOptions.Multiline | RegexOptions.Compiled);
		_amps = new("&(?!((#[0-9]+)|(#[xX][a-fA-F0-9]+)|([a-zA-Z][a-zA-Z0-9]*));)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		_angles = new(@"<(?![A-Za-z/?\$!])", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		_codeEncoder = new(@"&|<|>|\\|\*|_|\{|\}|\[|\]", RegexOptions.Compiled);
		_unescapes = new("\x1A" + "E\\d+E", RegexOptions.Compiled);
		_invertedEscapeTable = new Dictionary<string, string>();

		// Table of hash value for backslash escaped characters:
		_backslashEscapeTable = new Dictionary<string, string>();

		var backslashPattern = "";

		foreach (var c in @"\`*_{}[]()>#+-.!/:")
		{
			var key = c.ToString();
			var hash = GetHashKey(key, false);
			_escapeTable.Add(key, hash);
			_invertedEscapeTable.Add(hash, key);
			_backslashEscapeTable.Add(@"\" + key, hash);
			backslashPattern += Regex.Escape(@"\" + key) + "|";
		}

		_backslashEscapes = new Regex(backslashPattern.Substring(0, backslashPattern.Length - 1), RegexOptions.Compiled);
	}

	#endregion

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
	public bool AutoNewLines { get; set; }

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

	#region Methods

	public static string ToHtml(string markdown)
	{
		var converter = new MarkdownService();
		return converter.Transform(markdown);
	}

	/// <summary>
	/// Transforms the provided Markdown-formatted text to HTML;
	/// see http://en.wikipedia.org/wiki/Markdown
	/// </summary>
	/// <remarks>
	/// The order in which other subs are called here is
	/// essential. Link and image substitutions need to happen before
	/// EscapeSpecialChars(), so that any *'s or _'s in the a
	/// and img tags get encoded.
	/// </remarks>
	public string Transform(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}

		Setup();

		text = Normalize(text);

		text = HashHtmlBlocks(text);
		text = StripLinkDefinitions(text);
		text = RunBlockGamut(text);
		text = Unescape(text);

		Cleanup();

		return text + "\n";
	}

	private string AnchorInlineEvaluator(Match match)
	{
		var linkText = SaveFromAutoLinking(match.Groups[2].Value);
		var url = match.Groups[3].Value;
		var title = match.Groups[6].Value;

		if (url.StartsWith("<") && url.EndsWith(">"))
		{
			url = url.Substring(1, url.Length - 2); // remove <>'s surrounding URL, if present            
		}

		url = AttributeSafeUrl(url);

		var result = $"<a href=\"{url}\"";

		if (!string.IsNullOrEmpty(title))
		{
			title = AttributeEncode(title);
			title = EscapeBoldItalic(title);
			result += $" title=\"{title}\"";
		}

		result += $">{linkText}</a>";
		return result;
	}

	private string AnchorRefEvaluator(Match match)
	{
		var wholeMatch = match.Groups[1].Value;
		var linkText = SaveFromAutoLinking(match.Groups[2].Value);
		var linkId = match.Groups[3].Value.ToLowerInvariant();

		string result;

		// for shortcut links like [this][].
		if (linkId?.Length == 0)
		{
			linkId = linkText.ToLowerInvariant();
		}

		if (_urls.ContainsKey(linkId))
		{
			var url = _urls[linkId];

			url = AttributeSafeUrl(url);

			result = "<a href=\"" + url + "\"";

			if (_titles.ContainsKey(linkId))
			{
				var title = AttributeEncode(_titles[linkId]);
				title = AttributeEncode(EscapeBoldItalic(title));
				result += " title=\"" + title + "\"";
			}

			result += ">" + linkText + "</a>";
		}
		else
		{
			result = wholeMatch;
		}

		return result;
	}

	private string AnchorRefShortcutEvaluator(Match match)
	{
		var wholeMatch = match.Groups[1].Value;
		var linkText = SaveFromAutoLinking(match.Groups[2].Value);
		var linkId = Regex.Replace(linkText.ToLowerInvariant(), @"[ ]*\n[ ]*", " "); // lower case and remove newlines / extra spaces

		string result;

		if (_urls.ContainsKey(linkId))
		{
			var url = _urls[linkId];

			url = AttributeSafeUrl(url);

			result = "<a href=\"" + url + "\"";

			if (_titles.ContainsKey(linkId))
			{
				var title = AttributeEncode(_titles[linkId]);
				title = EscapeBoldItalic(title);
				result += " title=\"" + title + "\"";
			}

			result += ">" + linkText + "</a>";
		}
		else
		{
			result = wholeMatch;
		}

		return result;
	}

	private static string AttributeEncode(string s)
	{
		return s.Replace(">", "&gt;").Replace("<", "&lt;").Replace("\"", "&quot;").Replace("'", "&#39;");
	}

	private static string AttributeSafeUrl(string s)
	{
		s = AttributeEncode(s);
		foreach (var c in "*_:()[]")
		{
			s = s.Replace(c.ToString(), _escapeTable[c.ToString()]);
		}
		return s;
	}

	private string AtxHeaderEvaluator(Match match)
	{
		var level = match.Groups[1].Value.Length;
		var headerText = match.Groups[2].Value.Trim();
		var attrs = string.Empty;

		// If attributes group exists ($3)
		if (match.Groups[3].Success)
		{
			// Includes { and }
			var attrBlock = match.Groups[3].Value;
			attrs = ParseAttributes(attrBlock);
		}

		var inner = RunSpanGamut(headerText);
		return string.Format("<h{1}{2}>{0}</h{1}>\n\n", inner, level, attrs);
	}

	private string BlockQuoteEvaluator(Match match)
	{
		var bq = match.Groups[1].Value;

		bq = Regex.Replace(bq, "^[ ]*>[ ]?", "", RegexOptions.Multiline); // trim one level of quoting
		bq = Regex.Replace(bq, "^[ ]+$", "", RegexOptions.Multiline); // trim whitespace-only lines
		bq = RunBlockGamut(bq); // recurse

		bq = Regex.Replace(bq, "^", "  ", RegexOptions.Multiline);

		// These leading spaces screw with <pre> content, so we need to fix that:
		bq = Regex.Replace(bq, @"(\s*<pre>.+?</pre>)", BlockQuoteEvaluator2, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

		bq = $"<blockquote>\n{bq}\n</blockquote>";
		var key = GetHashKey(bq, true);
		_htmlBlocks[key] = bq;

		return "\n\n" + key + "\n\n";
	}

	private string BlockQuoteEvaluator2(Match match)
	{
		return Regex.Replace(match.Groups[1].Value, "^  ", "", RegexOptions.Multiline);
	}

	private void Cleanup()
	{
		Setup();
	}

	private string CodeBlockEvaluator(Match match)
	{
		var codeBlock = match.Groups[1].Value;

		codeBlock = EncodeCode(Outdent(codeBlock));
		codeBlock = _newlinesLeadingTrailing.Replace(codeBlock, "");

		return string.Concat("\n\n<pre><code>", codeBlock, "\n</code></pre>\n\n");
	}

	private string CodeSpanEvaluator(Match match)
	{
		var span = match.Groups[2].Value;
		span = Regex.Replace(span, "^[ ]*", ""); // leading whitespace
		span = Regex.Replace(span, "[ ]*$", ""); // trailing whitespace
		span = EncodeCode(span);
		span = SaveFromAutoLinking(span); // to prevent auto-linking. Not necessary in code *blocks*, but in code spans.

		return string.Concat("<code>", span, "</code>");
	}

	/// <summary>
	/// Turn Markdown link shortcuts into HTML anchor tags
	/// </summary>
	/// <remarks>
	/// [link text](url "title")
	/// [link text][id]
	/// [id]
	/// </remarks>
	private string DoAnchors(string text)
	{
		if (!text.Contains("["))
		{
			return text;
		}

		// First, handle reference-style links: [link text] [id]
		text = _anchorRef.Replace(text, AnchorRefEvaluator);

		// Next, inline-style links: [link text](url "optional title") or [link text](url "optional title")
		text = _anchorInline.Replace(text, AnchorInlineEvaluator);

		//  Last, handle reference-style shortcuts: [link text]
		//  These must come last in case you've also got [link test][1]
		//  or [link test](/foo)
		return _anchorRefShortcut.Replace(text, AnchorRefShortcutEvaluator);
	}

	/// <summary>
	/// Turn angle-delimited URLs into HTML anchor tags
	/// </summary>
	/// <remarks>
	/// &lt;http://www.example.com&gt;
	/// </remarks>
	private string DoAutoLinks(string text)
	{
		if (AutoHyperlink)
		{
			// fixup arbitrary URLs by adding Markdown < > so they get linked as well
			// note that at this point, all other URL in the text are already hyperlinked as <a href=""></a>
			// *except* for the <http://www.foo.com> case
			text = _autolinkBare.Replace(text, HandleTrailingParens);
		}

		// Hyperlinks: <http://foo.com>
		text = Regex.Replace(text, "<((https?|ftp):[^'\">\\s]+)>", HyperlinkEvaluator);

		if (LinkEmails)
		{
			// Email addresses: <address@domain.foo> or <mailto:address@domain.foo>
			// Also allow "address@domain.foo" and "mailto:address@domain.foo", without the <>
			//text = _autoEmailBare.Replace(text, EmailBareLinkEvaluator);
			text = _linkEmail.Replace(text, EmailEvaluator);
		}

		return text;
	}

	/// <summary>
	/// Turn Markdown > quoted blocks into HTML blockquote blocks
	/// </summary>
	private string DoBlockQuotes(string text)
	{
		return _blockquote.Replace(text, BlockQuoteEvaluator);
	}

	/// <summary>
	/// /// Turn Markdown 4-space indented code into HTML pre code blocks
	/// </summary>
	private string DoCodeBlocks(string text)
	{
		return _codeBlock.Replace(text, CodeBlockEvaluator);
	}

	/// <summary>
	/// Turn Markdown `code spans` into HTML code tags
	/// </summary>
	private string DoCodeSpans(string text)
	{
		//    * You can use multiple backticks as the delimiters if you want to
		//        include literal backticks in the code span. So, this input:
		//
		//        Just type ``foo `bar` baz`` at the prompt.
		//
		//        Will translate to:
		//
		//          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
		//
		//        There's no arbitrary limit to the number of backticks you
		//        can use as delimters. If you need three consecutive backticks
		//        in your code, use four for delimiters, etc.
		//
		//    * You can use spaces to get literal backticks at the edges:
		//
		//          ... type `` `bar` `` ...
		//
		//        Turns to:
		//
		//          ... type <code>`bar`</code> ...         
		//

		return _codeSpan.Replace(text, CodeSpanEvaluator);
	}

	/// <summary>
	/// Turn markdown line breaks (two space at end of line) into HTML break tags
	/// </summary>
	private string DoHardBreaks(string text)
	{
		if (AutoNewLines)
		{
			return Regex.Replace(text, @"\n", $"<br{EmptyElementSuffix}\n");
		}
		return Regex.Replace(text, @" {2,}\n", $"<br{EmptyElementSuffix}\n");
	}

	/// <summary>
	/// Turn Markdown headers into HTML header tags
	/// </summary>
	/// <remarks>
	/// <para>
	/// Header 1
	/// ========
	/// </para>
	/// <para>
	/// Header 2
	/// --------
	/// </para>
	/// <para>
	/// # Header 1
	/// ## Header 2
	/// ## Header 2 with closing hashes ##
	/// ...
	/// ###### Header 6
	/// </para>
	/// </remarks>
	private string DoHeaders(string text)
	{
		text = _headerSetext.Replace(text, SetextHeaderEvaluator);
		return _headerAtx.Replace(text, AtxHeaderEvaluator);
	}

	/// <summary>
	/// Turn Markdown horizontal rules into HTML hr tags
	/// </summary>
	/// <remarks>
	/// ***
	/// * * *
	/// ---
	/// - - -
	/// </remarks>
	private string DoHorizontalRules(string text)
	{
		return _horizontalRules.Replace(text, "<hr" + EmptyElementSuffix + "\n");
	}

	/// <summary>
	/// Turn Markdown image shortcuts into HTML img tags.
	/// </summary>
	/// <remarks>
	/// ![alt text][id]
	/// ![alt text](url "optional title")
	/// </remarks>
	private string DoImages(string text)
	{
		if (!text.Contains("!["))
		{
			return text;
		}

		// First, handle reference-style labeled images: ![alt text][id]
		text = _imagesRef.Replace(text, ImageReferenceEvaluator);

		// Next, handle inline images:  ![alt text](url "optional title")
		// Don't forget: encode * and _
		return _imagesInline.Replace(text, ImageInlineEvaluator);
	}

	/// <summary>
	/// Turn Markdown *italics* and **bold** into HTML strong and em tags
	/// </summary>
	private string DoItalicsAndBold(string text)
	{
		if (!(text.Contains("*") || text.Contains("_")))
		{
			return text;
		}

		// <strong> must go first, then <em>
		if (StrictBoldItalic)
		{
			if (AsteriskIntraWordEmphasis)
			{
				text = _semiStrictBold.Replace(text, "$1<strong>$3</strong>");
				text = _semiStrictItalic.Replace(text, "$1<em>$3</em>");
			}
			else
			{
				text = _strictBold.Replace(text, "$1<strong>$3</strong>");
				text = _strictItalic.Replace(text, "$1<em>$3</em>");
			}
		}
		else
		{
			text = _bold.Replace(text, "<strong>$2</strong>");
			text = _italic.Replace(text, "<em>$2</em>");
		}
		return text;
	}

	/// <summary>
	/// Turn Markdown lists into HTML ul and ol and li tags
	/// </summary>
	private string DoLists(string text)
	{
		// We use a different prefix before nested lists than top-level lists.
		// See extended comment in _ProcessListItems().
		if (_listLevel > 0)
		{
			return _listNested.Replace(text, ListEvaluator);
		}
		return _listTopLevel.Replace(text, ListEvaluator);
	}

	private static string EmailBareLinkEvaluator(Match match)
	{
		// We matched an opening <, so it's already enclosed
		if (match.Groups[1].Success)
		{
			return match.Value;
		}
		return "<" + match.Value + ">";
	}

	private string EmailEvaluator(Match match)
	{
		var email = Unescape(match.Groups[1].Value);

		//
		//    Input: an email address, e.g. "foo@example.com"
		//
		//    Output: the email address as a mailto link, with each character
		//            of the address encoded as either a decimal or hex entity, in
		//            the hopes of foiling most address harvesting spam bots. E.g.:
		//
		//      <a href="&#x6D;&#97;&#105;&#108;&#x74;&#111;:&#102;&#111;&#111;&#64;&#101;
		//        x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;">&#102;&#111;&#111;
		//        &#64;&#101;x&#x61;&#109;&#x70;&#108;&#x65;&#x2E;&#99;&#111;&#109;</a>
		//
		//    Based by a filter by Matthew Wickline, posted to the BBEdit-Talk
		//    mailing list: <http://tinyurl.com/yu7ue>
		//
		email = "mailto:" + email;

		// leave ':' alone (to spot mailto: later) 
		email = EncodeEmailAddress(email);

		email = string.Format("<a href=\"{0}\">{0}</a>", email);

		// strip the mailto: from the visible part
		return Regex.Replace(email, "\">.+?:", "\">");
	}

	/// <summary>
	/// Encode any ampersands (that aren't part of an HTML entity) and left or right angle brackets
	/// </summary>
	private string EncodeAmpsAndAngles(string s)
	{
		s = _amps.Replace(s, "&amp;");
		return _angles.Replace(s, "&lt;");
	}

	/// <summary>
	/// Encode/escape certain Markdown characters inside code blocks and spans where they are literals
	/// </summary>
	private string EncodeCode(string code)
	{
		return _codeEncoder.Replace(code, EncodeCodeEvaluator);
	}

	private string EncodeCodeEvaluator(Match match)
	{
		switch (match.Value)
		{
			// Encode all ampersands; HTML entities are not
			// entities within a Markdown code span.
			case "&":
				return "&amp;";

			// Do the angle bracket song and dance
			case "<":
				return "&lt;";
			case ">":
				return "&gt;";

			// escape characters that are magic in Markdown
			default:
				return _escapeTable[match.Value];
		}
	}

	/// <summary>
	/// encodes email address randomly
	/// roughly 10% raw, 45% hex, 45% dec
	/// note that @ is always encoded and : never is
	/// </summary>
	private string EncodeEmailAddress(string addr)
	{
		var sb = new StringBuilder(addr.Length * 5);
		var rand = new Random();
		int r;
		foreach (var c in addr)
		{
			r = rand.Next(1, 100);
			if (((r > 90) || (c == ':')) && (c != '@'))
			{
				sb.Append(c); // m
			}
			else if (r < 45)
			{
				sb.AppendFormat("&#x{0:x};", (int) c); // &#x6D
			}
			else
			{
				sb.AppendFormat("&#{0};", (int) c); // &#109
			}
		}
		return sb.ToString();
	}

	/// <summary>
	/// Encodes any escaped characters such as \`, \*, \[ etc
	/// </summary>
	private string EscapeBackslashes(string s)
	{
		return _backslashEscapes.Replace(s, EscapeBackslashesEvaluator);
	}

	private string EscapeBackslashesEvaluator(Match match)
	{
		return _backslashEscapeTable[match.Value];
	}

	/// <summary>
	/// escapes Bold [ * ] and Italic [ _ ] characters
	/// </summary>
	private string EscapeBoldItalic(string s)
	{
		s = s.Replace("*", _escapeTable["*"]);
		return s.Replace("_", _escapeTable["_"]);
	}

	// This prevents the creation of horribly broken HTML when some syntax ambiguities
	// collide. It likely still doesn't do what the user meant, but at least we're not
	// outputting garbage.
	private string EscapeImageAltText(string s)
	{
		s = EscapeBoldItalic(s);
		return Regex.Replace(s, @"[\[\]()]", m => _escapeTable[m.ToString()]);
	}

	/// <summary>
	/// Within tags -- meaning between &lt; and &gt; -- encode [\ ` * _] so they
	/// don't conflict with their use in Markdown for code, italics and strong.
	/// We're replacing each such character with its corresponding hash
	/// value; this is likely overkill, but it should prevent us from colliding
	/// with the escape values by accident.
	/// </summary>
	private string EscapeSpecialCharsWithinTagAttributes(string text)
	{
		var tokens = TokenizeHtml(text);

		// now, rebuild text from the tokens
		var sb = new StringBuilder(text.Length);

		foreach (var token in tokens)
		{
			var value = token.Value;

			if (token.Type == TokenType.Tag)
			{
				value = value.Replace(@"\", _escapeTable[@"\"]);

				if (AutoHyperlink && value.StartsWith("<!")) // escape slashes in comments to prevent autolinking there -- https://meta.stackexchange.com/questions/95987/html-comment-containing-url-breaks-if-followed-by-another-html-comment
				{
					value = value.Replace("/", _escapeTable["/"]);
				}

				value = Regex.Replace(value, "(?<=.)</?code>(?=.)", _escapeTable["`"]);
				value = EscapeBoldItalic(value);
			}

			sb.Append(value);
		}

		return sb.ToString();
	}

	/// <summary>
	/// splits on two or more newlines, to form "paragraphs";
	/// each paragraph is then unhashed (if it is a hash and unhashing isn't turned off) or wrapped in HTML p tag
	/// </summary>
	private string FormParagraphs(string text, bool unhash = true, bool createParagraphs = true)
	{
		// split on two or more newlines
		var grafs = _newlinesMultiple.Split(_newlinesLeadingTrailing.Replace(text, ""));

		for (var i = 0; i < grafs.Length; i++)
		{
			if (grafs[i].Contains("\x1AH"))
			{
				// unhashify HTML blocks
				if (unhash)
				{
					var sanityCheck = 50; // just for safety, guard against an infinite loop
					var keepGoing = true; // as long as replacements where made, keep going
					while (keepGoing && (sanityCheck > 0))
					{
						keepGoing = false;
						grafs[i] = _htmlBlockHash.Replace(grafs[i], match =>
						{
							keepGoing = true;
							return _htmlBlocks[match.Value];
						});
						sanityCheck--;
					}
					/* if (keepGoing)
					{
						// Logging of an infinite loop goes here.
						// If such a thing should happen, please open a new issue on http://code.google.com/p/markdownsharp/
						// with the input that caused it.
					}*/
				}
			}
			else
			{
				// do span level processing inside the block, then wrap result in <p> tags
				grafs[i] = _leadingWhitespace.Replace(RunSpanGamut(grafs[i]), createParagraphs ? "<p>" : "") + (createParagraphs ? "</p>" : "");
			}
		}

		return string.Join("\n\n", grafs);
	}

	/// <summary>
	/// derived pretty much verbatim from PHP Markdown
	/// </summary>
	private static string GetBlockPattern()
	{
		// Hashify HTML blocks:
		// We only want to do this for block-level HTML tags, such as headers,
		// lists, and tables. That's because we still want to wrap <p>s around
		// "paragraphs" that are wrapped in non-block-level tags, such as anchors,
		// phrase emphasis, and spans. The list of tags we're looking for is
		// hard-coded:
		//
		// *  List "a" is made of tags which can be both inline or block-level.
		//    These will be treated block-level when the start tag is alone on 
		//    its line, otherwise they're not matched here and will be taken as 
		//    inline later.
		// *  List "b" is made of tags which are always block-level;
		//
		const string blockTagsA = "ins|del";
		const string blockTagsB = "p|div|h[1-6]|blockquote|pre|table|dl|ol|ul|address|script|noscript|form|fieldset|iframe|math";

		// Regular expression for the content of a block tag.
		const string attr = @"
            (?>				            # optional tag attributes
              \s			            # starts with whitespace
              (?>
                [^>""/]+	            # text outside quotes
              |
                /+(?!>)		            # slash not followed by >
              |
                ""[^""]*""		        # text inside double quotes (tolerate >)
              |
                '[^']*'	                # text inside single quotes (tolerate >)
              )*
            )?	
            ";

		var content = RepeatString(@"
                (?>
                  [^<]+			        # content without tag
                |
                  <\2			        # nested opening tag
                    " + attr + @"       # attributes
                  (?>
                      />
                  |
                      >", NestDepth) + // end of opening tag
			".*?" + // last level nested tag content
			RepeatString(@"
                      </\2\s*>	        # closing nested tag
                  )
                  |				
                  <(?!/\2\s*>           # other tags with a different name
                  )
                )*", NestDepth);

		var content2 = content.Replace(@"\2", @"\3");

		// First, look for nested blocks, e.g.:
		// 	<div>
		// 		<div>
		// 		tags for inner block must be indented.
		// 		</div>
		// 	</div>
		//
		// The outermost tags must start at the left margin for this to match, and
		// the inner nested divs must be indented.
		// We need to do this before the next, more liberal match, because the next
		// match will start at the first `<div>` and stop at the first `</div>`.
		var pattern = @"
            (?>
                  (?>
                    (?<=\n)     # Starting at the beginning of a line
                    |           # or
                    \A\n?       # the beginning of the doc
                  )
                  (             # save in $1

                    # Match from `\n<tag>` to `</tag>\n`, handling nested tags 
                    # in between.
                      
                        <($block_tags_b_re)   # start tag = $2
                        $attr>                # attributes followed by > and \n
                        $content              # content, support nesting
                        </\2>                 # the matching end tag
                        [ ]*                  # trailing spaces
                        (?=\n+|\Z)            # followed by a newline or end of document

                  | # Special version for tags of group a.

                        <($block_tags_a_re)   # start tag = $3
                        $attr>[ ]*\n          # attributes followed by >
                        $content2             # content, support nesting
                        </\3>                 # the matching end tag
                        [ ]*                  # trailing spaces
                        (?=\n+|\Z)            # followed by a newline or end of document
                      
                  | # Special case just for <hr />. It was easier to make a special 
                    # case than to make the other regex more complicated.
                  
                        [ ]{0,$less_than_tab}
                        <hr
                        $attr                 # attributes
                        /?>                   # the matching end tag
                        [ ]*
                        (?=\n{2,}|\Z)         # followed by a blank line or end of document
                  
                  | # Special case for standalone HTML comments:
                  
                      (?<=\n\n|\A)            # preceded by a blank line or start of document
                      [ ]{0,$less_than_tab}
                      (?s:
                        <!--(?:|(?:[^>-]|-[^>])(?:[^-]|-[^-])*)-->
                      )
                      [ ]*
                      (?=\n{2,}|\Z)            # followed by a blank line or end of document
                  
                  | # PHP and ASP-style processor instructions (<? and <%)
                  
                      [ ]{0,$less_than_tab}
                      (?s:
                        <([?%])                # $4
                        .*?
                        \4>
                      )
                      [ ]*
                      (?=\n{2,}|\Z)            # followed by a blank line or end of document
                      
                  )
            )";

		pattern = pattern.Replace("$less_than_tab", (TabWidth - 1).ToString());
		pattern = pattern.Replace("$block_tags_b_re", blockTagsB);
		pattern = pattern.Replace("$block_tags_a_re", blockTagsA);
		pattern = pattern.Replace("$attr", attr);
		pattern = pattern.Replace("$content2", content2);
		return pattern.Replace("$content", content);
	}

	private static string GetHashKey(string s, bool isHtmlBlock)
	{
		var delim = isHtmlBlock ? 'H' : 'E';
		return "\x1A" + delim + Math.Abs(s.GetHashCode()) + delim;
	}

	/// <summary>
	/// Reusable pattern to match balanced [brackets]. See Friedl's
	/// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
	/// </summary>
	private static string GetNestedBracketsPattern()
	{
		// in other words [this] and [this[also]] and [this[also[too]]]
		// up to _nestDepth
		if (_nestedBracketsPattern == null)
		{
			_nestedBracketsPattern =
				RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", NestDepth) + RepeatString(
					@" \]
                    )*"
					, NestDepth);
		}

		return _nestedBracketsPattern;
	}

	/// <summary>
	/// Reusable pattern to match balanced (parens). See Friedl's
	/// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
	/// </summary>
	private static string GetNestedParensPattern()
	{
		// in other words (this) and (this(also)) and (this(also(too)))
		// up to _nestDepth
		if (_nestedParensPattern == null)
		{
			_nestedParensPattern =
				RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", NestDepth) + RepeatString(
					@" \)
                    )*"
					, NestDepth);
		}

		return _nestedParensPattern;
	}

	private static string HandleTrailingParens(Match match)
	{
		// The first group is essentially a negative lookbehind -- if there's a < or a =", we don't touch this.
		// We're not using a *real* lookbehind, because of links with in links, like <a href="http://web.archive.org/web/20121130000728/http://www.google.com/">
		// With a real lookbehind, the full link would never be matched, and thus the http://www.google.com *would* be matched.
		// With the simulated lookbehind, the full link *is* matched (just not handled, because of this early return), causing
		// the google link to not be matched again.
		if (match.Groups[1].Success)
		{
			return match.Value;
		}

		var protocol = match.Groups[2].Value;
		var link = match.Groups[3].Value;
		if (!link.EndsWith(")"))
		{
			return "<" + protocol + link + ">";
		}
		var level = 0;
		foreach (Match c in Regex.Matches(link, "[()]"))
		{
			if (c.Value == "(")
			{
				if (level <= 0)
				{
					level = 1;
				}
				else
				{
					level++;
				}
			}
			else
			{
				level--;
			}
		}
		var tail = "";
		if (level < 0)
		{
			link = Regex.Replace(link, @"\){1," + -level + "}$", m =>
			{
				tail = m.Value;
				return "";
			});
		}
		if (tail.Length > 0)
		{
			var lastChar = link[link.Length - 1];
			if (!_endCharRegex.IsMatch(lastChar.ToString()))
			{
				tail = lastChar + tail;
				link = link.Substring(0, link.Length - 1);
			}
		}
		return "<" + protocol + link + ">" + tail;
	}

	/// <summary>
	/// replaces any block-level HTML blocks with hash entries
	/// </summary>
	private string HashHtmlBlocks(string text)
	{
		return _blocksHtml.Replace(text, HtmlEvaluator);
	}

	private string HtmlEvaluator(Match match)
	{
		var text = match.Groups[1].Value;
		var key = GetHashKey(text, true);
		_htmlBlocks[key] = text;

		return string.Concat("\n\n", key, "\n\n");
	}

	private string HyperlinkEvaluator(Match match)
	{
		var link = match.Groups[1].Value;
		var url = AttributeSafeUrl(link);
		return $"<a href=\"{url}\">{link}</a>";
	}

	private string ImageInlineEvaluator(Match match)
	{
		var alt = match.Groups[2].Value;
		var url = match.Groups[3].Value;
		var title = match.Groups[6].Value;

		if (url.StartsWith("<") && url.EndsWith(">"))
		{
			url = url.Substring(1, url.Length - 2); // Remove <>'s surrounding URL, if present
		}

		return ImageTag(url, alt, title);
	}

	private string ImageReferenceEvaluator(Match match)
	{
		var wholeMatch = match.Groups[1].Value;
		var altText = match.Groups[2].Value;
		var linkId = match.Groups[3].Value.ToLowerInvariant();

		// for shortcut links like ![this][].
		if (linkId?.Length == 0)
		{
			linkId = altText.ToLowerInvariant();
		}

		if (_urls.ContainsKey(linkId))
		{
			var url = _urls[linkId];
			string title = null;

			if (_titles.ContainsKey(linkId))
			{
				title = _titles[linkId];
			}

			return ImageTag(url, altText, title);
		}

		// If there's no such link ID, leave intact:
		return wholeMatch;
	}

	private string ImageTag(string url, string altText, string title)
	{
		altText = EscapeImageAltText(AttributeEncode(altText));
		url = AttributeSafeUrl(url);
		var result = $"<img src=\"{url}\" alt=\"{altText}\"";
		if (!string.IsNullOrEmpty(title))
		{
			title = AttributeEncode(EscapeBoldItalic(title));
			result += $" title=\"{title}\"";
		}
		result += EmptyElementSuffix;
		return result;
	}

	private string LinkEvaluator(Match match)
	{
		var linkId = match.Groups[1].Value.ToLowerInvariant();
		_urls[linkId] = EncodeAmpsAndAngles(match.Groups[2].Value);

		if (match.Groups[3]?.Length > 0)
		{
			_titles[linkId] = match.Groups[3].Value.Replace("\"", "&quot;");
		}

		return "";
	}

	private string ListEvaluator(Match match)
	{
		var list = match.Groups[1].Value;
		var marker = match.Groups[3].Value;
		var listType = Regex.IsMatch(marker, MarkerUl) ? "ul" : "ol";
		var start = "";
		if (listType == "ol")
		{
			int.TryParse(marker.Substring(0, marker.Length - 1), out var firstNumber);
			if ((firstNumber != 1) && (firstNumber != 0))
			{
				start = " start=\"" + firstNumber + "\"";
			}
		}

		var result = ProcessListItems(list, listType == "ul" ? MarkerUl : MarkerOl);

		return string.Format("<{0}{1}>\n{2}</{0}>\n", listType, start, result);
	}

	/// <summary>
	/// convert all tabs to _tabWidth spaces;
	/// standardizes line endings from DOS (CR LF) or Mac (CR) to UNIX (LF);
	/// makes sure text ends with a couple of newlines;
	/// removes any blank lines (only spaces) in the text
	/// </summary>
	private string Normalize(string text)
	{
		var output = new StringBuilder(text.Length);
		var line = new StringBuilder();
		var valid = false;

		for (var i = 0; i < text.Length; i++)
		{
			switch (text[i])
			{
				case '\n':
					if (valid)
					{
						output.Append(line);
					}
					output.Append('\n');
					line.Length = 0;
					valid = false;
					break;
				case '\r':
					if ((i < (text.Length - 1)) && (text[i + 1] != '\n'))
					{
						if (valid)
						{
							output.Append(line);
						}
						output.Append('\n');
						line.Length = 0;
						valid = false;
					}
					break;
				case '\t':
					var width = TabWidth - (line.Length % TabWidth);
					for (var k = 0; k < width; k++)
					{
						line.Append(' ');
					}
					break;
				case '\x1A':
					break;
				default:
					if (!valid && (text[i] != ' '))
					{
						valid = true;
					}
					line.Append(text[i]);
					break;
			}
		}

		if (valid)
		{
			output.Append(line);
		}
		output.Append('\n');

		// add two newlines to the end before return
		return output.Append("\n\n").ToString();
	}

	/// <summary>
	/// Remove one level of line-leading spaces
	/// </summary>
	private string Outdent(string block)
	{
		return _outDent.Replace(block, "");
	}

	private string ParseAttributes(string attrBlock)
	{
		// Remove { and } and trim
		var content = attrBlock.Substring(1, attrBlock.Length - 2).Trim();
		if (string.IsNullOrEmpty(content))
		{
			return string.Empty;
		}

		var classes = new List<string>();
		var id = string.Empty;
		var otherAttrs = new List<string>();

		// Split by spaces (but respect quoted values)
		// Simple split for now; handles most cases without nested quotes in values
		var parts = content.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var part in parts)
		{
			if (part.StartsWith(".") && (part.Length > 1))
			{
				classes.Add(HttpUtility.HtmlEncode(part.Substring(1)));
			}
			else if (part.StartsWith("#") && (part.Length > 1))
			{
				id = HttpUtility.HtmlEncode(part.Substring(1));
			}
			else if (part.Contains("="))
			{
				// Basic key=value, quote value if needed
				var eq = part.IndexOf('=');
				var key = part.Substring(0, eq);
				var val = part.Substring(eq + 1);

				// Remove surrounding quotes if present
				if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
				{
					val = val.Substring(1, val.Length - 2);
				}
				otherAttrs.Add($"{HttpUtility.HtmlEncode(key)}=\"{HttpUtility.HtmlEncode(val)}\"");
			}

			// Ignore invalid parts
		}

		var attrList = new List<string>();
		if (!string.IsNullOrEmpty(id))
		{
			attrList.Add("id=\"" + id + "\"");
		}
		if (classes.Count > 0)
		{
			attrList.Add("class=\"" + string.Join(" ", classes) + "\"");
		}
		attrList.AddRange(otherAttrs);

		return attrList.Count > 0 ? " " + string.Join(" ", attrList) : string.Empty;
	}

	/// <summary>
	/// Process the contents of a single ordered or unordered list, splitting it
	/// into individual list items.
	/// </summary>
	private string ProcessListItems(string list, string marker)
	{
		// The listLevel global keeps track of when we're inside a list.
		// Each time we enter a list, we increment it; when we leave a list,
		// we decrement. If it's zero, we're not in a list anymore.

		// We do this because when we're not inside a list, we want to treat
		// something like this:

		//    I recommend upgrading to version
		//    8. Oops, now this line is treated
		//    as a sub-list.

		// As a single paragraph, despite the fact that the second line starts
		// with a digit-period-space sequence.

		// Whereas when we're inside a list (or sub-list), that line will be
		// treated as the start of a sub-list. What a kludge, huh? This is
		// an aspect of Markdown's syntax that's hard to parse perfectly
		// without resorting to mind-reading. Perhaps the solution is to
		// change the syntax rules such that sub-lists must start with a
		// starting cardinal number; e.g. "1." or "a.".

		_listLevel++;

		// Trim trailing blank lines:
		list = Regex.Replace(list, @"\n{2,}\z", "\n");

		var pattern = string.Format(
			@"(^[ ]*)                    # leading whitespace = $1
                ({0}) [ ]+                 # list marker = $2
                ((?s:.+?)                  # list item text = $3
                (\n+))      
                (?= (\z | \1 ({0}) [ ]+))", marker);

		var lastItemHadADoubleNewline = false;

		// has to be a closure, so subsequent invocations can share the bool
		string ListItemEvaluator(Match match)
		{
			var item = match.Groups[3].Value;

			var endsWithDoubleNewline = item.EndsWith("\n\n");
			var containsDoubleNewline = endsWithDoubleNewline || item.Contains("\n\n");

			var loose = containsDoubleNewline || lastItemHadADoubleNewline;

			// we could correct any bad indentation here..
			item = RunBlockGamut(Outdent(item) + "\n", false, loose);

			lastItemHadADoubleNewline = endsWithDoubleNewline;
			return $"<li>{item}</li>\n";
		}

		list = Regex.Replace(list, pattern, ListItemEvaluator,
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
		_listLevel--;
		return list;
	}

	/// <summary>
	/// this is to emulate what's evailable in PHP
	/// </summary>
	private static string RepeatString(string text, int count)
	{
		var sb = new StringBuilder(text.Length * count);
		for (var i = 0; i < count; i++)
		{
			sb.Append(text);
		}
		return sb.ToString();
	}

	/// <summary>
	/// Perform transformations that form block-level tags like paragraphs, headers, and list items.
	/// </summary>
	private string RunBlockGamut(string text, bool unhash = true, bool createParagraphs = true)
	{
		text = DoHeaders(text);
		text = DoHorizontalRules(text);
		text = DoLists(text);
		text = DoCodeBlocks(text);
		text = DoBlockQuotes(text);

		// We already ran HashHTMLBlocks() before, in Markdown(), but that
		// was to escape raw HTML in the original Markdown source. This time,
		// we're escaping the markup we've just created, so that we don't wrap
		// <p> tags around block-level tags.
		text = HashHtmlBlocks(text);

		return FormParagraphs(text, unhash, createParagraphs);
	}

	/// <summary>
	/// Perform transformations that occur *within* block-level tags like paragraphs, headers, and list items.
	/// </summary>
	private string RunSpanGamut(string text)
	{
		text = DoCodeSpans(text);
		text = EscapeSpecialCharsWithinTagAttributes(text);
		text = EscapeBackslashes(text);

		// Images must come first, because ![foo][f] looks like an anchor.
		text = DoImages(text);
		text = DoAnchors(text);

		// Must come after DoAnchors(), because you can use < and >
		// delimiters in inline links like [this](<url>).
		text = DoAutoLinks(text);

		text = text.Replace(AutoLinkPreventionMarker, "://");

		text = EncodeAmpsAndAngles(text);
		text = DoItalicsAndBold(text);
		return DoHardBreaks(text);
	}

	private string SaveFromAutoLinking(string s)
	{
		return s.Replace("://", AutoLinkPreventionMarker);
	}

	private string SetextHeaderEvaluator(Match match)
	{
		var header = match.Groups[1].Value;
		var level = match.Groups[2].Value.StartsWith("=") ? 1 : 2;
		return string.Format("<h{1}>{0}</h{1}>\n\n", RunSpanGamut(header), level);
	}

	private void Setup()
	{
		// Clear the global hashes. If we don't clear these, you get conflicts
		// from other articles when generating a page which contains more than
		// one article (e.g. an index page that shows the N most recent
		// articles):
		_urls.Clear();
		_titles.Clear();
		_htmlBlocks.Clear();
		_listLevel = 0;
	}

	/// <summary>
	/// Strips link definitions from text, stores the URLs and titles in hash references.
	/// </summary>
	/// <remarks>
	/// ^[id]: url "optional title"
	/// </remarks>
	private string StripLinkDefinitions(string text)
	{
		return _linkDef.Replace(text, LinkEvaluator);
	}

	/// <summary>
	/// returns an array of HTML tokens comprising the input string. Each token is
	/// either a tag (possibly with nested, tags contained therein, such
	/// as &lt;a href="&lt;MTFoo&gt;"&gt;, or a run of text between tags. Each element of the
	/// array is a two-element array; the first is either 'tag' or 'text'; the second is
	/// the actual value.
	/// </summary>
	private List<Token> TokenizeHtml(string text)
	{
		var pos = 0;
		var tagStart = 0;
		var tokens = new List<Token>();

		// this regex is derived from the _tokenize() subroutine in Brad Choate's MTRegex plugin.
		// http://www.bradchoate.com/past/mtregex.php
		foreach (Match m in _htmlTokens.Matches(text))
		{
			tagStart = m.Index;

			if (pos < tagStart)
			{
				tokens.Add(new Token(TokenType.Text, text.Substring(pos, tagStart - pos)));
			}

			tokens.Add(new Token(TokenType.Tag, m.Value));
			pos = tagStart + m.Length;
		}

		if (pos < text.Length)
		{
			tokens.Add(new Token(TokenType.Text, text.Substring(pos, text.Length - pos)));
		}

		return tokens;
	}

	/// <summary>
	/// swap back in all the special characters we've hidden
	/// </summary>
	private string Unescape(string s)
	{
		return _unescapes.Replace(s, UnescapeEvaluator);
	}

	private string UnescapeEvaluator(Match match)
	{
		return _invertedEscapeTable[match.Value];
	}

	#endregion

	#region Structures

	private struct Token
	{
		#region Fields

		public readonly TokenType Type;
		public readonly string Value;

		#endregion

		#region Constructors

		public Token(TokenType type, string value)
		{
			Type = type;
			Value = value;
		}

		#endregion
	}

	#endregion

	#region Enumerations

	private enum TokenType
	{
		Text,
		Tag
	}

	#endregion
}