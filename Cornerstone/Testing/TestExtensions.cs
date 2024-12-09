#region References

using System;
using System.Collections.Generic;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Serialization;
using Cornerstone.Text;

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
	/// Dump each item to the Console.WriteLine().
	/// </summary>
	/// <typeparam name="T"> The type of the item in the list. </typeparam>
	/// <param name="items"> The items to dump. </param>
	/// <returns> The items that was dumped. </returns>
	public static IEnumerable<T> Dump<T>(this IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			item.Dump();
		}

		return items;
	}

	/// <summary>
	/// Dump the item as CSharp code to the Console.WriteLine().
	/// </summary>
	/// <param name="item"> The item to dump. </param>
	/// <param name="options"> The code writer options. </param>
	/// <param name="prefix"> An optional prefix. </param>
	public static string DumpCSharp(this object item, CodeWriterSettings? options = null, string prefix = null)
	{
		if (!string.IsNullOrEmpty(prefix))
		{
			Console.Write(prefix);
		}

		if (item == null)
		{
			Console.WriteLine("null");
			return "null";
		}

		var code = item.ToCSharp(options ?? new CodeWriterSettings
		{
			IgnoreDefaultValues = true,
			IgnoreReadOnly = true,
			IgnoreNullValues = true,
			OutputMode = CodeWriterMode.Instance,
			TextFormat = TextFormat.Spaced
		});

		Console.WriteLine(code);

		return code;
	}

	/// <summary>
	/// Dump the items as CSharp array code to the Console.WriteLine().
	/// </summary>
	/// <param name="items"> The items to dump. </param>
	/// <param name="builder"> The text builder to use. </param>
	/// <param name="options"> The code writer options. </param>
	/// <param name="prefix"> An optional prefix. </param>
	public static ITextBuilder DumpCSharpArray<T>(this T[] items, ITextBuilder builder = null, CodeWriterSettings? options = null, string prefix = null)
	{
		if (!string.IsNullOrEmpty(prefix))
		{
			Console.Write(prefix);
		}

		if (items == null)
		{
			Console.WriteLine("null");
			return new TextBuilder("null");
		}

		options ??= new CodeWriterSettings
		{
			IgnoreDefaultValues = true,
			IgnoreReadOnly = true,
			IgnoreNullValues = true,
			OutputMode = CodeWriterMode.Instance,
			TextFormat = TextFormat.Spaced
		};
		builder ??= new TextBuilder(options);

		for (var index = 0; index < items.Length; index++)
		{
			var item = items[index];
			var itemCode = item.ToCSharp(options);
			
			if (index > 0)
			{
				builder.Append(",");
			}

			builder.AppendLine();
			if (!string.IsNullOrWhiteSpace(prefix))
			{
				builder.Append(prefix);
			}
			builder.Append(itemCode);
		}

		builder.AppendLine("");
		var code = builder.ToString();

		Console.WriteLine(code);

		return builder;
	}

	/// <summary>
	/// Dump the item to the Console.WriteLine().
	/// </summary>
	/// <param name="item"> The item to dump. </param>
	/// <param name="prefix"> An optional prefix. </param>
	public static string DumpJson(this object item, string prefix = null)
	{
		if (!string.IsNullOrEmpty(prefix))
		{
			Console.Write(prefix);
		}

		if (item == null)
		{
			Console.WriteLine("null");
			return "null";
		}

		var isArray = item.GetType().IsArray;
		var json = item.ToJson(new SerializationSettings
		{
			TextFormat = isArray ? TextFormat.None : TextFormat.Indented,
			EnumFormat = EnumFormat.Name
		});

		Console.WriteLine(json);
		return json;
	}

	/// <summary>
	/// Dump the item to the Console.WriteLine().
	/// </summary>
	/// <param name="item"> The item to dump. </param>
	/// <param name="prefix"> An optional prefix. </param>
	public static string DumpPrettyJson(this object item, string prefix = null)
	{
		if (!string.IsNullOrEmpty(prefix))
		{
			Console.Write(prefix);
		}

		if (item == null)
		{
			Console.WriteLine("null");
			return "null";
		}

		var json = item.ToJson(new SerializationSettings
		{
			TextFormat = TextFormat.Indented,
			EnumFormat = EnumFormat.Name
		});

		Console.WriteLine(json);
		return json;
	}

	#endregion
}