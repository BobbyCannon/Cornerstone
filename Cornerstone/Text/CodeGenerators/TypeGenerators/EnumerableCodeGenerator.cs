#region References

using System;
using System.Collections;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class EnumerableCodeGenerator : CodeGenerator
{
	#region Methods

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

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo type, object value)
	{
		var list = ((IEnumerable) value).IterateList();
		var propertyMode = builder.Mode == CodeBuilderMode.Property;

		if (list.Count <= 0)
		{
			builder.Write(propertyMode ? " []" : $" new {CodeBuilder.GetCodeTypeName(type.Type)}()");
			return;
		}

		if (propertyMode)
		{
			builder.WriteLine();
			builder.IndentWriteLine("[");
		}
		else
		{
			builder.Write($"new {CodeBuilder.GetCodeTypeName(type.Type)}");
			builder.WriteLine("{");
		}

		builder.IncreaseIndent();

		var first = true;

		foreach (var item in list)
		{
			if (!first)
			{
				builder.WriteLine(",");
			}

			builder.WriteObject(item);
			first = false;
		}

		//if (builder.Settings.TextFormat == TextFormat.Indented)
		//{
		//	builder.WriteLine();
		//	builder.DecreaseIndent();
		//}

		builder.DecreaseIndent();
		builder.WriteLine();
		builder.IndentWrite(propertyMode ? "]" : "}");
	}

	#endregion
}