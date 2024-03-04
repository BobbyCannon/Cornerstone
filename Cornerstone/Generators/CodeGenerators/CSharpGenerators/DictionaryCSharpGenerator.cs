#region References

using System;
using System.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class DictionaryCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public DictionaryCSharpGenerator()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		var dictionary = (IDictionary) value;
		var keys = dictionary.Keys.ToObjectArray();
		var lastIndex = keys.Length - 1;

		codeWriter.AppendLine($"new {CSharpCodeWriter.GetCodeTypeName(type)}");
		codeWriter.AppendLineThenPushIndent('{');

		for (var index = 0; index <= lastIndex; index++)
		{
			var key = keys[index];

			//codeWriter.AppendLineThenPushIndent('{');
			//codeWriter.AppendObject(key);
			//codeWriter.AppendLine(",");
			//codeWriter.AppendObject(dictionary[key]);
			//codeWriter.PopIndent();
			//codeWriter.NewLine();
			//codeWriter.Append("}");

			codeWriter.Append("{ ");
			codeWriter.AppendObject(key);
			codeWriter.Append(", ");
			codeWriter.AppendObject(dictionary[key]);
			//codeWriter.PopIndent();
			//codeWriter.NewLine();
			codeWriter.Append(" }");

			if (index != lastIndex)
			{
				codeWriter.AppendLine(",");
			}
		}

		codeWriter.PopIndent();
		codeWriter.NewLine();

		codeWriter.Append('}');
	}

	/// <inheritdoc />
	public override bool SupportsType(Type type)
	{
		return type.ImplementsType<IDictionary>();
	}

	#endregion
}