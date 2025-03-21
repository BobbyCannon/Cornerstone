namespace Cornerstone.Avalonia.TextEditor.Snippets;

/// <summary>
/// An element inside a snippet.
/// </summary>
public abstract class SnippetElement
{
	#region Methods

	/// <summary>
	/// Performs insertion of the snippet.
	/// </summary>
	public abstract void Insert(InsertionContext context);

	#endregion

	///// <summary>
	///// Converts the snippet to text, with replaceable fields in italic.
	///// </summary>
	//public virtual Inline ToTextRun()
	//{
	//	return null;
	//}
}