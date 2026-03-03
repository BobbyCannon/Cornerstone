#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a Cornerstone database.
/// </summary>
[SourceReflection]
public class Database
	: IDatabase
{
	#region Methods

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
		}
	}

	#endregion
}

/// <summary>
/// The interfaces for a Cornerstone database.
/// </summary>
public interface IDatabase
	: IDisposable
{
}