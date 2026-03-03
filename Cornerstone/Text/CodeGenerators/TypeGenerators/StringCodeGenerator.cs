#region References

using System.Text;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class StringCodeGenerator : CodeGenerator
{
	#region Constructors

	public StringCodeGenerator() : base(
		ArrayExtensions.CombineArrays(
			SourceTypes.StringTypes,
			SourceTypes.CharTypes
		))
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var code = value switch
		{
			null => "null",
			string sValue when sValue == string.Empty => "string.Empty",
			string sValue => $"\"{sValue.Escape()}\"",
			char sValue => $"'{sValue.ToString().Escape()}'",
			StringGapBuffer sValue => $"new StringGapBuffer(\"{sValue.ToString().Escape()}\")",
			GapBuffer<char> sValue => $"new GapBuffer<char>(\"{sValue.ToString().Escape()}\")",
			StringBuilder sValue => $"new StringBuilder(\"{sValue.ToString().Escape()}\")",
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};

		builder.Write(code);
	}

	#endregion
}