#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents a global section within a .NET solution.
/// </summary>
public class DotNetSolutionGlobalSection
{
	#region Fields

	private Dictionary<string, string> _settings;

	#endregion

	#region Properties

	/// <summary>
	/// The global section name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The global section settings.
	/// </summary>
	public Dictionary<string, string> Settings
	{
		get => _settings ??= new Dictionary<string, string>();
		set => _settings = value;
	}

	/// <summary>
	/// The global section load type.
	/// </summary>
	public GlobalSectionType Type { get; set; }

	#endregion
}