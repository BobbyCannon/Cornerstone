#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class FuncCodeGenerator : CodeGenerator
{
	#region Methods

	public override bool SupportsType(Type type)
	{
		return (type != null)
			&& typeof(Delegate).IsAssignableFrom(type);
	}

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		builder.Write("() =>");
		if (value is Delegate d
			&& (d.Method.GetParameters().Length == 0))
		{
			builder.WriteObject(d.DynamicInvoke());
		}
		else
		{
			builder.Write(" {}");
		}
	}

	#endregion
}