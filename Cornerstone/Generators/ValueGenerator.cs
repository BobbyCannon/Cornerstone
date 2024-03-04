#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Generators.ValueGenerators;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// Generate test values for types.
/// </summary>
public static class ValueGenerator
{
	#region Fields

	private static readonly List<IValueGenerator> _builtInGenerators;

	#endregion

	#region Constructors

	static ValueGenerator()
	{
		_builtInGenerators = [];

		RegisterGlobalGenerator(new BooleanValueGenerator());
		RegisterGlobalGenerator(new DateValueGenerator());
		RegisterGlobalGenerator(new GuidValueGenerator());
		RegisterGlobalGenerator(new NumberValueGenerator());
		RegisterGlobalGenerator(new StringValueGenerator());
		RegisterGlobalGenerator(new TimeValueGenerator());
		RegisterGlobalGenerator(new VersionValueGenerator());
	}

	#endregion

	#region Methods

	/// <summary>
	/// Generate a single test value for the type.
	/// </summary>
	/// <param name="type"> The type to create a test value for. </param>
	/// <returns> The test value for the type. </returns>
	public static object GenerateValue(Type type)
	{
		var g = _builtInGenerators.FirstOrDefault(x => x.SupportsType(type));
		return g?.GenerateValue(type);
	}

	/// <summary>
	/// Gets a set of values for testing.
	/// </summary>
	/// <param name="type"> The type to get values for. </param>
	/// <returns> The values for testing. </returns>
	public static IList<object> GenerateValues(Type type)
	{
		var g = _builtInGenerators.FirstOrDefault(x => x.SupportsType(type));
		return g?.GenerateValues(type);
	}

	/// <summary>
	/// Get value combinations for provided type.
	/// </summary>
	/// <param name="type"> The type to get combinations for. </param>
	/// <returns> The values. </returns>
	public static IList<object> GetValueCombinations(Type type)
	{
		var results = GenerateValues(type);
		if (results != null)
		{
			return results;
		}

		if (type.IsEnum)
		{
			return EnumExtensions
				.GetAllEnumDetails(type)
				.Select(x => x.Key)
				.Cast<object>()
				.ToArray();
		}

		return Array.Empty<object>();
	}

	/// <summary>
	/// Register a value generator for all generator instances.
	/// </summary>
	/// <param name="generator"> The generator to register. </param>
	public static void RegisterGlobalGenerator(IValueGenerator generator)
	{
		if (_builtInGenerators.Contains(generator)
			|| _builtInGenerators.Any(x => x.GetType() == generator.GetType()))
		{
			// Already registered.
			return;
		}

		_builtInGenerators.Add(generator);
	}

	#endregion
}