#region References

using System;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

/// <inheritdoc />
public class SearchPatternException : Exception
{
	#region Constructors

	/// <inheritdoc />
	public SearchPatternException()
	{
	}

	/// <inheritdoc />
	public SearchPatternException(string message) : base(message)
	{
	}

	/// <inheritdoc />
	public SearchPatternException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}