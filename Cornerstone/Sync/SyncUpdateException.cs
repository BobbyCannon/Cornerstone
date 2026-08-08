#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents an update exception.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class SyncUpdateException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public SyncUpdateException() : this(string.Empty)
	{
	}

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public SyncUpdateException(string message) : base(message)
	{
	}

	/// <summary>
	/// Instantiates an instance of the update exception.
	/// </summary>
	public SyncUpdateException(string message, Exception inner) : base(message, inner)
	{
	}

	#endregion
}