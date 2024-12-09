#region References

using System;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public abstract class CSharpCodeGenerator : CodeGenerator
{
	#region Constructors

	/// <summary>
	/// Supports parseable types.
	/// </summary>
	protected CSharpCodeGenerator(params Type[] types) : base(types)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string GenerateCode(Type type, object value, ICodeWriterSettings settings)
	{
		var builder = new CSharpCodeWriter(settings);
		AppendCode(builder, type, value);
		return builder.ToString();
	}

	#endregion
}