#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Runtime;

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
	public DependencyInjectorConstructorException() : this(string.Empty, string.Empty)
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