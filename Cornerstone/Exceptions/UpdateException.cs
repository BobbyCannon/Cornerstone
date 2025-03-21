#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents an update exception.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class UpdateException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public UpdateException() : this(string.Empty)
	{
	}

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public UpdateException(string message) : base(message)
	{
	}

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public UpdateException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}