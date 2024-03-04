#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents a project reference's project section within a in a .NET solution.
/// </summary>
public class DotNetSolutionProjectSection
{
	#region Fields

	private Dictionary<string, string> _dependencies;

	#endregion

	#region Properties

	/// <summary>
	/// Declared project dependencies.
	/// </summary>
	public Dictionary<string, string> Dependencies
	{
		get => _dependencies ??= new Dictionary<string, string>();
		set => _dependencies = value;
	}

	/// <summary>
	/// Project section name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Project section load type.
	/// </summary>
	public ProjectSectionType Type { get; set; }

	#endregion
}