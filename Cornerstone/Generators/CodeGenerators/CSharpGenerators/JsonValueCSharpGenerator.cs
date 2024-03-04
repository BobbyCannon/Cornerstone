#region References

using System;
using Cornerstone.Exceptions;
using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class JsonValueCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public JsonValueCSharpGenerator() : base(Activator.JsonValueTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		var code = value switch
		{
			null => "null",
			JsonNumber sValue => $"new JsonNumber(({CSharpCodeWriter.GetCodeTypeName(sValue.Value.GetType())}) {sValue})",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};
		codeWriter.Append(code);
	}

	#endregion
}