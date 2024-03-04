#region References

using System;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class GuidCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public GuidCSharpGenerator() : base(Activator.GuidTypes)
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
			Guid sValue when sValue == Guid.Empty => "Guid.Empty",
			Guid sValue => $"Guid.Parse(\"{sValue:D}\")",
			ShortGuid sValue when sValue == ShortGuid.Empty => "ShortGuid.Empty",
			ShortGuid sValue => $"ShortGuid.Parse(\"{sValue.Value}\")",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};
		codeWriter.Append(code);
	}

	#endregion
}