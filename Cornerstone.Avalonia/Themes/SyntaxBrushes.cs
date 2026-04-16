#region References

using System.Collections.Generic;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Parsers;

#endregion

namespace Cornerstone.Avalonia.Themes;

public class SyntaxBrushes
{
	#region Constructors

	static SyntaxBrushes()
	{
		Brushes = new Dictionary<SyntaxColor, SolidColorBrush>();
		Refresh();
	}

	#endregion

	#region Properties

	public static Dictionary<SyntaxColor, SolidColorBrush> Brushes { get; }

	#endregion

	#region Methods

	public static void Refresh()
	{
		if (!ResourceService.TryGet<Brush>("SyntaxAttribute", out _))
		{
			return;
		}

		AddOrUpdate(SyntaxColor.Attribute, "SyntaxAttribute");
		AddOrUpdate(SyntaxColor.Comment, "SyntaxComment");
		AddOrUpdate(SyntaxColor.Error, "SyntaxError");
		AddOrUpdate(SyntaxColor.Keyword, "SyntaxKeyword");
		AddOrUpdate(SyntaxColor.Method, "SyntaxMethod");
		AddOrUpdate(SyntaxColor.Number, "SyntaxNumber");
		AddOrUpdate(SyntaxColor.Operator, "SyntaxOperator");
		AddOrUpdate(SyntaxColor.Preprocessor, "SyntaxPreprocessor");
		AddOrUpdate(SyntaxColor.Statement, "SyntaxStatement");
		AddOrUpdate(SyntaxColor.String, "SyntaxString");
		AddOrUpdate(SyntaxColor.Type, "SyntaxType");
		AddOrUpdate(SyntaxColor.Variable, "SyntaxVariable");
	}

	public static bool TryGetValue(SyntaxColor key, out SolidColorBrush brush)
	{
		if (Brushes.Count <= 0)
		{
			Refresh();
		}

		return Brushes.TryGetValue(key, out brush);
	}

	private static void AddOrUpdate(SyntaxColor key, string name)
	{
		if (ResourceService.GetBrush(name) is SolidColorBrush color)
		{
			Brushes[key] = color;
		}
	}

	#endregion
}