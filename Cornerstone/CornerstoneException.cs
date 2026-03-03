#region References

using System;

#endregion

namespace Cornerstone;

/// <summary>
/// The base exception for the Cornerstone framework.
/// </summary>
public class CornerstoneException : Exception
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException()
	{
	}

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}