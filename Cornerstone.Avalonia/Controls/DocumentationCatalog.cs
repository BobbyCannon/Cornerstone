#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Set of known markdown documents the reader may navigate to.
/// Navigation is restricted to catalog entries only.
/// </summary>
public sealed class DocumentationCatalog
{
	#region Fields

	private readonly Dictionary<string, DocumentationDocument> _byId;

	#endregion

	#region Constructors

	public DocumentationCatalog(IEnumerable<DocumentationDocument> documents, string entryId = null)
	{
		_byId = new Dictionary<string, DocumentationDocument>(StringComparer.OrdinalIgnoreCase);
		foreach (var doc in documents ?? [])
		{
			if (doc is null || string.IsNullOrEmpty(doc.Id))
			{
				continue;
			}

			_byId[doc.Id] = doc;
		}

		if (!string.IsNullOrEmpty(entryId) && _byId.TryGetValue(DocumentationDocument.NormalizeId(entryId), out var entry))
		{
			Entry = entry;
		}
		else
		{
			Entry = _byId.Values.FirstOrDefault(d =>
					d.Id.EndsWith("Readme.md", StringComparison.OrdinalIgnoreCase)
					|| d.Id.EndsWith("README.md", StringComparison.OrdinalIgnoreCase))
				?? _byId.Values.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
		}
	}

	#endregion

	#region Properties

	public IReadOnlyCollection<DocumentationDocument> Documents => _byId.Values;

	public DocumentationDocument Entry { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Builds a catalog from manifest embedded resources whose names start with
	/// <paramref name="resourceNamePrefix" /> and end with <c>.md</c>.
	/// Logical document ids are the resource names with the prefix stripped
	/// (forward slashes preserved — set MSBuild <c>LogicalName</c> with <c>/</c> separators).
	/// </summary>
	/// <param name="assembly">Assembly that contains the embedded markdown.</param>
	/// <param name="resourceNamePrefix">
	/// Resource name prefix, e.g. <c>Documents/cornerstone/</c>.
	/// Matching uses ordinal ignore-case and accepts a missing trailing slash.
	/// </param>
	/// <param name="entryRelativePath">Entry document relative to the prefix (default <c>Readme.md</c>).</param>
	/// <param name="idPrefix">Optional catalog id prefix prepended to each logical path.</param>
	public static DocumentationCatalog FromAssemblyResources(
		Assembly assembly,
		string resourceNamePrefix,
		string entryRelativePath = "Readme.md",
		string idPrefix = null)
	{
		if (assembly is null)
		{
			return new DocumentationCatalog([]);
		}

		// NormalizeId keeps a trailing slash; strip then re-add so we never get "prefix//".
		var prefix = DocumentationDocument.NormalizeId(resourceNamePrefix ?? string.Empty).TrimEnd('/');
		if (prefix.Length > 0)
		{
			prefix += "/";
		}

		var catalogIdPrefix = string.IsNullOrEmpty(idPrefix)
			? string.Empty
			: DocumentationDocument.NormalizeId(idPrefix).TrimEnd('/') + "/";

		var docs = new List<DocumentationDocument>();
		foreach (var resourceName in assembly.GetManifestResourceNames())
		{
			if (string.IsNullOrEmpty(resourceName)
				|| !resourceName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var normalizedResource = DocumentationDocument.NormalizeId(resourceName);
			if ((prefix.Length > 0)
				&& !normalizedResource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var logical = prefix.Length == 0
				? normalizedResource
				: normalizedResource[prefix.Length..];
			logical = DocumentationDocument.NormalizeId(logical);
			if (string.IsNullOrEmpty(logical))
			{
				continue;
			}

			var id = catalogIdPrefix + logical;
			docs.Add(DocumentationDocument.FromResource(id, id, assembly, resourceName));
		}

		var entryId = catalogIdPrefix + DocumentationDocument.NormalizeId(entryRelativePath ?? "Readme.md");
		return new DocumentationCatalog(docs, entryId);
	}

	/// <summary>
	/// Builds a catalog by scanning a directory for <c> *.md </c> files.
	/// Logical paths are relative to <paramref name="contentRoot" />.
	/// </summary>
	public static DocumentationCatalog FromDirectory(string contentRoot, string entryRelativePath = "Readme.md", string idPrefix = null)
	{
		if (string.IsNullOrWhiteSpace(contentRoot) || !Directory.Exists(contentRoot))
		{
			return new DocumentationCatalog([]);
		}

		var root = Path.GetFullPath(contentRoot);
		var prefix = string.IsNullOrEmpty(idPrefix)
			? string.Empty
			: DocumentationDocument.NormalizeId(idPrefix).TrimEnd('/') + "/";

		var docs = new List<DocumentationDocument>();
		foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
		{
			var relative = Path.GetRelativePath(root, file);
			var logical = DocumentationDocument.NormalizeId(relative);
			var id = prefix + logical;
			docs.Add(DocumentationDocument.FromFile(id, id, file));
		}

		var entryId = prefix + DocumentationDocument.NormalizeId(entryRelativePath ?? "Readme.md");
		return new DocumentationCatalog(docs, entryId);
	}

	public bool TryGet(string id, out DocumentationDocument document)
	{
		return _byId.TryGetValue(DocumentationDocument.NormalizeId(id), out document);
	}

	/// <summary>
	/// Resolves a link href relative to the current document.
	/// Only succeeds when the target document is in the catalog.
	/// </summary>
	public bool TryResolve(string currentId, string href, out DocumentationDocument document, out string fragment)
	{
		document = null;
		fragment = null;

		if (string.IsNullOrWhiteSpace(href))
		{
			return false;
		}

		href = href.Trim();

		// External URLs are not catalog documents
		if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var hashIndex = href.IndexOf('#');
		var pathPart = hashIndex >= 0 ? href[..hashIndex] : href;
		fragment = hashIndex >= 0 ? href[(hashIndex + 1)..] : null;
		if (string.IsNullOrEmpty(fragment))
		{
			fragment = null;
		}

		pathPart = pathPart.Trim();
		if (pathPart.Length == 0)
		{
			// Fragment-only: same document
			return TryGet(currentId, out document);
		}

		// Absolute-in-catalog paths (leading /)
		if (pathPart.StartsWith('/'))
		{
			return TryGet(pathPart, out document);
		}

		var current = DocumentationDocument.NormalizeId(currentId);
		var directory = GetDirectory(current);
		var combined = string.IsNullOrEmpty(directory)
			? DocumentationDocument.NormalizeId(pathPart)
			: DocumentationDocument.NormalizeId(directory + "/" + pathPart);

		combined = NormalizePath(combined);
		return TryGet(combined, out document);
	}

	private static string GetDirectory(string logicalPath)
	{
		var normalized = DocumentationDocument.NormalizeId(logicalPath);
		var lastSlash = normalized.LastIndexOf('/');
		return lastSlash <= 0 ? string.Empty : normalized[..lastSlash];
	}

	private static string NormalizePath(string path)
	{
		var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var stack = new List<string>(parts.Length);
		foreach (var part in parts)
		{
			if (part == ".")
			{
				continue;
			}

			if (part == "..")
			{
				if (stack.Count > 0)
				{
					stack.RemoveAt(stack.Count - 1);
				}
				continue;
			}

			stack.Add(part);
		}

		return string.Join('/', stack);
	}

	#endregion
}