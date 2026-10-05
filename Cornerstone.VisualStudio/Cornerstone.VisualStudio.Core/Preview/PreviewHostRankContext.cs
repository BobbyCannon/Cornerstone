namespace Cornerstone.VisualStudio.Core.Preview;

/// <summary>
/// Inputs for ranking a preview-host candidate against the XAML project being edited.
/// </summary>
public sealed class PreviewHostRankContext
{
	#region Constructors

	public PreviewHostRankContext(
		string xamlProjectName,
		string candidateName,
		bool candidateIsXamlProject,
		bool hasDesktopStack,
		bool isStartupProject,
		bool directlyReferencesXamlProject)
	{
		XamlProjectName = xamlProjectName;
		CandidateName = candidateName;
		CandidateIsXamlProject = candidateIsXamlProject;
		HasDesktopStack = hasDesktopStack;
		IsStartupProject = isStartupProject;
		DirectlyReferencesXamlProject = directlyReferencesXamlProject;
	}

	#endregion

	#region Properties

	public string CandidateName { get; }

	public bool CandidateIsXamlProject { get; }

	public bool DirectlyReferencesXamlProject { get; }

	public bool HasDesktopStack { get; }

	public bool IsStartupProject { get; }

	public string XamlProjectName { get; }

	#endregion
}
