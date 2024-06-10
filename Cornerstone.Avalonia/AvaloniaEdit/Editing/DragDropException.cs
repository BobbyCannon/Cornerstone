#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Wraps exceptions that occur during drag'n'drop.
/// Exceptions during drag'n'drop might
/// get swallowed, so AvaloniaEdit catches them and re-throws them later
/// wrapped in a DragDropException.
/// </summary>
public class DragDropException : Exception
{
	#region Constructors

	/// <summary>
	/// Creates a new DragDropException.
	/// </summary>
	public DragDropException()
	{
	}

	/// <summary>
	/// Creates a new DragDropException.
	/// </summary>
	public DragDropException(string message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new DragDropException.
	/// </summary>
	public DragDropException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}