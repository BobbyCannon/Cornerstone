#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.Preview;
using Cornerstone.VisualStudio.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Queries the projects in the current solution.
/// </summary>
internal class SolutionService
{
	#region Fields

	private readonly DTE _dte;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="SolutionService" /> class.
	/// </summary>
	/// <param name="dte"> The Visual Studio DTE. </param>
	public SolutionService(DTE dte)
	{
		_dte = dte;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets a list of projects in the current solution, waiting for the projects to be
	/// fully loaded.
	/// </summary>
	/// <remarks>
	/// There is no decent way (that I can find) to wait until all projects in a solution
	/// (including their references) are full loaded, so this method uses a series of hacks
	/// to try and do this. It may or may not be successful...
	/// </remarks>
	/// <returns> A collection of <see cref="ProjectInfo" /> objects. </returns>
	public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var result = new Dictionary<Project, ProjectInfo>();
		var uninitialized = new Dictionary<VSProject, ProjectInfo>();
		var startupProjects = ((Array) _dte.Solution.SolutionBuild.StartupProjects)?.Cast<string>().ToList();

		foreach (var project in FlattenProjects(_dte.Solution))
		{
			if (project.Object is VSProject vsProject)
			{
				var projectInfo = new ProjectInfo
				{
					IsStartupProject = startupProjects?.Contains(project.UniqueName) ?? false,
					Name = project.Name,
					UniqueName = TryGetUniqueName(project),
					FullName = TryGetFullName(project),
					Project = project,
					LazyProjectReferences = LazyGetProjectReferences(vsProject),
					References = GetReferences(vsProject)
				};

				result.Add(project, projectInfo);

				// If the project is a .csproj, and it has no references then we assume its
				// references are not yet loaded. We might want to handle e.g. F# and VB
				// projects here too.
				if (IsCsproj(projectInfo) && (projectInfo.References.Count == 0))
				{
					uninitialized.Add(vsProject, projectInfo);
				}
			}
		}

		if (uninitialized.Count > 0)
		{
			var tcs = new TaskCompletionSource<object>();

			foreach (var i in uninitialized)
			{
				void Handler(Reference reference)
				{
					// Here we're assuming that all references will be added at once, so the
					// fact we've got one means we've got them all. Is this guaranteed to be
					// true? No idea, but it *seems* to be the case.
					i.Value.LazyProjectReferences = LazyGetProjectReferences(i.Key);
					i.Value.References = GetReferences(i.Key);
					i.Key.Events.ReferencesEvents.ReferenceAdded -= Handler;
					uninitialized.Remove(i.Key);

					if (uninitialized.Count == 0)
					{
						tcs.SetResult(null);
					}
				}

				i.Key.Events.ReferencesEvents.ReferenceAdded += Handler;
			}

			await tcs.Task;
		}

		// Now everything should be loaded, loop back through the projects and add the output
		// info and recurse the project references.
		foreach (var item in result)
		{
			var outputType = TryGetProperty(item.Key?.Properties, "OutputType") ??
				TryGetProperty(item.Key?.ConfigurationManager?.ActiveConfiguration?.Properties, "OutputType");
			var outputTypeIsExecutable = (outputType == "0") || (outputType == "1") ||
				(outputType?.ToLowerInvariant() == "exe") ||
				(outputType?.ToLowerInvariant() == "winexe");

			item.Value.IsExecutable = outputTypeIsExecutable;
			item.Value.Outputs = await GetOutputInfoAsync(item.Key);

			// Capture direct refs, then flatten. Clear the ProjectReferences cache so the
			// next read uses the flattened lazy (otherwise the first get would stick to directs).
			var directRefs = item.Value.LazyProjectReferences?.Value ?? Array.Empty<Project>();
			item.Value.DirectProjectReferences = directRefs;
			item.Value.DirectProjectReferenceUniqueNames = UniqueNamesOf(directRefs);
			item.Value.LazyProjectReferences = LazyFlattenProjectReferences(result, directRefs);
			item.Value.ProjectReferences = null;

			// Classify Avalonia / web so the designer can exclude ASP.NET and mobile hosts.
			var refs = item.Value.References ?? Array.Empty<string>();
			item.Value.HasAvaloniaDesignerSupport =
				ContainsReference(refs, "Avalonia.DesignerSupport") ||
				(item.Value.Outputs?.Any(o => !string.IsNullOrWhiteSpace(o.AvaloniaHostApp)) == true);
			item.Value.HasCornerstoneDesignerSupport =
				ContainsReference(refs, "Cornerstone.Presentation") ||
				(item.Value.Outputs?.Any(o => !string.IsNullOrWhiteSpace(o.CornerstoneHostApp)) == true);
			item.Value.HasAvaloniaDesktop =
				ContainsReference(refs, "Avalonia.Desktop") ||
				ContainsReference(refs, "Avalonia.Win32") ||
				ContainsReference(refs, "Avalonia.Native") ||
				ContainsReference(refs, "Avalonia.X11") ||

				// Some templates only reference the metapackage; treat Avalonia + executable as desktop-ish
				// only when designer host tooling is present (HostApp) and it is not a web project.
				(ContainsReference(refs, "Avalonia") &&
					(item.Value.Outputs?.Any(o => !string.IsNullOrWhiteSpace(o.AvaloniaHostApp)) == true));
			item.Value.HasCornerstoneDesktop =
				ContainsReference(refs, "Cornerstone.Presentation") &&
				(item.Value.Outputs?.Any(o => !string.IsNullOrWhiteSpace(o.CornerstoneHostApp)) == true);
			item.Value.IsWebProject = await IsWebProjectAsync(item.Key, refs, item.Value.Outputs);
		}

		InheritCornerstoneDesignerSupport(result);

		foreach (var item in result.Values)
		{
			item.ProjectReferenceUniqueNames = UniqueNamesOf(item.ProjectReferences);
		}

		return result.Values.ToList();
	}

