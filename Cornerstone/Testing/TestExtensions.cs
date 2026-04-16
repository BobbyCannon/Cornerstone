#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Text.CodeGenerators;

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
	/// <param name="suffix"> An optional suffix to the dump. </param>
	public static T Dump<T>(this T value, string prefix = null, string suffix = null)
	{
		if (prefix != null)
		{
			Console.Write(prefix);
		}

		Console.Write(value);
		Console.WriteLine(suffix);
		return value;
	}

	/// <summary>
	/// Dump the value to the console.
	/// </summary>
	/// <param name="values"> The value to process. </param>
	/// <param name="prefix"> An optional prefix to the dump. </param>
	/// <param name="suffix"> An optional suffix to the dump. </param>
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	public static IEnumerable<T> DumpArray<T>(this IEnumerable<T> values, string prefix = null, string suffix = null)
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
	/// Dump the object as CSharp code to the console.
	/// </summary>
	/// <param name="value"> The value to process. </param>
	/// <param name="update"> Optional action to update settings. </param>
	/// <param name="suffix"> An optional suffix to the dump. </param>
	public static T DumpCSharp<T>(this T value, Action<CodeBuilderSettings> update = null, string suffix = null)
	{
		var writer = new CodeBuilder();
		update?.Invoke(writer.Settings);
		writer.WriteObject(value);
		writer.ToString().Dump(suffix: suffix);
		return value;
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