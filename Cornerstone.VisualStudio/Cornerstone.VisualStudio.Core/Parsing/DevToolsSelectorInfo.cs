#region References

using System;
using System.Text;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

public record struct DevToolsSelectorInfo(Range ElementType, Range Namespace, Range AssemblyName = default)
{
	#region Methods

	public static string GetFullName(char[] buffer, DevToolsSelectorInfo info)
	{
		var sb = new StringBuilder();
		if (info.Namespace.Start.Value < info.Namespace.End.Value)
		{
			sb.Append(buffer[info.Namespace]);
			sb.Append('.');
		}
		sb.Append(buffer[info.ElementType]);
		return sb.ToString();
	}

	#endregion
}