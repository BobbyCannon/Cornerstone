#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// Generator for creating code.
/// </summary>
public static class CodeGenerator
{
	#region Constructors

	/// <summary>
	/// Initialize the code generator
	/// </summary>
	static CodeGenerator()
	{
		DefaultWriterSettings = new CodeWriterSettings
		{
			IgnoreDefaultValues = true,
			IgnoreNullValues = true,
			IgnoreReadOnly = true,
			MaxDepth = int.MaxValue,
			NamingConvention = NamingConvention.PascalCase,
			OutputMode = CodeWriterMode.Instance,
			TextFormat = TextFormat.Indented
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// The default settings for writing code.
	/// </summary>
	public static CodeWriterSettings DefaultWriterSettings { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Get all combinations of an object.
	/// </summary>
	/// <typeparam name="T"> The type to generate property combinations for. </typeparam>
	/// <returns> The array of objects with unique state. </returns>
	public static IEnumerable<T> GetAllCodeCombinations<T>(Func<T, bool> where = null)
	{
		var type = typeof(T);
		var properties = type.GetCachedPropertyDictionary();
		var propertiesOptions = properties
			.Values
			.ToDictionary(x => x.Name, x => ValueGenerator.GetValueCombinations(x.PropertyType))
			.Where(x => x.Value.Count > 0)
			.Select(x => x.Value.Select(v => (x.Key, v)))
			.ToList();

		Func<IEnumerable<IEnumerable<(string, object)>>,
			IEnumerable<IList<(string, object)>>> getCombinations = null;
		getCombinations = xss =>
		{
			if (!xss.Any())
			{
				return [new List<(string, object)>()];
			}
			var query = xss.First()
				.SelectMany(x =>
						getCombinations(xss.Skip(1)),
					(x, y) => new[] { x }.Concat(y).ToList()
				);
			return query;
		};

		var combinations = getCombinations
			.Invoke(propertiesOptions)
			.SelectMany(x => x)
			.ToArray();

		var response = new List<T>();

		foreach (var chunk in combinations.EnumerateChunks(propertiesOptions.Count))
		{
			// Do not change the type declaration from object.
			// This is required for struct instances.
			object instance = Activator.CreateInstance<T>();

			foreach (var x in chunk)
			{
				if (properties.TryGetValue(x.Item1, out var info))
				{
					info.SetValue(instance, x.Item2, null);
				}
			}

			if ((where != null) && !where.Invoke((T) instance))
			{
				continue;
			}

			response.Add((T) instance);
		}

		return response.Distinct().ToList();
	}

	#endregion
}