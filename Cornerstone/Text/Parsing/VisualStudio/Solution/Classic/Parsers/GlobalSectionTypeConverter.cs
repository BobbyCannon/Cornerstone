namespace Cornerstone.Text.Parsing.VisualStudio.Solution.Classic.Parsers;

internal static class GlobalSectionTypeConverter
{
	#region Methods

	public static GlobalSectionType ConvertToType(string value)
	{
		switch (value)
		{
			case "preSolution":
				return GlobalSectionType.PreSolution;
			case "postSolution":
				return GlobalSectionType.PostSolution;
			default:
				return GlobalSectionType.Unknown;
		}
	}

	#endregion
}