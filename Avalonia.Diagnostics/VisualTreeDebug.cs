#region References

using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Utilities;

#endregion

namespace Avalonia.Diagnostics;

public static class VisualTreeDebug
{
	#region Methods

	public static string PrintVisualTree(Visual visual)
	{
		var result = StringBuilderCache.Acquire();
		PrintVisualTree(visual, result, 0);
		return StringBuilderCache.GetStringAndRelease(result);
	}

	private static string Indent(int indent)
	{
		return new string(' ', Math.Max(indent, 0) * 4);
	}

	private static void PrintVisualTree(Visual visual, StringBuilder builder, int indent)
	{
		var control = visual as Control;

		builder.Append(Indent(indent - 1));

		if (indent > 0)
		{
			builder.Append(" +- ");
		}

		builder.Append(visual.GetType().Name);

		if (control != null)
		{
			builder.Append(" ");
			builder.AppendLine(control.Classes.ToString());

			foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(control))
			{
				var value = control.GetDiagnostic(property);

				if (value.Priority != BindingPriority.Unset)
				{
					builder.Append(Indent(indent));
					builder.Append(" |  ");
					builder.Append(value.Property.Name);
					builder.Append(" = ");
					builder.Append(value.Value ?? "(null)");
					builder.Append(" [");
					builder.Append(value.Priority);
					builder.AppendLine("]");
				}
			}
		}
		else
		{
			builder.AppendLine();
		}

		foreach (var child in visual.VisualChildren)
		{
			PrintVisualTree(child, builder, indent + 1);
		}
	}

	#endregion
}