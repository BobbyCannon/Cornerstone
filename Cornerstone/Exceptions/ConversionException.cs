#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// Represents an exception when converting an object.
/// <see cref="Converter" />
/// </summary>
[ExcludeFromCodeCoverage]
public class ConversionException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public ConversionException() : this(null, null, null, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
	public ConversionException(Type fromType, Type toType, string message) : this(fromType, toType, message, null)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public ConversionException(Type fromType, Type toType, string message, Exception inner) : base(message, inner)
	{
		FromType = fromType;
		ToType = toType;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The type we failed to convert from.
	/// </summary>
	public Type FromType { get; }

	/// <summary>
	/// The type we failed to convert to.
	/// </summary>
	public Type ToType { get; }

	#endregion
}