#region References

using System;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Indicates that the highlighting definition that was tried to load was invalid.
/// </summary>
public class HighlightingDefinitionInvalidException : Exception
{
	#region Constructors

	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException()
	{
	}

	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException(string message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new HighlightingDefinitionInvalidException instance.
	/// </summary>
	public HighlightingDefinitionInvalidException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}