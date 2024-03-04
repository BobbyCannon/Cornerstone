#region References

using System;

#endregion

namespace Cornerstone.Protocols.Nmea.Exceptions;

/// <summary>
/// Represents a checksum error during parsing.
/// </summary>
[Serializable]
public class NmeaParseChecksumException : Exception
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public NmeaParseChecksumException()
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public NmeaParseChecksumException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes an instance of the exception.
	/// </summary>
	public NmeaParseChecksumException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}