using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Cornerstone.VisualStudio.Core.Cleanup;

/// <summary>
/// Host-agnostic XAML/XML code cleanup. Safe to unit test without Visual Studio.
/// </summary>
public static class CleanupPipeline
{
	#region Methods

	/// <summary>
	/// Runs the cleanup pipeline on <paramref name="text" />.
	/// </summary>
	/// <param name="text">Document text.</param>
	/// <param name="options">Rule toggles and formatting options.</param>
	/// <param name="allowStructural">
	/// When false, only hygiene rules run (used for partial selection cleanup).
	/// </param>
	public static CleanupResult Clean(string text, CleanupOptions options, bool allowStructural = true)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		text ??= string.Empty;
		var original = text;

		try
		{
			if (!AnyRuleEnabled(options))
			{
				return CleanupResult.CreateSkipped(original, "No cleanup rules are enabled.");
			}

			var lineEnding = ResolveLineEnding(text, options.NormalizeLineEndings);
			var working = text;

			if (options.TrimTrailingWhitespace)
			{
				working = TrimTrailingWhitespace(working);
			}

			var structuralApplied = false;
			string structuralMessage = null;

			var wantStructural = allowStructural &&
				(options.FormatXml || options.SortXmlns || options.SortAttributes || options.PreferSelfClosing);

			if (wantStructural)
			{
				if (TryStructuralCleanup(working, options, lineEnding, out var structured, out var error))
				{
					working = structured;
					structuralApplied = true;
				}
				else
				{
					structuralMessage = error ?? "Document is not well-formed XML; structural cleanup skipped.";
				}
			}

			// Re-apply hygiene after structural rewrite (formatter may reintroduce trailing spaces on blank lines).
			if (options.TrimTrailingWhitespace)
			{
				working = TrimTrailingWhitespace(working);
			}

			working = NormalizeLineEndings(working, lineEnding);

			if (options.EnsureFinalNewline)
			{
				working = EnsureFinalNewline(working, lineEnding);
			}
			else
			{
				// Still normalize internal endings when EnsureFinalNewline is off.
				working = NormalizeLineEndings(working, lineEnding);
			}

			if (string.Equals(working, original, StringComparison.Ordinal))
			{
				if (structuralMessage != null)
				{
					return CleanupResult.CreateStructuralSkipped(working, original, structuralMessage);
				}

				return CleanupResult.CreateUnchanged(working, structuralApplied);
			}

			if (structuralMessage != null)
			{
				return CleanupResult.CreateStructuralSkipped(working, original, structuralMessage);
			}

			return CleanupResult.CreateChanged(working, structuralApplied);
		}
		catch (Exception ex)
		{
			return CleanupResult.CreateError(original, ex.Message);
		}
	}

	/// <summary>
	/// Hygiene-only cleanup for a selected span (no full-document structural rewrite).
	/// </summary>
	public static CleanupResult CleanSelection(string selectedText, CleanupOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		// Structural rules require a full well-formed document.
		var selectionOptions = CloneForSelection(options);
		return Clean(selectedText ?? string.Empty, selectionOptions, allowStructural: false);
	}

	private static bool AnyRuleEnabled(CleanupOptions options)
	{
		return options.TrimTrailingWhitespace ||
			options.EnsureFinalNewline ||
			options.NormalizeLineEndings != CleanupLineEndingMode.Keep ||
			options.FormatXml ||
			options.SortXmlns ||
			options.SortAttributes ||
			options.PreferSelfClosing;
	}

	private static CleanupOptions CloneForSelection(CleanupOptions options)
	{
		return new CleanupOptions
		{
			FileExtensions = options.FileExtensions,
			TrimTrailingWhitespace = options.TrimTrailingWhitespace,
			EnsureFinalNewline = false, // do not force EOF newline inside a mid-document selection
			NormalizeLineEndings = options.NormalizeLineEndings,
			FormatXml = false,
			SortXmlns = false,
			SortAttributes = false,
			PreferSelfClosing = false,
			IndentSize = options.IndentSize,
			MaxFileBytes = options.MaxFileBytes
		};
	}

	private static string EnsureFinalNewline(string text, string lineEnding)
	{
		if (text.Length == 0)
		{
			return text;
		}

		if (text.EndsWith("\r\n", StringComparison.Ordinal))
		{
			return lineEnding == "\r\n" ? text : text.Substring(0, text.Length - 2) + lineEnding;
		}

		if (text.EndsWith("\n", StringComparison.Ordinal))
		{
			if (lineEnding == "\n")
			{
				return text;
			}

			// ends with LF only; replace trailing LF with desired ending
			return text.Substring(0, text.Length - 1) + lineEnding;
		}

		if (text.EndsWith("\r", StringComparison.Ordinal))
		{
			return text.Substring(0, text.Length - 1) + lineEnding;
		}

		return text + lineEnding;
	}

	private static string NormalizeLineEndings(string text, string lineEnding)
	{
		// Normalize to LF first, then to target.
		var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
		if (lineEnding == "\n")
		{
			return normalized;
		}

		return normalized.Replace("\n", lineEnding);
	}

	private static string ResolveLineEnding(string text, CleanupLineEndingMode mode)
	{
		switch (mode)
		{
			case CleanupLineEndingMode.Crlf:
				return "\r\n";
			case CleanupLineEndingMode.Lf:
				return "\n";
			default:
				return DetectDominantLineEnding(text);
		}
	}

	private static string DetectDominantLineEnding(string text)
	{
		var crlf = 0;
		var lf = 0;
		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				if (i > 0 && text[i - 1] == '\r')
				{
					crlf++;
				}
				else
				{
					lf++;
				}
			}
		}

		if (crlf == 0 && lf == 0)
		{
			return Environment.NewLine == "\n" ? "\n" : "\r\n";
		}

		return crlf >= lf ? "\r\n" : "\n";
	}

	private static string TrimTrailingWhitespace(string text)
	{
		if (text.Length == 0)
		{
			return text;
		}

		var sb = new StringBuilder(text.Length);
		var lineStart = 0;
		for (var i = 0; i < text.Length; i++)
		{
			var c = text[i];
			if (c == '\n')
			{
				AppendTrimmedLine(sb, text, lineStart, i);
				sb.Append('\n');
				lineStart = i + 1;
			}
			else if (c == '\r')
			{
				AppendTrimmedLine(sb, text, lineStart, i);
				sb.Append('\r');
				if (i + 1 < text.Length && text[i + 1] == '\n')
				{
					sb.Append('\n');
					i++;
				}

				lineStart = i + 1;
			}
		}

		if (lineStart <= text.Length)
		{
			AppendTrimmedLine(sb, text, lineStart, text.Length);
		}

		return sb.ToString();
	}

	private static void AppendTrimmedLine(StringBuilder sb, string text, int start, int endExclusive)
	{
		var end = endExclusive;
		while (end > start)
		{
			var c = text[end - 1];
			if (c == ' ' || c == '\t')
			{
				end--;
				continue;
			}

			break;
		}

		if (end > start)
		{
			sb.Append(text, start, end - start);
		}
	}

	private static bool TryStructuralCleanup(
		string text,
		CleanupOptions options,
		string lineEnding,
		out string result,
		out string error)
	{
		result = text;
		error = null;

		if (string.IsNullOrWhiteSpace(text))
		{
			error = "Document is empty.";
			return false;
		}

		XDocument document;
		try
		{
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				IgnoreWhitespace = false
			};

			using (var stringReader = new StringReader(text))
			using (var reader = XmlReader.Create(stringReader, settings))
			{
				document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			}
		}
		catch (XmlException ex)
		{
			error = "Document is not well-formed XML; structural cleanup skipped. " + ex.Message;
			return false;
		}

		if (document.Root == null)
		{
			error = "Document has no root element.";
			return false;
		}

		if (options.SortXmlns || options.SortAttributes)
		{
			SortAttributesRecursive(document.Root, options.SortXmlns, options.SortAttributes);
		}

		if (options.PreferSelfClosing)
		{
			CollapseEmptyElements(document.Root);
		}

		// Always rewrite through XmlWriter when any structural rule ran so formatting is consistent.
		// FormatXml controls indent; without it we still emit a compact but attribute-ordered tree.
		result = WriteDocument(document, options, lineEnding);
		return true;
	}

	private static void SortAttributesRecursive(XElement element, bool sortXmlns, bool sortAttributes)
	{
		var attrs = element.Attributes().ToList();
		if (attrs.Count > 1 && (sortXmlns || sortAttributes))
		{
			var ordered = attrs
				.OrderBy(a => AttributeSortKey(a, sortXmlns, sortAttributes), StringComparer.Ordinal)
				.ToList();

			foreach (var attr in attrs)
			{
				attr.Remove();
			}

			foreach (var attr in ordered)
			{
				element.Add(attr);
			}
		}

		foreach (var child in element.Elements())
		{
			SortAttributesRecursive(child, sortXmlns, sortAttributes);
		}
	}

	/// <summary>
	/// Sort key: xmlns first (default then prefixes), then Name/x:Name, then remaining by name.
	/// </summary>
	private static string AttributeSortKey(XAttribute attribute, bool sortXmlns, bool sortAttributes)
	{
		var name = attribute.Name;
		var isXmlns = name.Namespace == XNamespace.Xmlns ||
			(name.LocalName == "xmlns" && name.Namespace == XNamespace.None);

		if (isXmlns)
		{
			if (!sortXmlns)
			{
				// Keep relative xmlns order when only non-xmlns sorting is on: park xmlns first still for stability.
				return "0|" + attribute.ToString();
			}

			// default xmlns, then xmlns:prefix alpha
			if (name.LocalName == "xmlns" || name.Namespace == XNamespace.None)
			{
				return "0|xmlns";
			}

			return "0|xmlns:" + name.LocalName;
		}

		if (!sortAttributes)
		{
			// Keep non-xmlns relative order after xmlns block.
			return "1|" + attribute.ToString();
		}

		// x:Name / Name near front
		if (name.LocalName == "Name")
		{
			return "1|Name|" + (name.NamespaceName ?? string.Empty);
		}

		// Attached properties (contain '.') after simple names, still alpha within group
		var local = name.LocalName;
		var attached = local.IndexOf('.') >= 0 ? "3|" : "2|";
		var prefix = name.NamespaceName ?? string.Empty;
		return attached + local + "|" + prefix;
	}

	private static void CollapseEmptyElements(XElement element)
	{
		foreach (var child in element.Elements().ToList())
		{
			CollapseEmptyElements(child);
		}

		if (!element.HasElements && !element.Nodes().OfType<XComment>().Any() &&
			!element.Nodes().OfType<XCData>().Any())
		{
			// Only whitespace/text that is empty or whitespace → treat as empty for self-closing.
			var text = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value));
			if (string.IsNullOrWhiteSpace(text))
			{
				element.RemoveNodes();
			}
		}
	}

	private static string WriteDocument(XDocument document, CleanupOptions options, string lineEnding)
	{
		var indent = Math.Max(0, options.IndentSize);
		var format = options.FormatXml;

		var settings = new XmlWriterSettings
		{
			Indent = format,
			IndentChars = format ? new string(' ', indent) : string.Empty,
			NewLineChars = lineEnding,
			NewLineHandling = NewLineHandling.Replace,
			OmitXmlDeclaration = document.Declaration == null,
			Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
			// Empty elements write as <Foo /> when they have no children.
			ConformanceLevel = ConformanceLevel.Document
		};

		var sb = new StringBuilder();
		using (var stringWriter = new StringWriter(sb))
		using (var writer = XmlWriter.Create(stringWriter, settings))
		{
			document.Save(writer);
		}

		var result = sb.ToString();

		// XmlWriter may emit XML declaration based on settings; strip if original had none and we omitted it.
		// Also normalize accidental UTF-16 BOM-less string.

		// XmlWriter with OmitXmlDeclaration still might add newline after root differently.
		// Convert to desired line endings already set via NewLineChars.

		// Prefer " />" spacing consistency: XmlWriter typically writes " />"
		return result;
	}

	#endregion
}
