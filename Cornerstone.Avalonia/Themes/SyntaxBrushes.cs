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
		Brushes = new Dictionary<SyntaxKind, SolidColorBrush>();
		Refresh();
	}

	#endregion

	#region Properties

	public static Dictionary<SyntaxKind, SolidColorBrush> Brushes { get; }

	#endregion

	#region Methods

	public static void Refresh()
	{
		if (!ResourceService.TryGet<Brush>("SyntaxAttribute", out _))
		{
			return;
		}

		AddOrUpdate(SyntaxKind.Attribute, "SyntaxAttribute");
		AddOrUpdate(SyntaxKind.Comment, "SyntaxComment");
		AddOrUpdate(SyntaxKind.Error, "SyntaxError");
		AddOrUpdate(SyntaxKind.Keyword, "SyntaxKeyword");
		AddOrUpdate(SyntaxKind.Method, "SyntaxMethod");
		AddOrUpdate(SyntaxKind.Number, "SyntaxNumber");
		AddOrUpdate(SyntaxKind.Operator, "SyntaxOperator");
		AddOrUpdate(SyntaxKind.Preprocessor, "SyntaxPreprocessor");
		AddOrUpdate(SyntaxKind.Statement, "SyntaxStatement");
		AddOrUpdate(SyntaxKind.String, "SyntaxString");
		AddOrUpdate(SyntaxKind.Type, "SyntaxType");
		AddOrUpdate(SyntaxKind.Variable, "SyntaxVariable");
	}

	public static bool TryGetValue(SyntaxKind key, out SolidColorBrush brush)
	{
		if (Brushes.Count <= 0)
		{
			Refresh();
		}

		return Brushes.TryGetValue(key, out brush);
	}

	private static void AddOrUpdate(SyntaxKind key, string name)
	{
		if (ResourceService.GetBrush(name) is SolidColorBrush color)
		{
			Brushes[key] = color;
		}
	}

	#endregion
}