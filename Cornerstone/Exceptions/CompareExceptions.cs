#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Compare;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents an exception when comparing objects.
/// <see cref="CompareSession{T,T2}" />
/// </summary>
[ExcludeFromCodeCoverage]
public class CompareException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public CompareException() : this(null, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
	public CompareException(string message) : this(message, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public CompareException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}