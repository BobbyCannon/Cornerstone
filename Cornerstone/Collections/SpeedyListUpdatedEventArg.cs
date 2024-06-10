#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Collections;

/// <inheritdoc />
public class SpeedyListUpdatedEventArg<T> : EventArgs
{
	#region Constructors

	/// <summary>
	/// Represents changes to a SpeedyList.
	/// </summary>
	/// <param name="added"> The items added. </param>
	/// <param name="removed"> The items removed. </param>
	public SpeedyListUpdatedEventArg(IList<T> added, IList<T> removed)
	{
		Added = added;
		Removed = removed;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The items added.
	/// </summary>
	public IList<T> Added { get; }

	/// <summary>
	/// The items removed.
	/// </summary>
	public IList<T> Removed { get; }

	#endregion
}