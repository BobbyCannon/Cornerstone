#region References

using System;

#endregion

namespace Cornerstone.Text.CodeGenerators;

public class CodeBuilderSettings
{
	#region Constructors

	public CodeBuilderSettings()
	{
		DesiredOutput = CodeBuilderOutput.Instance;
		IndentChar = '\t';
		IndentLength = 1;
		NewLineChars = Environment.NewLine;
	}

	#endregion

	#region Properties

	public CodeBuilderOutput DesiredOutput { get; set; }

	public char IndentChar { get; set; }

	public uint IndentLength { get; set; }

	public string NewLineChars { get; set; }

	#endregion
}