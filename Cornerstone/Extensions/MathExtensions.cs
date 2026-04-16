using System;

namespace Cornerstone.Extensions;

public static class MathExtensions
{
	#region Methods

	/// <summary>
	/// Clamp a value to its maximum.
	/// </summary>
	public static int Clamp(this int value, int max)
	{
		return value > max ? max : value;
	}

	#endregion
}