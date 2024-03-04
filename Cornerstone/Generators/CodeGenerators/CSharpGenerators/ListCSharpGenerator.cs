#region References

using System;
using System.Collections;
using System.Reflection;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class ListCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public ListCSharpGenerator()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void AppendCode(ICodeWriter codeWriter, Type type, object value)
	{
		var list = ((IEnumerable) value).IterateList();

		if (list.Count <= 0)
		{
			codeWriter.Append($"new {CSharpCodeWriter.GetCodeTypeName(type)}()");
			return;
		}

		var lastIndex = list.Count - 1;
		codeWriter.AppendLine($"new {CSharpCodeWriter.GetCodeTypeName(type)}");
		codeWriter.AppendLineThenPushIndent('{');

		for (var index = 0; index <= lastIndex; index++)
		{
			var item = list[index];
			codeWriter.AppendObject(item);

			if (index != lastIndex)
			{
				codeWriter.AppendLine(",");
			}
		}

		codeWriter.NewLine();
		codeWriter.PopIndent();
		codeWriter.Append("}");
	}

	/// <inheritdoc />
	public override bool SupportsType(Type type)
	{
		if (type == typeof(string))
		{
			return false;
		}

		var info = type?.GetTypeInfo();
		return info is { IsArray: true }
			|| type.ImplementsType<IEnumerable>();
	}

	#endregion
}