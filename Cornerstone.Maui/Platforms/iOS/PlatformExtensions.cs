#region References

using System;
using Foundation;

#endregion

namespace Cornerstone.Maui.Platforms.iOS;

/// <summary>
/// iOS Platform extensions
/// </summary>
public static class PlatformExtensions
{
	#region Methods

	/// <summary>
	/// Converts NSDate to DateTime.
	/// </summary>
	public static DateTime ToDateTime(this NSDate date)
	{
		return (DateTime) date;
	}

	#endregion
}