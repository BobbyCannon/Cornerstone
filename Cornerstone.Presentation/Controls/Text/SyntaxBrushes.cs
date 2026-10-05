#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Text.Parsing;

#endregion

namespace Cornerstone.Presentation.Controls.Text;

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
		if (!TryGetSolidBrush("SyntaxAttribute", out _))
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
		if (TryGetSolidBrush(name, out var color))
		{
			Brushes[key] = color;
		}
	}

	private static bool TryGetSolidBrush(string name, out SolidColorBrush brush)
	{
		brush = null;
		var app = Application.Current;
		if (app == null)
		{
			return false;
		}

		if (!app.TryGetResource(name, app.ActualThemeVariant, out var found) || (found == null))
		{
			return false;
		}

		switch (found)
		{
			case SolidColorBrush solid:
			{
				brush = solid;
				return true;
			}
			case Color color:
			{
				brush = new SolidColorBrush(color);
				return true;
			}
			case ISolidColorBrush solidBrush:
			{
				brush = new SolidColorBrush(solidBrush.Color);
				return true;
			}
			default:
			{
				return false;
			}
		}
	}

	#endregion
}