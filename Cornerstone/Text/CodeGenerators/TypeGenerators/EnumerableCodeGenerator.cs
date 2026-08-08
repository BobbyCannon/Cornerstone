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
			if (propertyMode)
			{
				builder.Append(" []");
			}
			else
			{
				builder.Append(" new ");
				builder.Append(CodeBuilder.GetCodeTypeName(type.Type));
				builder.Append("()");
			}
			return;
		}

		if (propertyMode)
		{
			builder.AppendLine();
			builder.IndentWriteLine("[");
		}
		else
		{
			builder.Append("new ");
			builder.AppendLine(CodeBuilder.GetCodeTypeName(type.Type));
			builder.AppendLine("{");
		}

		builder.IncreaseIndent();

		var first = true;

		foreach (var item in list)
		{
			if (!first)
			{
				builder.AppendLine(",");
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
		builder.AppendLine();
		builder.IndentWrite(propertyMode ? "]" : "}");
	}

	#endregion
}