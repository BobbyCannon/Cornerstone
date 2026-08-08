namespace Cornerstone.VisualStudio.Core.Manipulation;

/// <summary>
/// Represents edit to be applied to text buffer
/// For simplicity’s sake two types of manipulations are offered only - Insertion and Deletion
/// </summary>
public record TextManipulation(int Start, int End, string? Text, ManipulationType Type)
{
	#region Methods

	public static TextManipulation Delete(int position, int length)
	{
		return new TextManipulation(position, position + length, null, ManipulationType.Delete);
	}

	public static TextManipulation Insert(int position, string text)
	{
		return new TextManipulation(position, position + text.Length, text, ManipulationType.Insert);
	}

	#endregion
}