#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class VersionCodeGenerator : CodeGenerator
{
	#region Constructors

	public VersionCodeGenerator() : base(typeof(Version))
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var code = value switch
		{
			null => "null",
			Version { Build: -1 } sValue => $"new Version({sValue.Major}, {sValue.Minor})",
			Version { Revision: -1 } sValue => $"new Version({sValue.Major}, {sValue.Minor}, {sValue.Build})",
			Version sValue => $"new Version({sValue.Major}, {sValue.Minor}, {sValue.Build}, {sValue.Revision})",
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};
		builder.Write(code);
	}

	#endregion
}