#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for sets
/// </summary>
public static class SetExtensions
{
	#region Methods

	/// <summary>
	/// Add or update a dictionary entry.
	/// </summary>
	/// <typeparam name="T"> The type of the set. </typeparam>
	/// <typeparam name="T2"> The type of the values in the set. </typeparam>
	/// <param name="set"> The set to update. </param>
	/// <param name="values"> The values to be added or updated. </param>
	public static T AddRange<T, T2>(this T set, params T2[] values) where T : ISet<T2>
	{
		foreach (var value in values)
		{
			set.Add(value);
		}

		return set;
	}

	#endregion
}