#region References

using System;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

/// <inheritdoc />
public class VersionCSharpGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public VersionCSharpGenerator() : base([typeof(Version)])
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
			Version { Build: -1 } sValue => $"new Version({sValue.Major}, {sValue.Minor})",
			Version { Revision: -1 } sValue => $"new Version({sValue.Major}, {sValue.Minor}, {sValue.Build})",
			Version sValue => $"new Version({sValue.Major}, {sValue.Minor}, {sValue.Build}, {sValue.Revision})",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};
		codeWriter.Append(code);
	}

	#endregion
}