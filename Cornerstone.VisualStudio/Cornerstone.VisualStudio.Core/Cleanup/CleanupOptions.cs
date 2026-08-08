using System;
using System.Collections.Generic;
using System.Linq;

namespace Cornerstone.VisualStudio.Core.Cleanup;

/// <summary>
/// Options for <see cref="CleanupPipeline" />. Mirrors Tools → Options → Cornerstone settings.
/// </summary>
public sealed class CleanupOptions
{
	#region Constants

	public const string DefaultFileExtensions = ".axaml;.xaml";
	public const int DefaultIndentSize = 2;
	public const long DefaultMaxFileBytes = 5 * 1024 * 1024;

	#endregion

	#region Properties

	public bool EnsureFinalNewline { get; set; } = true;

	/// <summary>
	/// Semicolon- or comma-separated list of extensions (with or without leading dots).
	/// </summary>
	public string FileExtensions { get; set; } = DefaultFileExtensions;

	public bool FormatXml { get; set; } = true;

	public int IndentSize { get; set; } = DefaultIndentSize;

	public long MaxFileBytes { get; set; } = DefaultMaxFileBytes;

	public CleanupLineEndingMode NormalizeLineEndings { get; set; } = CleanupLineEndingMode.Keep;

	public bool PreferSelfClosing { get; set; } = true;

	public bool SortAttributes { get; set; } = true;

	public bool SortXmlns { get; set; } = true;

	public bool TrimTrailingWhitespace { get; set; } = true;

	#endregion

	#region Methods

	/// <summary>
	/// Returns normalized extensions including the leading dot, lower-invariant (e.g. ".axaml").
	/// </summary>
	public IReadOnlyList<string> GetNormalizedExtensions()
	{
		if (string.IsNullOrWhiteSpace(FileExtensions))
		{
			return ParseExtensions(DefaultFileExtensions);
		}

		return ParseExtensions(FileExtensions);
	}

	public bool MatchesExtension(string pathOrFileName)
	{
		if (string.IsNullOrWhiteSpace(pathOrFileName))
		{
			return false;
		}

		var ext = GetExtension(pathOrFileName);
		if (ext.Length == 0)
		{
			return false;
		}

		foreach (var allowed in GetNormalizedExtensions())
		{
			if (string.Equals(ext, allowed, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	public static IReadOnlyList<string> ParseExtensions(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<string>();
		}

		return value
			.Split([';', ',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
			.Select(NormalizeExtension)
			.Where(e => e.Length > 1)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static string GetExtension(string pathOrFileName)
	{
		var name = pathOrFileName;
		var slash = Math.Max(pathOrFileName.LastIndexOf('/'), pathOrFileName.LastIndexOf('\\'));
		if (slash >= 0 && slash + 1 < pathOrFileName.Length)
		{
			name = pathOrFileName.Substring(slash + 1);
		}

		var dot = name.LastIndexOf('.');
		if (dot < 0)
		{
			return string.Empty;
		}

		return NormalizeExtension(name.Substring(dot));
	}

	private static string NormalizeExtension(string extension)
	{
		if (string.IsNullOrWhiteSpace(extension))
		{
			return string.Empty;
		}

		var e = extension.Trim().ToLowerInvariant();
		if (!e.StartsWith(".", StringComparison.Ordinal))
		{
			e = "." + e;
		}

		return e;
	}

	#endregion
}
