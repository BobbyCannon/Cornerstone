#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents an NotFound exception.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class NotFoundException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the NotFound exception.
	/// </summary>
	public NotFoundException()
	{
	}

	/// <summary>
	/// Initializes an instance of the NotFound exception.
	/// </summary>
	public NotFoundException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes an instance of the NotFound exception.
	/// </summary>
	public NotFoundException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}