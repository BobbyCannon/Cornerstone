#region References

using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Cancels the remainder of a solution build when the first project fails.
/// DTE BuildEvents must be kept alive (COM sinks are not rooted by the DTE).
/// </summary>
internal sealed class StopBuildOnFirstFailureService
{
	#region Fields

	private readonly DTE _dte;
	private readonly ICornerstoneSettings _settings;
	private BuildEvents _buildEvents;
	private bool _stopping;

	#endregion

	#region Constructors

	public StopBuildOnFirstFailureService(DTE dte, ICornerstoneSettings settings)
	{
		_dte = dte;
		_settings = settings;
	}

	#endregion

	#region Methods

	public void Start()
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if ((_dte == null) || (_settings == null) || (_buildEvents != null))
		{
			return;
		}

		_buildEvents = _dte.Events.BuildEvents;
		_buildEvents.OnBuildBegin += HandleBuildBegin;
		_buildEvents.OnBuildProjConfigDone += HandleBuildProjConfigDone;
		Log.Debug("Stop-on-first-build-failure listener started (enabled={Enabled})", _settings.StopBuildOnFirstFailure);
	}

	private void HandleBuildBegin(vsBuildScope scope, vsBuildAction action)
	{
		_stopping = false;
	}

	private void HandleBuildProjConfigDone(
		string project,
		string projectConfig,
		string platform,
		string solutionConfig,
		bool success)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (success || _stopping || !_settings.StopBuildOnFirstFailure)
		{
			return;
		}

		_stopping = true;
		Log.Information(
			"Stopping solution build after {Project} failed ({Config}|{Platform})",
			project,
			projectConfig,
			platform);

		try
		{
			var manager = Package.GetGlobalService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
			if (manager != null)
			{
				manager.CancelUpdateSolutionConfiguration();
				return;
			}

			_dte.ExecuteCommand("Build.Cancel");
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to cancel solution build after project failure");
			_stopping = false;
		}
	}

	#endregion
}
