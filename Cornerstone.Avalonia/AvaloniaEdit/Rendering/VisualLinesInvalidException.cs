#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// A VisualLinesInvalidException indicates that you accessed the <see cref="TextView.VisualLines" /> property
/// of the <see cref="TextView" /> while the visual lines were invalid.
/// </summary>
public class VisualLinesInvalidException : Exception
{
	#region Constructors

	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException()
	{
	}

	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException(string message) : base(message)
	{
	}

	/// <summary>
	/// Creates a new VisualLinesInvalidException instance.
	/// </summary>
	public VisualLinesInvalidException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}