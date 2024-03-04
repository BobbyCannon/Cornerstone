#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Generators.ValueGenerators;

/// <inheritdoc />
public abstract class ValueGenerator : IValueGenerator
{
	#region Fields

	private readonly Type[] _types;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes the value generator.
	/// </summary>
	/// <param name="types"> The types this value generator supports. </param>
	protected ValueGenerator(params Type[] types)
	{
		_types = types;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public abstract object GenerateValue(Type type);

	/// <inheritdoc />
	public abstract IList<object> GenerateValues(Type type);

	/// <inheritdoc />
	public Type[] GetSupportedTypes()
	{
		return _types;
	}

	/// <inheritdoc />
	public bool SupportsType(Type type)
	{
		return _types.Contains(type);
	}

	#endregion
}

/// <summary>
/// Represents a value generator
/// </summary>
public interface IValueGenerator
{
	#region Methods

	/// <summary>
	/// Generate a single test value for the type.
	/// </summary>
	/// <param name="type"> The type to create a test value for. </param>
	/// <returns> The test value for the type. </returns>
	object GenerateValue(Type type);

	/// <summary>
	/// Generate a set of test values for the type.
	/// </summary>
	/// <param name="type"> The type to create test values for. </param>
	/// <returns> The test values for the type. </returns>
	IList<object> GenerateValues(Type type);

	/// <summary>
	/// Return the types the value generator supports.
	/// </summary>
	/// <returns> The types this generator supports. </returns>
	Type[] GetSupportedTypes();

	/// <summary>
	/// Return true if the type provided is supported.
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> True if the type is supported. </returns>
	bool SupportsType(Type type);

	#endregion
}