namespace Cornerstone.Parsers.Html;

public static class SyntaxColorExtensions
{
	#region Methods

	public static string ToHtmlColor(this SyntaxColor color)
	{
		return color switch
		{
			SyntaxColor.Argument => "#9CDCFE",
			SyntaxColor.Command => "#00FFFF",
			SyntaxColor.Comment => "#55B030",
			SyntaxColor.Identifier => "#44C8B0",
			SyntaxColor.Keyword => "#4E9AD3",
			SyntaxColor.KeywordMuted => "#808080",
			SyntaxColor.KeywordControl => "#D8A0DF",
			SyntaxColor.Link => "#0066FF",
			SyntaxColor.Member => "#EE82EE",
			SyntaxColor.Method => "#DCDCAA",
			SyntaxColor.Number => "#B5CEA8",
			SyntaxColor.Parameter => "#78C4CF",
			SyntaxColor.ParameterName => "#78C4CF",
			SyntaxColor.String => "#D69D85",
			SyntaxColor.Variable => "#9CDCFE",
			_ => "#D8D8D8"
		};
	}

	#endregion
}