#region References

using System.Drawing;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.CodeGenerators.TypeGenerators;

public class SystemDrawingCodeGenerator : CodeGenerator
{
	#region Constructors

	public SystemDrawingCodeGenerator()
		: base(typeof(Size))
	{
	}

	#endregion

	#region Methods

	public override void WriteObject(CodeBuilder builder, SourceTypeInfo typeInfo, object value)
	{
		var code = value switch
		{
			null => "null",
			Size s => $"new Size({s.Width}, {s.Height})",
			_ => throw new CornerstoneException($"Type ({typeInfo.Type.FullName}) not supported.")
		};

		builder.Write(code);
	}

	#endregion
}