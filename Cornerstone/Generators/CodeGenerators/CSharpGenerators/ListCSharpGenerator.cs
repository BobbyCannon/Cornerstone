#region References

using System;
using System.Collections;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Text;

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
		var writer = (CSharpCodeWriter) codeWriter;

		if (list.Count <= 0)
		{
			writer.Append(writer.WritingPropertyValue ? "[]" : $"new {CSharpCodeWriter.GetCodeTypeName(type)}()");
			return;
		}

		var lastIndex = list.Count - 1;

		if (writer.WritingPropertyValue)
		{
			if (writer.Settings.TextFormat == TextFormat.Indented)
			{
				writer.AppendLine("[");
			}
			else
			{
				writer.Append("[");
			}
		}
		else
		{
			writer.AppendLine($"new {CSharpCodeWriter.GetCodeTypeName(type)}");
			writer.AppendLineThenPushIndent('{');
		}

		for (var index = 0; index <= lastIndex; index++)
		{
			var item = list[index];
			writer.AppendObject(item);

			if (index != lastIndex)
			{
				writer.AppendLine(",");
			}
		}

		if (writer.Settings.TextFormat == TextFormat.Indented)
		{
			writer.NewLine();
			writer.PopIndent();
		}

		writer.Append(writer.WritingPropertyValue ? "]" : "}");
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