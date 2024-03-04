#region References

using System;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents a sync issue exception.
/// </summary>
[Serializable]
public class SyncException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncException()
		: this(string.Empty)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncException(string message)
		: this(message, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public SyncException(string message, Exception inner)
		: base(message, inner)
	{
	}

	#endregion
}