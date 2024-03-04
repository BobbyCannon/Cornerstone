#region References

using System;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents an invalid .NET solution exception.
/// </summary>
[Serializable]
public class InvalidDotNetSolutionException : Exception
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.InvalidDotNetSolutionException" /> class.
	/// </summary>
	public InvalidDotNetSolutionException() : this("The .NET solution was invalid.")
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.InvalidDotNetSolutionException" /> class.
	/// </summary>
	/// <param name="message"> The message that describes the error. </param>
	public InvalidDotNetSolutionException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.InvalidDotNetSolutionException" /> class.
	/// </summary>
	/// <param name="message"> The message that describes the error. </param>
	/// <param name="innerException"> The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified. </param>
	public InvalidDotNetSolutionException(string message, Exception innerException) : base(message, innerException)
	{
	}

	#endregion
}