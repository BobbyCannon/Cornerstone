#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.IO;

/// <summary>
/// The settings for a hierarchy.
/// </summary>
public class HierarchySettings : IHierarchySettings
{
	#region Constructors

	/// <summary>
	/// Initialize the settings.
	/// </summary>
	public HierarchySettings()
	{
		Exclusions = new SpeedyList<string>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Exclusions for the hierarchy.
	/// </summary>
	public SpeedyList<string> Exclusions { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool ShouldExclude(DirectoryInfo folder)
	{
		return Exclusions.Any(x =>
			x.Equals(folder.Name, StringComparison.OrdinalIgnoreCase)
		);
	}

	#endregion
}

/// <summary>
/// The settings for a hierarchy.
/// </summary>
public interface IHierarchySettings
{
	#region Properties

	/// <summary>
	/// Exclusions for the hierarchy.
	/// </summary>
	SpeedyList<string> Exclusions { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Check to see if a folder should be excluded.
	/// </summary>
	/// <param name="folder"> The folder to check. </param>
	/// <returns> True to exclude the folder otherwise false. </returns>
	bool ShouldExclude(DirectoryInfo folder);

	#endregion
}