#region References

using System;

#endregion

namespace Cornerstone.Protocols.Nmea.Exceptions;

/// <summary>
/// Represents a unknown error during parsing.
/// </summary>
public class NmeaParseUnknownException : Exception
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public NmeaParseUnknownException()
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public NmeaParseUnknownException(string message) : base(message)
	{
	}

	#endregion
}