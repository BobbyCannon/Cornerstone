#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Parsers.VisualStudio.Project;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents a project reference within a .NET solution.
/// </summary>
public class DotNetSolutionProject : DotNetProject
{
	#region Fields

	private IList<DotNetSolutionProjectSection> _projectSections;
	private readonly DotNetSolution _solution;
	private bool? _supportsPackageReference;

	#endregion

	#region Constructors

	internal DotNetSolutionProject(DotNetSolution solution, string project)
	{
		_solution = solution;

		ProjectName = project;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Relative project file location.
	/// </summary>
	public string AbsolutePath => $"{_solution.Directory}\\{RelativePath}";

	/// <summary>
	/// Unique project ID.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// True if the project supports "PackageReference" nuget.
	/// </summary>
	public bool SupportsPackageReferences => _supportsPackageReference ??= CheckIfSupportsPackageReferences();

	/// <summary>
	/// The name of the project.
	/// </summary>
	public string ProjectName { get; }

	/// <summary>
	/// Collection of project section settings.
	/// </summary>
	public IList<DotNetSolutionProjectSection> ProjectSections
	{
		get => _projectSections ??= new List<DotNetSolutionProjectSection>();
		set => _projectSections = value;
	}

	/// <summary>
	/// Relative project file location.
	/// </summary>
	public string RelativePath { get; set; }

	/// <summary>
	/// Project type.
	/// </summary>
	public DotNetProjectType Type { get; set; }

	/// <summary>
	/// Project type id.
	/// </summary>
	public Guid TypeId { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Load the project from the path.
	/// </summary>
	public void Load()
	{
		if (!File.Exists(AbsolutePath))
		{
			return;
		}

		LoadFile(AbsolutePath);
	}

	/// <summary>
	/// Save the changes to the project.
	/// </summary>
	public void Save()
	{
		if (!Path.HasExtension(AbsolutePath))
		{
			return;
		}

		SaveFile(AbsolutePath);
	}

	/// <summary>
	/// Returns true if the project is a classic project.
	/// </summary>
	/// <returns> True if classic otherwise false. </returns>
	private bool CheckIfSupportsPackageReferences()
	{
		var types = new List<DotNetProjectType>(ProjectTypes) { Type };
		var isOldProject = types
			.Any(x => x is
				DotNetProjectType.AspNet5
				or DotNetProjectType.AspNetMvc1
				or DotNetProjectType.AspNetMvc2
				or DotNetProjectType.AspNetMvc3
				or DotNetProjectType.AspNetMvc4
				or DotNetProjectType.AspNetMvc5
				or DotNetProjectType.XamarinAndroid
				or DotNetProjectType.XamarinAndroidBinding
				or DotNetProjectType.XamarinIos
				or DotNetProjectType.XamarinIosBinding
			);

		return !isOldProject;
	}

	#endregion
}