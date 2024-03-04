namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal static class ProjectSectionTypeConverter
{
    #region Methods

    public static ProjectSectionType ConvertToType(string value)
    {
        switch (value)
        {
            case "preProject":
                return ProjectSectionType.PreProject;
            case "postProject":
                return ProjectSectionType.PostProject;
            default:
                return ProjectSectionType.Unknown;
        }
    }

    #endregion
}