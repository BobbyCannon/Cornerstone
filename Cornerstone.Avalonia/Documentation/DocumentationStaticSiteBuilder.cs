#region References

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Parsers.Markdown;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Writes a catalog of markdown documents as static HTML plus generated theme CSS.
/// </summary>
public static class DocumentationStaticSiteBuilder
{
	#region Fields

	private static readonly (string AvaloniaPath, string FileName)[] FontAssets =
	[
		("/Assets/Fonts/OpenSans/OpenSans-Regular.ttf", "OpenSans-Regular.ttf"),
		("/Assets/Fonts/OpenSans/OpenSans-Italic.ttf", "OpenSans-Italic.ttf"),
		("/Assets/Fonts/OpenSans/OpenSans-Bold.ttf", "OpenSans-Bold.ttf"),
		("/Assets/Fonts/OpenSans/OpenSans-BoldItalic.ttf", "OpenSans-BoldItalic.ttf"),
		("/Assets/Fonts/OpenSans/OpenSans-Light.ttf", "OpenSans-Light.ttf"),
		("/Assets/Fonts/DejaVuSansMono/DejaVuSansMono.ttf", "DejaVuSansMono.ttf"),
		("/Assets/Fonts/DejaVuSansMono/DejaVuSansMono-Bold.ttf", "DejaVuSansMono-Bold.ttf"),
		("/Assets/Fonts/DejaVuSansMono/DejaVuSansMono-Oblique.ttf", "DejaVuSansMono-Oblique.ttf"),
		("/Assets/Fonts/DejaVuSansMono/DejaVuSansMono-BoldOblique.ttf", "DejaVuSansMono-BoldOblique.ttf")
	];

	private static readonly Regex HrefPattern = new("href=\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	#endregion

	#region Methods

	public static void Export(DocumentationCatalog catalog, string outputDirectory)
	{
		if (catalog is null)
		{
			throw new ArgumentNullException(nameof(catalog));
		}

		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
		}

		var root = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(root);
		File.WriteAllText(Path.Combine(root, "theme.css"), ThemeCssWriter.Write(), Encoding.UTF8);
		File.WriteAllText(Path.Combine(root, "site.css"), DocumentationSiteStyles.Css, Encoding.UTF8);
		File.WriteAllText(Path.Combine(root, "site.js"), DocumentationSiteScripts.JavaScript, Encoding.UTF8);
		ExportFonts(root);

		foreach (var document in catalog.Documents.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
		{
			var relativeHtml = ToHtmlRelativePath(document.LogicalPath);
			var fullPath = Path.Combine(root, relativeHtml.Replace('/', Path.DirectorySeparatorChar));
			var directory = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var markdown = document.ReadAllText();
			var body = new MarkdownRendererForHtml().ToHtml(markdown);
			body = RewriteHrefs(catalog, document.Id, relativeHtml, body);
			var title = ResolveTitle(document, markdown);
			var cssPrefix = CssPrefix(relativeHtml);
			var breadcrumb = BuildBreadcrumb(catalog, document, relativeHtml, title);
			var html = WrapPage(title, cssPrefix, breadcrumb, body);
			File.WriteAllText(fullPath, html, Encoding.UTF8);
		}
	}

	public static string ToHtmlRelativePath(string logicalPath)
	{
		var normalized = DocumentationDocument.NormalizeId(logicalPath);
		if (string.IsNullOrEmpty(normalized))
		{
			return "index.html";
		}

		var fileName = Path.GetFileName(normalized);
		var directory = GetDirectory(normalized);
		if (fileName.Equals("Readme.md", StringComparison.OrdinalIgnoreCase)
			|| fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)
			|| fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase))
		{
			return string.IsNullOrEmpty(directory) ? "index.html" : directory + "/index.html";
		}

