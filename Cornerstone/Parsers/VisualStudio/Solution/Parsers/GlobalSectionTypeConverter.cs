namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

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