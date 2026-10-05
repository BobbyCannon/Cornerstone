namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Article column width for <see cref="DocumentationReader" />.
/// </summary>
public enum DocumentationReadingWidth
{
	/// <summary>
	/// Centered column capped at <see cref="DocumentationReader.ReadingColumnMaxWidth" />.
	/// </summary>
	Column = 0,

	/// <summary>
	/// Stretch the article across the reader.
	/// </summary>
	Full = 1
}
