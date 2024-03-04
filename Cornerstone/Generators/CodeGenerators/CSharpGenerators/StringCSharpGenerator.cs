#region References

using System;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class StringCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public StringCSharpGenerator() : base(
		ArrayExtensions.CombineArrays(
			Activator.StringTypes,
			Activator.CharTypes
		))
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
			string sValue when sValue == string.Empty => "string.Empty",
			string sValue => $"\"{sValue.Escape()}\"",
			char sValue => $"'{sValue.ToString().Escape()}'",
			GapBuffer<char> sValue => $"new GapBuffer<char>(\"{sValue.ToString().Escape()}\")",
			RopeBuffer<char> sValue => $"new RopeBuffer<char>(\"{sValue.ToString().Escape()}\")",
			StringBuilder sValue => $"new StringBuilder(\"{sValue.ToString().Escape()}\")",
			TextBuilder sValue => $"new TextBuilder(\"{sValue.ToString().Escape()}\")",
			JsonString sValue => $"new JsonString(\"{sValue.Value.Escape()}\")",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};

		codeWriter.Append(code);
	}

	#endregion
}