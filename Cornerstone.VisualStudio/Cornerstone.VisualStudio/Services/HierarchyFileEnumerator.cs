#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Enumerates physical project files from Solution Explorer selection or an entire solution.
/// </summary>
internal static class HierarchyFileEnumerator
{
	#region Methods

	/// <summary>
	/// Collects file paths under the current Solution Explorer selection.
	/// </summary>
	public static IReadOnlyList<string> GetSelectedFilePaths(DTE dte, Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		pathFilter ??= _ => true;

		var selectedItems = GetSelectedUiItems(dte);
		if (selectedItems.Count == 0)
		{
			return Array.Empty<string>();
		}

		foreach (var item in selectedItems)
		{
			try
			{
				CollectFromUiHierarchyItem(item, results, pathFilter);
			}
			catch
			{
				// Ignore individual selection failures (unloaded projects, etc.).
			}
		}

		return results.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
	}

	/// <summary>
	/// Collects all matching file paths in the open solution.
	/// </summary>
	public static IReadOnlyList<string> GetSolutionFilePaths(DTE dte, Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		pathFilter ??= _ => true;

		if (dte.Solution == null)
		{
			return Array.Empty<string>();
		}

		foreach (var project in FlattenProjects(dte.Solution))
		{
			CollectFromProject(project, results, pathFilter);
		}

		return results.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static void CollectFromUiHierarchyItem(
		UIHierarchyItem item,
		ISet<string> results,
		Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		switch (item.Object)
		{
			case ProjectItem projectItem:
				CollectFromProjectItem(projectItem, results, pathFilter);
				break;
			case Project project:
				if (project.Object is SolutionFolder)
				{
					CollectFromSolutionFolder(project, results, pathFilter);
				}
				else
				{
					CollectFromProject(project, results, pathFilter);
				}

				break;
			case Solution solution:
				foreach (var p in FlattenProjects(solution))
				{
					CollectFromProject(p, results, pathFilter);
				}

				break;
			default:
				// Nested solution explorer nodes sometimes expose Object as something else;
				// recurse UI children when present.
				if (item.UIHierarchyItems != null)
				{
					foreach (UIHierarchyItem child in item.UIHierarchyItems)
					{
						CollectFromUiHierarchyItem(child, results, pathFilter);
					}
				}

				break;
		}
	}

	private static void CollectFromSolutionFolder(
		Project solutionFolder,
		ISet<string> results,
		Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (solutionFolder?.ProjectItems == null)
		{
			return;
		}

		foreach (ProjectItem item in solutionFolder.ProjectItems)
		{
			if (item.SubProject != null)
			{
				if (item.SubProject.Object is SolutionFolder)
				{
					CollectFromSolutionFolder(item.SubProject, results, pathFilter);
				}
				else if (item.SubProject.Object is VSProject)
				{
					CollectFromProject(item.SubProject, results, pathFilter);
				}
			}
			else
			{
				CollectFromProjectItem(item, results, pathFilter);
			}
		}
	}

	private static void CollectFromProject(
		Project project,
		ISet<string> results,
		Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (project?.ProjectItems == null)
		{
			return;
		}

		try
		{
			// Skip un-loaded projects.
			if (string.Equals(project.Kind, EnvDTE.Constants.vsProjectKindUnmodeled, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}
		catch
		{
			return;
		}

		foreach (ProjectItem item in project.ProjectItems)
		{
			CollectFromProjectItem(item, results, pathFilter);
		}
	}

	private static void CollectFromProjectItem(
		ProjectItem item,
		ISet<string> results,
		Func<string, bool> pathFilter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (item == null)
		{
			return;
		}

		try
		{
			if (item.SubProject != null)
			{
				if (item.SubProject.Object is SolutionFolder)
				{
					CollectFromSolutionFolder(item.SubProject, results, pathFilter);
				}
				else
				{
					CollectFromProject(item.SubProject, results, pathFilter);
				}

				return;
			}

			// Physical files
			short fileCount = 0;
			try
			{
				fileCount = item.FileCount;
			}
			catch
			{
				fileCount = 0;
			}

			if (fileCount > 0)
			{
				for (short i = 1; i <= fileCount; i++)
				{
					string path = null;
					try
					{
						path = item.FileNames[i];
					}
					catch
					{
						// ignored
					}

					if (!string.IsNullOrWhiteSpace(path) &&
						File.Exists(path) &&
						pathFilter(path))
					{
						results.Add(Path.GetFullPath(path));
					}
				}
			}

			if (item.ProjectItems != null && item.ProjectItems.Count > 0)
			{
				foreach (ProjectItem child in item.ProjectItems)
				{
					CollectFromProjectItem(child, results, pathFilter);
				}
			}
		}
		catch
		{
			// Ignore items that throw when expanding (some CPS virtual nodes).
		}
	}

	private static List<UIHierarchyItem> GetSelectedUiItems(DTE dte)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var list = new List<UIHierarchyItem>();
		try
		{
			var dte2 = dte as DTE2;
			var solutionExplorer = dte2?.ToolWindows?.SolutionExplorer;
			if (solutionExplorer?.SelectedItems is not object[] selected)
			{
				return list;
			}

			foreach (var o in selected)
			{
				if (o is UIHierarchyItem ui)
				{
					list.Add(ui);
				}
			}
		}
		catch
		{
			// ignored
		}

		return list;
	}

	private static IEnumerable<Project> FlattenProjects(Solution solution)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (solution?.Projects == null)
		{
			yield break;
		}

		foreach (Project project in solution.Projects)
		{
			foreach (var p in FlattenProjectTree(project))
			{
				yield return p;
			}
		}
	}

	private static IEnumerable<Project> FlattenProjectTree(Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (project == null)
		{
			yield break;
		}

		if (project.Object is VSProject)
		{
			yield return project;
			yield break;
		}

		if (project.Object is SolutionFolder)
		{
			if (project.ProjectItems == null)
			{
				yield break;
			}

			foreach (ProjectItem item in project.ProjectItems)
			{
				if (item.SubProject != null)
				{
					foreach (var child in FlattenProjectTree(item.SubProject))
					{
						yield return child;
					}
				}
			}
		}
	}

	#endregion
}
