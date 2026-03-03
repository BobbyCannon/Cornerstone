#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// Represents an exception when comparing objects.
/// </summary>
[ExcludeFromCodeCoverage]
public class CompareException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public CompareException() : this(string.Empty, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public CompareException(string message) : this(message, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public CompareException(string message, Exception inner) : base(message, inner!)
	{
	}

	#endregion
}