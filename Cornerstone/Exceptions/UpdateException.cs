#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

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
	/// Initializes an instance of the update exception.
	/// </summary>
	public UpdateException()
	{
	}

	/// <summary>
	/// Initializes an instance of the update exception.
	/// </summary>
	public UpdateException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes an instance of the update exception.
	/// </summary>
	public UpdateException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}