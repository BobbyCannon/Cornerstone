#region References

using System;
using System.Drawing;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Generators.CodeGenerators.CSharpGenerators;

public class SystemDrawingGenerator : CSharpCodeGenerator
{
	#region Constructors

	/// <inheritdoc />
	public SystemDrawingGenerator()
		: base(typeof(Size))
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
			Size s => $"new Size({s.Width}, {s.Height})",
			_ => throw new CornerstoneException($"Type ({type.FullName}) not supported.")
		};

		codeWriter.Append(code);
	}

	#endregion
}