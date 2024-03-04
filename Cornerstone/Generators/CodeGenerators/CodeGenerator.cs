#region References

using System;
using System.Linq;

#endregion

namespace Cornerstone.Generators.CodeGenerators;

/// <inheritdoc />
public abstract class CodeGenerator : ICodeGenerator
{
	#region Fields

	private readonly Type[] _supportedTypes;

	#endregion

	#region Constructors

	/// <summary>
	/// Supports parseable types.
	/// </summary>
	protected CodeGenerator(params Type[] types)
	{
		_supportedTypes = types;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public abstract void AppendCode(ICodeWriter codeWriter, Type type, object value);

	/// <inheritdoc />
	public abstract string GenerateCode(Type type, object value, ICodeWriterOptions options);

	/// <inheritdoc />
	public Type[] GetSupportedTypes()
	{
		return _supportedTypes;
	}

	/// <inheritdoc />
	public virtual bool SupportsType(Type type)
	{
		return _supportedTypes.Contains(type);
	}

	#endregion
}

/// <summary>
/// Represents a code generator
/// </summary>
public interface ICodeGenerator
{
	#region Methods

	/// <summary>
	/// Generate the code for the value.
	/// </summary>
	/// <param name="codeWriter"> The writer to write the code to. </param>
	/// <param name="type"> The type to be converted to code. </param>
	/// <param name="value"> The value to generate code for. </param>
	/// <returns> The code version of the value. </returns>
	void AppendCode(ICodeWriter codeWriter, Type type, object value);

	/// <summary>
	/// Generate the code for the value.
	/// </summary>
	/// <param name="type"> The type to be converted to code. </param>
	/// <param name="value"> The value to generate code for. </param>
	/// <param name="options"> The settings for generating the code. </param>
	/// <returns> The code version of the value. </returns>
	string GenerateCode(Type type, object value, ICodeWriterOptions options);

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