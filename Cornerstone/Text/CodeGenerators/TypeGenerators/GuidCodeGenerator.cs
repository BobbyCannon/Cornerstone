#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class GuidCodeGenerator : CodeGenerator
{
	#region Constructors

	public GuidCodeGenerator() 
		: base(SourceTypes.GuidTypes)
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var code = value switch
		{
			null => "null",
			Guid sValue when sValue == Guid.Empty => "Guid.Empty",
			Guid sValue => $"Guid.Parse(\"{sValue:D}\")",
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};
		builder.Write(code);
	}

	#endregion
}