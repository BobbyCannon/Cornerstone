#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents the load type for a project section.
/// </summary>
public enum ProjectSectionType
{
	/// <summary>
	/// Unknown load type (type could not be determined).
	/// </summary>
	Unknown,

	/// <summary>
	/// Load pre-project.
	/// </summary>
	[Display(Name = "preProject")]
	PreProject,

	/// <summary>
	/// Load post-project.
	/// </summary>
	[Display(Name = "postProject")]
	PostProject
}