#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents an exception for the <see cref="DependencyProvider" /> when trying to create an object.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class DependencyInjectorConstructorException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public DependencyInjectorConstructorException()
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public DependencyInjectorConstructorException(string message, string typeName) : base(message)
	{
		TypeName = typeName;
	}

	#endregion

	#region Properties

	public string TypeName { get; }

	#endregion
}