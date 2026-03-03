#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class EnumCodeGenerator : CodeGenerator
{
	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		builder.Write($"{typeInfo.Type.Name}.{value}");
	}

	public override bool SupportsType(Type type)
	{
		return type.IsEnum;
	}

	#endregion
}