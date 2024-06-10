namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// Interface for resolvers that can solve cross-definition references.
/// </summary>
public interface IHighlightingDefinitionReferenceResolver
{
	#region Methods

	/// <summary>
	/// Gets the highlighting definition by name, or null if it is not found.
	/// </summary>
	IHighlightingDefinition GetDefinition(string name);

	#endregion
}