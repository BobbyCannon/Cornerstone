#region References

using System;
using System.IO;
using System.Reflection;
using System.Text;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// A single known markdown document in a <see cref="DocumentationCatalog" />.
/// </summary>
public sealed class DocumentationDocument
{
	#region Fields

	private readonly Func<string> _readAllText;

	#endregion

	#region Constructors

	public DocumentationDocument(string id, string logicalPath, Func<string> readAllText, string displayTitle = null)
	{
		Id = NormalizeId(id);
		LogicalPath = NormalizeId(logicalPath);
		_readAllText = readAllText ?? throw new ArgumentNullException(nameof(readAllText));
		DisplayTitle = displayTitle;
	}

	#endregion

	#region Properties

	public string DisplayTitle { get; }

	/// <summary>
	/// Stable catalog key (normalized logical path).
	/// </summary>
	public string Id { get; }

	/// <summary>
	/// Path used when resolving relative links (forward slashes, no leading slash).
	/// </summary>
	public string LogicalPath { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Creates a document that reads from a file on disk.
	/// </summary>
	public static DocumentationDocument FromFile(string id, string logicalPath, string fullPath, string displayTitle = null)
	{
		return new DocumentationDocument(id, logicalPath, () => File.ReadAllText(fullPath), displayTitle);
	}

	/// <summary>
	/// Creates a document that reads a manifest embedded resource from an assembly.
	/// </summary>
	public static DocumentationDocument FromResource(string id, string logicalPath, Assembly assembly, string resourceName, string displayTitle = null)
	{
		if (assembly is null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		if (string.IsNullOrWhiteSpace(resourceName))
		{
			throw new ArgumentException("Resource name is required.", nameof(resourceName));
		}

		// Capture locals for the deferred reader (do not close over mutable parameters unexpectedly).
		var sourceAssembly = assembly;
		var name = resourceName;
		return new DocumentationDocument(id, logicalPath, () => ReadManifestResourceText(sourceAssembly, name), displayTitle);
	}

	public static string NormalizeId(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		var value = path.Replace('\\', '/').Trim();
		while (value.StartsWith("./", StringComparison.Ordinal))
		{
			value = value[2..];
		}

		return value.TrimStart('/');
	}

	public string ReadAllText()
	{
		return _readAllText() ?? string.Empty;
	}

	private static string ReadManifestResourceText(Assembly assembly, string resourceName)
	{
		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream is null)
		{
			return string.Empty;
		}

		using var reader = new StreamReader(stream, Encoding.UTF8, true);
		return reader.ReadToEnd();
	}

	#endregion
}