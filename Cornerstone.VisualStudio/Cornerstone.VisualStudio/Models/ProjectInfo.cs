#region References

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

#endregion

namespace Cornerstone.VisualStudio.Models;

/// <summary>
/// Holds information required by the designer about a project.
/// </summary>
internal class ProjectInfo
{
	#region Fields

	private IReadOnlyList<Project> _projectReferences;

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets a value indicating whether the project references the Avalonia desktop stack
	/// (e.g. Avalonia.Desktop / Avalonia.Win32 / Avalonia.Native).
	/// </summary>
	public bool HasAvaloniaDesktop { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the project has Avalonia design-time support
	/// (Avalonia.DesignerSupport or Avalonia previewer MSBuild paths).
	/// </summary>
	public bool HasAvaloniaDesignerSupport { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the project is an executable.
	/// </summary>
	public bool IsExecutable { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the project is a solution startup project.
	/// </summary>
	public bool IsStartupProject { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the project uses a web SDK
	/// (ASP.NET / Blazor / etc.) and is not an Avalonia desktop host.
	/// </summary>
	public bool IsWebProject { get; set; }

	/// <summary>
	/// Direct project-to-project references only (not transitive).
	/// </summary>
	public IReadOnlyList<Project> DirectProjectReferences { get; set; }

	public Lazy<IReadOnlyList<Project>> LazyProjectReferences { get; set; }

	/// <summary>
	/// Gets or sets the project name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the project's outputs.
	/// </summary>
	public IReadOnlyList<ProjectOutputInfo> Outputs { get; set; }

	/// <summary>
	/// Gets or sets the underlying EnvDTE project.
	/// </summary>
	public Project Project { get; set; }

	/// <summary>
	/// Gets or sets the project's project references.
	/// </summary>
	public IReadOnlyList<Project> ProjectReferences
	{
		get => _projectReferences ??= LazyProjectReferences?.Value;
		set => _projectReferences = value;
	}

	/// <summary>
	/// Gets or sets the project's assembly references.
	/// </summary>
	public IReadOnlyList<string> References { get; set; }

	/// <summary>
	/// True when this project can host the Avalonia XAML previewer for a control library.
	/// Must be an executable with Avalonia designer tooling and a HostApp path, not a web project.
	/// Platform (desktop vs browser/mobile) is filtered per-output when building targets.
	/// </summary>
	public bool IsAvaloniaDesktopHostCandidate =>
		IsExecutable &&
		!IsWebProject &&
		HasAvaloniaDesignerSupport &&
		(Outputs?.Any(o => !string.IsNullOrWhiteSpace(o.HostApp)) == true);

	#endregion
}