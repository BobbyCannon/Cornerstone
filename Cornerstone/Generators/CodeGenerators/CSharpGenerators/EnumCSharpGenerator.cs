#region References

using System;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class EnumCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public EnumCSharpGenerator()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		codeWriter.Append($"{type.Name}.{value}");
	}

	/// <inheritdoc />
	public override bool SupportsType(Type type)
	{
		return type.IsEnum;
	}

	#endregion
}