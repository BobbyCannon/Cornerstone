#region References

using System;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Extensions for testing.
/// </summary>
public static class TestExtensions
{
	#region Methods

	/// <summary>
	/// Dump the value to the console.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <param name="prefix"> An optional prefix to the dump. </param>
	public static T Dump<T>(this T value, string prefix = null)
	{
		if (prefix != null)
		{
			Console.Write(prefix);
		}

		Console.WriteLine(value);
		return value;
	}

	/// <summary>
	/// Dump the value to the console.
	/// </summary>
	/// <param name="values"> The value to process. </param>
	/// <param name="prefix"> An optional prefix to the dump. </param>
	/// <param name="suffix"> An optional suffix to the dump. </param>
	public static T[] DumpArray<T>(this T[] values, string prefix = null, string suffix = null)
	{
		if (prefix != null)
		{
			Console.Write(prefix);
		}

		var first = true;
		foreach (var value in values)
		{
			if (!first)
			{
				Console.Write(", ");
			}

			Console.Write(value);
			first = false;
		}

		if (suffix != null)
		{
			Console.Write(suffix);
		}

		Console.WriteLine();
		return values;
	}

	/// <summary>
	/// Dump the value to the console.
	/// </summary>
	/// <param name="values"> The value to process. </param>
	/// <param name="prefix"> An optional prefix to the dump. </param>
	/// <param name="suffix"> An optional suffix to the dump. </param>
	public static byte[] DumpHexArray(this byte[] values, string prefix = null, string suffix = null)
	{
		if (prefix != null)
		{
			Console.Write(prefix);
		}

		var first = true;
		foreach (var value in values)
		{
			if (!first)
			{
				Console.Write(", ");
			}

			Console.Write($"0x{value:X2}");
			first = false;
		}

		if (suffix != null)
		{
			Console.Write(suffix);
		}

		Console.WriteLine();
		return values;
	}

	#endregion
}