		var withoutExtension = normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? normalized[..^3]
			: normalized;
		return withoutExtension + ".html";
	}

	private static void AppendCrumb(StringBuilder builder, string label, string href, bool current)
	{
		builder.Append("\t\t\t<li");
		if (current)
		{
			builder.Append(" aria-current=\"page\"");
		}

		builder.Append('>');
		if (!current && !string.IsNullOrEmpty(href))
		{
			builder.Append("<a href=\"");
			builder.Append(href);
			builder.Append("\">");
			builder.Append(WebUtility.HtmlEncode(label));
			builder.Append("</a>");
		}
		else
		{
			builder.Append(WebUtility.HtmlEncode(label));
		}

		builder.AppendLine("</li>");
	}

	private static string BuildBreadcrumb(
		DocumentationCatalog catalog,
		DocumentationDocument document,
		string currentHtml,
		string currentTitle)
	{
		var builder = new StringBuilder();
		builder.AppendLine("\t<nav class=\"breadcrumbs\" aria-label=\"Breadcrumb\">");
		builder.AppendLine("\t\t<ol>");

		var isRootIndex = string.Equals(currentHtml, "index.html", StringComparison.OrdinalIgnoreCase);
		AppendCrumb(builder, "Documentation", isRootIndex ? string.Empty : Relativize(currentHtml, "index.html"), isRootIndex);

		var directory = GetDirectory(document.LogicalPath);
		if (!string.IsNullOrEmpty(directory))
		{
			var parts = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
			var accumulated = string.Empty;
			for (var i = 0; i < parts.Length; i++)
			{
				accumulated = accumulated.Length == 0 ? parts[i] : accumulated + "/" + parts[i];
				var folderIsCurrent = IsFolderIndex(document.LogicalPath) && (i == (parts.Length - 1));
				var href = string.Empty;
				if (!folderIsCurrent)
				{
					var indexLogical = accumulated + "/Readme.md";
					if (catalog.TryGet(indexLogical, out _))
					{
						href = Relativize(currentHtml, ToHtmlRelativePath(indexLogical));
					}
				}

				var label = folderIsCurrent ? currentTitle : parts[i];
				AppendCrumb(builder, label, href, folderIsCurrent);
			}
		}

		if (!isRootIndex && !IsFolderIndex(document.LogicalPath))
		{
			AppendCrumb(builder, currentTitle, string.Empty, true);
		}

		builder.AppendLine("\t\t</ol>");
		builder.AppendLine("\t</nav>");
		return builder.ToString();
	}

	private static void CopyExactly(Stream source, Stream destination, int byteCount)
	{
		var remaining = byteCount;
		var buffer = new byte[Math.Min(8192, Math.Max(remaining, 1))];
		while (remaining > 0)
		{
			var read = source.Read(buffer, 0, Math.Min(buffer.Length, remaining));
			if (read <= 0)
			{
				throw new EndOfStreamException("Font asset was shorter than the Avalonia resource index.");
			}

			destination.Write(buffer, 0, read);
			remaining -= read;
		}
	}

	private static string CssPrefix(string htmlRelativePath)
	{
		var slashCount = htmlRelativePath.Count(c => c == '/');
		if (slashCount == 0)
		{
			return string.Empty;
		}

		var builder = new StringBuilder();
		for (var i = 0; i < slashCount; i++)
		{
			builder.Append("../");
		}

		return builder.ToString();
	}

	private static void ExportFonts(string root)
	{
		var fontsDirectory = Path.Combine(root, "fonts");
		Directory.CreateDirectory(fontsDirectory);
		var assembly = typeof(DocumentationStaticSiteBuilder).Assembly;
		foreach (var asset in FontAssets)
		{
			var destination = Path.Combine(fontsDirectory, asset.FileName);
			if (!TryWriteAvaloniaResource(assembly, asset.AvaloniaPath, destination))
			{
				throw new InvalidOperationException("Missing font asset " + asset.AvaloniaPath + ".");
			}
		}
	}

	private static string GetDirectory(string logicalPath)
	{
		var lastSlash = logicalPath.LastIndexOf('/');
		return lastSlash <= 0 ? string.Empty : logicalPath[..lastSlash];
	}

	private static bool IsFolderIndex(string logicalPath)
	{
		var fileName = Path.GetFileName(DocumentationDocument.NormalizeId(logicalPath));
		return fileName.Equals("Readme.md", StringComparison.OrdinalIgnoreCase)
			|| fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)
			|| fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase);
	}

	private static string Relativize(string fromHtml, string toHtml)
	{
		var fromParts = GetDirectory(fromHtml).Split('/', StringSplitOptions.RemoveEmptyEntries);
		var toParts = toHtml.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var shared = 0;
		while ((shared < fromParts.Length)
				&& (shared < (toParts.Length - 1))
				&& fromParts[shared].Equals(toParts[shared], StringComparison.OrdinalIgnoreCase))
		{
			shared++;
		}

		var builder = new StringBuilder();
		for (var i = shared; i < fromParts.Length; i++)
		{
			builder.Append("../");
		}

		for (var i = shared; i < toParts.Length; i++)
		{
			if (i > shared)
			{
				builder.Append('/');
			}

			builder.Append(toParts[i]);
		}

		return builder.Length == 0 ? toHtml : builder.ToString();
	}

	private static string ResolveTitle(DocumentationDocument document, string markdown)
	{
		if (!string.IsNullOrEmpty(document.DisplayTitle))
		{
			return document.DisplayTitle;
		}

		using var reader = new StringReader(markdown ?? string.Empty);
		string line;
		while ((line = reader.ReadLine()) != null)
		{
			var trimmed = line.Trim();
			if (trimmed.StartsWith("# ", StringComparison.Ordinal))
			{
				return trimmed[2..].Trim();
			}
		}

		return Path.GetFileNameWithoutExtension(document.LogicalPath);
	}

	private static string RewriteHrefs(DocumentationCatalog catalog, string currentId, string currentHtml, string html)
	{
		return HrefPattern.Replace(html, match =>
		{
			var href = match.Groups[1].Value;
			if (!catalog.TryResolve(currentId, href, out var target, out var fragment))
			{
				return match.Value;
			}

			var targetHtml = ToHtmlRelativePath(target.LogicalPath);
			var relative = Relativize(currentHtml, targetHtml);
			if (!string.IsNullOrEmpty(fragment))
			{
				relative += "#" + fragment;
			}

			return "href=\"" + relative + "\"";
		});
	}

	private static bool TryWriteAvaloniaResource(Assembly assembly, string avaloniaPath, string destination)
	{
		using var stream = assembly.GetManifestResourceStream("!AvaloniaResources");
		if (stream is null)
		{
			return false;
		}

		using var reader = new BinaryReader(stream, Encoding.UTF8, true);
		var indexLength = reader.ReadInt32();
		var indexBytes = reader.ReadBytes(indexLength);
		var dataStart = stream.Position;
		using var indexStream = new MemoryStream(indexBytes);
		using var indexReader = new BinaryReader(indexStream, Encoding.UTF8, false);
		var version = indexReader.ReadInt32();
		if (version != 2)
		{
			return false;
		}

		var entryCount = indexReader.ReadInt32();
		for (var i = 0; i < entryCount; i++)
		{
			var path = indexReader.ReadString();
			var offset = indexReader.ReadInt32();
			var size = indexReader.ReadInt32();
			if (!path.Equals(avaloniaPath, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			stream.Position = dataStart + offset;
			using var output = File.Create(destination);
			CopyExactly(stream, output, size);
			return true;
		}

		return false;
	}

	private static string WrapPage(string title, string cssPrefix, string breadcrumb, string body)
	{
		var builder = new StringBuilder();
		builder.AppendLine("<!DOCTYPE html>");
		builder.AppendLine("<html lang=\"en\" data-theme-color=\"Blue\" data-density=\"normal\">");
		builder.AppendLine("<head>");
		builder.AppendLine("\t<meta charset=\"utf-8\" />");
		builder.AppendLine("\t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
		builder.Append("\t<title>");
		builder.Append(WebUtility.HtmlEncode(title));
		builder.AppendLine("</title>");
		builder.Append("\t<link rel=\"stylesheet\" href=\"");
		builder.Append(cssPrefix);
		builder.AppendLine("theme.css\" />");
		builder.Append("\t<link rel=\"stylesheet\" href=\"");
		builder.Append(cssPrefix);
		builder.AppendLine("site.css\" />");
		builder.AppendLine("</head>");
		builder.AppendLine("<body>");
		builder.AppendLine("\t<header class=\"site-header\">");
		builder.Append("\t\t<a class=\"site-home\" href=\"");
		builder.Append(cssPrefix);
		builder.AppendLine("index.html\">Documentation</a>");
		builder.AppendLine("\t\t<div class=\"site-toolbar\">");
		builder.AppendLine("\t\t\t<label>Color");
		builder.AppendLine("\t\t\t\t<select id=\"theme-color\">");
		foreach (var color in Theme.Colors)
		{
			builder.Append("\t\t\t\t\t<option value=\"");
			builder.Append(color);
			builder.Append('"');
			if (color == ThemeColor.Blue)
			{
				builder.Append(" selected=\"selected\"");
			}

			builder.Append('>');
			builder.Append(color);
			builder.AppendLine("</option>");
		}

		builder.AppendLine("\t\t\t\t</select>");
		builder.AppendLine("\t\t\t</label>");
		builder.AppendLine("\t\t\t<label>Density");
		builder.AppendLine("\t\t\t\t<select id=\"density\">");
		builder.AppendLine("\t\t\t\t\t<option value=\"compact\">Compact</option>");
		builder.AppendLine("\t\t\t\t\t<option value=\"normal\" selected=\"selected\">Normal</option>");
		builder.AppendLine("\t\t\t\t\t<option value=\"large\">Large</option>");
		builder.AppendLine("\t\t\t\t</select>");
		builder.AppendLine("\t\t\t</label>");
		builder.AppendLine("\t\t\t<button type=\"button\" id=\"theme-toggle\">Dark</button>");
		builder.AppendLine("\t\t</div>");
		builder.AppendLine("\t</header>");
		builder.Append(breadcrumb);
		builder.AppendLine("\t<main class=\"markdown-body\">");
		builder.AppendLine(body);
		builder.AppendLine("\t</main>");
		builder.Append("\t<script src=\"");
		builder.Append(cssPrefix);
		builder.AppendLine("site.js\"></script>");
		builder.AppendLine("</body>");
		builder.AppendLine("</html>");
		return builder.ToString();
	}

	#endregion
}