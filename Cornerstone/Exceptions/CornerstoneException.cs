#region References

using System;
using System.Runtime.Serialization;

#endregion

namespace Cornerstone.Exceptions;

/// <summary>
/// The base exception for the Cornerstone framework.
/// </summary>
public class CornerstoneException : Exception
{
	#region Constants

	/// <summary>
	/// Represents message for invalid sync clients.
	/// </summary>
	public const string ClientNotSupported = "This client is no longer supported. Please update to a supported version.";

	/// <summary>
	/// Represents message for key not found.
	/// </summary>
	public const string KeyNotFound = "Could not find the entry with the key.";

	/// <summary>
	/// Represents message for repository not found.
	/// </summary>
	public const string RepositoryNotFound = "The repository was not found.";

	/// <summary>
	/// Represents message for invalid sync entity type.
	/// </summary>
	public const string SyncEntityIncorrectType = "The sync entity is not the correct type.";

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException()
	{
	}

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes an instance of the cornerstone exception.
	/// </summary>
	public CornerstoneException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}