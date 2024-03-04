#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents the load type for a global section.
/// </summary>
public enum GlobalSectionType
{
	/// <summary>
	/// Unknown load type (type could not be determined).
	/// </summary>
	Unknown,

	/// <summary>
	/// Load pre-solution.
	/// </summary>
	[Display(Name = "preSolution")]
	PreSolution,

	/// <summary>
	/// Load post-solution.
	/// </summary>
	[Display(Name = "postSolution")]
	PostSolution
}