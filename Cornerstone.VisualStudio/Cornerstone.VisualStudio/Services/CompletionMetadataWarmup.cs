#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Starts completion metadata loads when a solution is open, and again after a build
/// when the assemblies on disk have changed.
/// </summary>
internal sealed class CompletionMetadataWarmup
{
	#region Fields

	private BuildEvents _buildEvents;
	private DTE _dte;

	// DTE does not root this. If it is collected, build and solution sinks stop
	// and metadata never reloads after the first solution open.
	private Events _events;
	private int _running;
	private int _runAgain;
	private SolutionEvents _solutionEvents;
	private int _startupRequested;

	#endregion

	#region Methods

	public void Start(DTE dte)
	{
		_dte = dte;
		_events = dte.Events;
		if (_events == null)
		{
			return;
		}

		_buildEvents = _events.BuildEvents;
		_solutionEvents = _events.SolutionEvents;
		_buildEvents.OnBuildDone += OnBuildDone;
		_solutionEvents.Opened += OnSolutionOpened;
		if ((dte.Solution != null) && dte.Solution.IsOpen)
		{
			RequestStartupWarm();
		}
	}

	private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
	{
		try
		{
			Log.Information("Completion metadata refresh after build ({Scope}, {Action})", scope, action);
			RequestWarm();
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Completion metadata rebuild check failed");
		}
	}

	private void OnSolutionOpened()
	{
		RequestStartupWarm();
	}

	private void RequestStartupWarm()
	{
		if (System.Threading.Interlocked.Exchange(ref _startupRequested, 1) == 1)
		{
			return;
		}

		RequestWarm();
	}

	private void RequestWarm()
	{
		if (System.Threading.Interlocked.CompareExchange(ref _running, 1, 0) != 0)
		{
			System.Threading.Volatile.Write(ref _runAgain, 1);
			return;
		}

		WarmLoopAsync().FireAndForget();
	}

	private async Task WarmLoopAsync()
	{
		try
		{
			do
			{
				System.Threading.Volatile.Write(ref _runAgain, 0);
				await WarmAsync().ConfigureAwait(true);
			}
			while (System.Threading.Interlocked.Exchange(ref _runAgain, 0) == 1);
		}
		finally
		{
			System.Threading.Interlocked.Exchange(ref _running, 0);
			if (System.Threading.Volatile.Read(ref _runAgain) == 1)
			{
				RequestWarm();
			}
		}
	}

	private async Task WarmAsync()
	{
		EditorHostActivity.Enter();
		try
		{
			EditorHostActivity.SetMessage("Finding assemblies");
			var projects = await CornerstonePackage.SolutionService.GetProjectsAsync().ConfigureAwait(true);
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var targetSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var assemblies = new List<string>();
			var targets = new List<string>();
			for (var i = 0; i < projects.Count; i++)
			{
				var project = projects[i];
				var projectLabel = string.IsNullOrEmpty(project.Name) ? "project" : project.Name;
				EditorHostActivity.SetMessage((i + 1) + " of " + projects.Count + " - Collecting references of " + projectLabel);
				if (!project.HasCornerstoneDesignerSupport && !project.HasAvaloniaDesignerSupport)
				{
					continue;
				}

				var outputs = project.Outputs;
				if (outputs == null)
				{
					continue;
				}

				for (var o = 0; o < outputs.Count; o++)
				{
					var output = outputs[o];
					if (!output.IsDesignerRunnableFramework || string.IsNullOrWhiteSpace(output.TargetAssembly))
					{
						continue;
					}

					if (!File.Exists(output.TargetAssembly))
					{
						continue;
					}

					AddDistinct(targets, targetSeen, output.TargetAssembly);
					AddDistinct(assemblies, seen, output.TargetAssembly);
					var paths = AssemblyPathsFor(project.Project, output.TargetAssembly);
					for (var p = 0; p < paths.Count; p++)
					{
						AddDistinct(assemblies, seen, paths[p]);
					}
				}
			}

			if (assemblies.Count == 0)
			{
				EditorHostActivity.SetMessage("No assemblies to read");
				Log.Information("Completion metadata refresh found no assemblies");
				return;
			}

			await LoadAndLogAsync(assemblies, targets, SolutionDirectory()).ConfigureAwait(true);
			Log.Information("Completion metadata warm-up finished ({Count} assemblies)", assemblies.Count);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Completion metadata warm-up failed");
		}
		finally
		{
			EditorHostActivity.Exit();
		}
	}

	private static void AddDistinct(List<string> paths, HashSet<string> seen, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		string full;
		try
		{
			full = Path.GetFullPath(path);
		}
		catch (Exception)
		{
			return;
		}

		if (seen.Add(full))
		{
			paths.Add(full);
		}
	}

	private static async Task LoadAndLogAsync(List<string> paths, List<string> targets, string solutionDirectory)
	{
		try
		{
			var response = await EditorHostSession.EnsureMetadataAsync(paths, solutionDirectory, targets).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(response.Error))
			{
				Log.Error("Completion metadata load failed: {Error}", response.Error);
			}
			else
			{
				var reused = paths.Count - response.ReadCount;
				if (reused < 0)
				{
					reused = 0;
				}

				if (response.ReadCount > 0)
				{
					Log.Information(
						"Completion metadata refreshed {Read} of {Assemblies} assemblies in {Seconds:0.0}s ({Reused} unchanged)",
						response.ReadCount,
						paths.Count,
						response.Seconds,
						reused);
				}
				else
				{
					Log.Information(
						"Completion metadata unchanged ({Assemblies} assemblies, {Seconds:0.0}s)",
						paths.Count,
						response.Seconds);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Completion metadata load failed");
		}
	}

	private string SolutionDirectory()
	{
		try
		{
			var fullName = _dte?.Solution?.FullName;
			if (string.IsNullOrWhiteSpace(fullName))
			{
				return string.Empty;
			}

			return Path.GetDirectoryName(fullName) ?? string.Empty;
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	private static List<string> AssemblyPathsFor(Project project, string targetAssembly)
	{
		IAssemblyProvider provider = null;
		if (project != null)
		{
			provider = VsProjectAssembliesProvider.TryCreate(project, targetAssembly);
		}

		if (provider == null)
		{
			provider = new DepsJsonFileAssemblyProvider(targetAssembly, targetAssembly);
		}

		var paths = new List<string>();
		foreach (var path in provider.GetAssemblies())
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				paths.Add(path);
			}
		}

		return paths;
	}

	#endregion
}