	/// <summary>
	/// Desktop hosts often only ProjectReference the CXAML library. That library imports
	/// CornerstoneBuildTasks (HostApp path + Presentation). The executable itself has neither
	/// a Cornerstone.Presentation assembly reference nor CornerstonePreviewerNetCoreToolPath.
	/// Inherit designer support and HostApp from the project graph.
	/// </summary>
	private static void InheritCornerstoneDesignerSupport(Dictionary<Project, ProjectInfo> projects)
	{
		var sharedHostApp = projects.Values
			.SelectMany(p => p.Outputs ?? Array.Empty<ProjectOutputInfo>())
			.Select(o => o.CornerstoneHostApp)
			.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

		if (string.IsNullOrWhiteSpace(sharedHostApp))
		{
			sharedHostApp = projects.Values
				.FirstOrDefault(p => string.Equals(p.Name, "Cornerstone.Designer.HostApp", StringComparison.OrdinalIgnoreCase))
				?.Outputs
				?.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.TargetAssembly))
				?.TargetAssembly;
		}

		// Hosts are often listed before the CXAML library (Album.Desktop before Album).
		// Repeat until no new flags so a later library can mark an earlier executable.
		var pending = true;
		var guard = 0;
		while (pending && (guard++ < 32))
		{
			pending = false;
			foreach (var item in projects.Values)
			{
				if (item.HasCornerstoneDesignerSupport)
				{
					continue;
				}

				var refs = item.ProjectReferences;
				if (refs == null)
				{
					continue;
				}

				for (var i = 0; i < refs.Count; i++)
				{
					var referenced = refs[i];
					var name = referenced?.Name;
					if (string.Equals(name, "Cornerstone.Presentation", StringComparison.OrdinalIgnoreCase))
					{
						item.HasCornerstoneDesignerSupport = true;
						pending = true;
						break;
					}

					if ((referenced != null) &&
						projects.TryGetValue(referenced, out var info) &&
						info.HasCornerstoneDesignerSupport)
					{
						item.HasCornerstoneDesignerSupport = true;
						pending = true;
						break;
					}

					if (referenced != null)
					{
						var referencedInfo = FindByIdentity(projects.Values, referenced);
						if ((referencedInfo != null) && referencedInfo.HasCornerstoneDesignerSupport)
						{
							item.HasCornerstoneDesignerSupport = true;
							pending = true;
							break;
						}
					}
				}
			}
		}

		foreach (var item in projects.Values)
		{
			if (!item.HasCornerstoneDesignerSupport)
			{
				continue;
			}

			if (item.IsExecutable && !item.IsWebProject)
			{
				item.HasCornerstoneDesktop = true;
			}

			if (string.IsNullOrWhiteSpace(sharedHostApp) || (item.Outputs == null))
			{
				continue;
			}

			for (var i = 0; i < item.Outputs.Count; i++)
			{
				var output = item.Outputs[i];
				if (string.IsNullOrWhiteSpace(output.CornerstoneHostApp))
				{
					output.CornerstoneHostApp = sharedHostApp;
				}
			}
		}
	}

	private static ProjectInfo FindByIdentity(IEnumerable<ProjectInfo> projects, Project project)
	{
		var unique = TryGetUniqueName(project);
		var full = TryGetFullName(project);
		foreach (var item in projects)
		{
			if (IsSameProject(item, unique, full, project))
			{
				return item;
			}
		}

		return null;
	}

	internal static bool IsSameProject(ProjectInfo item, Project project)
	{
		if ((item == null) || (project == null))
		{
			return false;
		}

		return IsSameProject(item, TryGetUniqueName(project), TryGetFullName(project), project);
	}

	private static bool IsSameProject(ProjectInfo item, string uniqueName, string fullName, Project project)
	{
		if (item.Project == project)
		{
			return true;
		}

		if (!string.IsNullOrEmpty(uniqueName) &&
			string.Equals(item.UniqueName, uniqueName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!string.IsNullOrEmpty(fullName) &&
			string.Equals(item.FullName, fullName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}

	private static IReadOnlyList<string> UniqueNamesOf(IReadOnlyList<Project> projects)
	{
		if ((projects == null) || (projects.Count == 0))
		{
			return Array.Empty<string>();
		}

		var names = new List<string>(projects.Count);
		for (var i = 0; i < projects.Count; i++)
		{
			var name = TryGetUniqueName(projects[i]);
			if (!string.IsNullOrEmpty(name))
			{
				names.Add(name);
			}
		}

		return names;
	}

	private static string TryGetUniqueName(Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		try
		{
			return project?.UniqueName;
		}
		catch
		{
			return null;
		}
	}

	private static string TryGetFullName(Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		try
		{
			return project?.FullName;
		}
		catch
		{
			return null;
		}
	}

	private static bool ContainsReference(IReadOnlyList<string> references, string name)
	{
		for (var i = 0; i < references.Count; i++)
		{
			if (string.Equals(references[i], name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static IReadOnlyList<Project> FlattenProjectReferences(
		Dictionary<Project, ProjectInfo> projects,
		IReadOnlyList<Project> references)
	{
		var result = new HashSet<Project>();

		foreach (var reference in references)
		{
			FlattenProjectReferences(projects, reference, result);
		}

		return result.ToList();
	}

	private static void FlattenProjectReferences(
		Dictionary<Project, ProjectInfo> projects,
		Project reference,
		HashSet<Project> result)
	{
		result.Add(reference);

		if (projects.TryGetValue(reference, out var info))
		{
			foreach (var child in info.ProjectReferences)
			{
				FlattenProjectReferences(projects, child, result);
			}
		}
	}

	private static IEnumerable<Project> FlattenProjects(IEnumerable projects)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		foreach (Project project in projects)
		{
			if (project.Object is VSProject)
			{
				yield return project;
			}
			else if (project.Object is SolutionFolder)
			{
				foreach (var child in FlattenSubProjects(project.ProjectItems))
				{
					yield return child;
				}
			}
		}
	}

	private static IEnumerable<Project> FlattenSubProjects(ProjectItems items)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		foreach (ProjectItem item in items)
		{
			var project = item.SubProject;

			if (project?.Object is VSProject)
			{
				yield return project;
			}
			else if (project?.Object is SolutionFolder)
			{
				foreach (var child in FlattenSubProjects(project.ProjectItems))
				{
					yield return child;
				}
			}
		}
	}

	private static string GetFrameworkInfo(
		ConfiguredProject loaded,
		ICollection<ProjectProperty> msbuildProperties,
		string keyTargetInfo)
	{
		return loaded.ProjectConfiguration.Dimensions.TryGetValue(keyTargetInfo, out var info)
			? info
			: GetMsBuildProperty(msbuildProperties, keyTargetInfo) ?? "unknown";
	}

	private static string GetMsBuildProperty(
		ICollection<ProjectProperty> msbuildProperties,
		string propertyName)
	{
		return msbuildProperties.FirstOrDefault(x => x.Name == propertyName)?.EvaluatedValue;
	}

	/// <summary>
	/// Avalonia 12.0 ships net8.0 and net10.0 designer hosts; later packs may only ship net8.0.
	/// Only rewrite when the net10.0 file exists so .axaml preview still finds HostApp.
	/// </summary>
	private static string PreferNet10HostApp(string hostAppPath, string targetFramework)
	{
		if (string.IsNullOrEmpty(hostAppPath)
			|| string.IsNullOrEmpty(targetFramework)
			|| !targetFramework.StartsWith("net10", StringComparison.OrdinalIgnoreCase)
			|| (hostAppPath.IndexOf(@"\net8.0\", StringComparison.OrdinalIgnoreCase) < 0))
		{
			return hostAppPath;
		}

		var net10Path = hostAppPath.Replace(@"\net8.0\", @"\net10.0\");
		return File.Exists(net10Path) ? net10Path : hostAppPath;
	}

	private static string GetActiveConfigurationName(Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		try
		{
			return project?.ConfigurationManager?.ActiveConfiguration?.ConfigurationName;
		}
		catch
		{
			return null;
		}
	}

	private static async Task<IReadOnlyList<ProjectOutputInfo>> GetOutputInfoAsync(Project project)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var alternatives = new Dictionary<string, ProjectOutputInfo>();
		var unconfigured = (project as IVsBrowseObjectContext)?.UnconfiguredProject;
		var activeConfiguration = GetActiveConfigurationName(project);

		if (unconfigured != null)
		{
			foreach (var loaded in unconfigured.LoadedConfiguredProjects)
			{
				if (loaded.GetType()
						.GetProperty("MSBuildProject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
						?.GetMethod.Invoke(loaded, null) is not Task<Microsoft.Build.Evaluation.Project> task)
				{
					continue;
				}

				var msbuildProperties = (await task).AllEvaluatedProperties;
				var targetPath = GetMsBuildProperty(msbuildProperties, "TargetPath");

				if (!string.IsNullOrWhiteSpace(targetPath))
				{
					var hostAppNetCore = GetMsBuildProperty(msbuildProperties, "AvaloniaPreviewerNetCoreToolPath");
					var hostAppNetFx = GetMsBuildProperty(msbuildProperties, "AvaloniaPreviewerNetFullToolPath");
					var cornerstoneHostAppNetCore = GetMsBuildProperty(msbuildProperties, "CornerstonePreviewerNetCoreToolPath");
					var cornerstoneHostAppNetFx = GetMsBuildProperty(msbuildProperties, "CornerstonePreviewerNetFullToolPath");

					var tf = GetFrameworkInfo(loaded, msbuildProperties, "TargetFramework");
					var tfi = GetFrameworkInfo(loaded, msbuildProperties, "TargetFrameworkIdentifier");
					var rid = GetFrameworkInfo(loaded, msbuildProperties, "RuntimeIdentifier");
					var tpi = GetFrameworkInfo(loaded, msbuildProperties, "TargetPlatformIdentifier");
					var configuration = GetFrameworkInfo(loaded, msbuildProperties, "Configuration");
					if (string.Equals(configuration, "unknown", StringComparison.OrdinalIgnoreCase))
					{
						configuration = GetMsBuildProperty(msbuildProperties, "Configuration");
					}

					if (!MsBuildConfigurationMatch.Matches(activeConfiguration, configuration, targetPath))
					{
						continue;
					}

					hostAppNetCore = PreferNet10HostApp(hostAppNetCore, tf);
					cornerstoneHostAppNetCore = PreferNet10HostApp(cornerstoneHostAppNetCore, tf);

					var hostApp = FrameworkInformation.IsNetFramework(tfi) ? hostAppNetFx : hostAppNetCore;
					var cornerstoneHostApp = FrameworkInformation.IsNetFramework(tfi)
						? cornerstoneHostAppNetFx
						: cornerstoneHostAppNetCore;

					alternatives[tf] = new ProjectOutputInfo(
						targetPath, tf, tfi, hostApp, rid, tpi, cornerstoneHostApp, configuration);
				}
			}
		}

		return alternatives.Values.ToList();
	}

	private static IReadOnlyList<Project> GetProjectReferences(VSProject project)
	{
		return project.References
			.OfType<Reference>()
			.Where(x => GetSourceProjectSafe(x) != null)
			.Select(x => x.SourceProject)
			.ToList();
	}

	private static IReadOnlyList<string> GetReferences(VSProject project)
	{
		return project.References
			.OfType<Reference>()
			.Where(x => GetSourceProjectSafe(x) == null)
			.Select(x => x.Name).ToList();
	}

	/// <summary>
	/// Returns the Reference's SourceProject. Returns null if any exceptions occur while
	/// attempting to access the Reference's SourceProject property.
	/// </summary>
	/// <remarks>
	/// COM Errors or NotImplementedException may be thrown when attempting to access the
	/// Reference's SourceProject property.
	/// </remarks>
	private static Project GetSourceProjectSafe(Reference reference)
	{
		try
		{
			return reference.SourceProject;
		}
		catch
		{
			return null;
		}
	}

	private bool IsCsproj(ProjectInfo projectInfo)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (!string.IsNullOrWhiteSpace(projectInfo.Project.FullName))
		{
			return string.Equals(
				Path.GetExtension(projectInfo.Project.FullName),
				".csproj",
				StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	/// <summary>
	/// Detects ASP.NET / Blazor / other web SDK projects that should never host the desktop previewer.
	/// </summary>
	private static async Task<bool> IsWebProjectAsync(
		Project project,
		IReadOnlyList<string> references,
		IReadOnlyList<ProjectOutputInfo> outputs)
	{
		var hasDesktopPlatformOutput = false;
		if (outputs != null)
		{
			for (var i = 0; i < outputs.Count; i++)
			{
				if (WebProjectDetection.HasDesktopPlatformOutput(outputs[i].TargetPlatformIdentifier))
				{
					hasDesktopPlatformOutput = true;
					break;
				}
			}
		}

		if (WebProjectDetection.IsLikelyWebProject(references, false, hasDesktopPlatformOutput))
		{
			return true;
		}

		if (hasDesktopPlatformOutput || WebProjectDetection.HasDesktopUiStack(references))
		{
			return false;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var unconfigured = (project as IVsBrowseObjectContext)?.UnconfiguredProject;
		if (unconfigured == null)
		{
			return WebProjectDetection.HasAspNetWebServerReferences(references);
		}

		foreach (var loaded in unconfigured.LoadedConfiguredProjects)
		{
			try
			{
				var task = loaded.GetType()
					.GetProperty("MSBuildProject",
						BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
					?.GetMethod?.Invoke(loaded, null) as Task<Microsoft.Build.Evaluation.Project>;

				if (task == null)
				{
					continue;
				}

				var msbuildProperties = (await task).AllEvaluatedProperties;
				var usingWebSdk = GetMsBuildProperty(msbuildProperties, "UsingMicrosoftNETSdkWeb");
				if (string.Equals(usingWebSdk, "true", StringComparison.OrdinalIgnoreCase))
				{
					return WebProjectDetection.IsLikelyWebProject(references, true, hasDesktopPlatformOutput);
				}

				var projectSdk = GetMsBuildProperty(msbuildProperties, "ProjectSdk")
					?? GetMsBuildProperty(msbuildProperties, "SDK");
				if (!string.IsNullOrEmpty(projectSdk) &&
					(projectSdk.IndexOf("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					return WebProjectDetection.IsLikelyWebProject(references, true, hasDesktopPlatformOutput);
				}
			}
			catch
			{
				// Ignore MSBuild reflection failures; assembly refs are enough for most cases.
			}
		}

		return false;
	}

	private static Lazy<IReadOnlyList<Project>> LazyFlattenProjectReferences(
		Dictionary<Project, ProjectInfo> projects,
		IReadOnlyList<Project> references)
	{
		return new Lazy<IReadOnlyList<Project>>(() => FlattenProjectReferences(projects, references));
	}

	private static Lazy<IReadOnlyList<Project>> LazyGetProjectReferences(VSProject project)
	{
		return new Lazy<IReadOnlyList<Project>>(() => GetProjectReferences(project));
	}

	private static string TryGetProperty(Properties props, string name)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		try
		{
			return props.Item(name).Value.ToString();
		}
		catch
		{
			return null;
		}
	}

	#endregion
}