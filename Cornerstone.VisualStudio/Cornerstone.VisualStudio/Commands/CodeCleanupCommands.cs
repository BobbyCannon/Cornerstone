#region References

using System;
using System.ComponentModel.Design;
using Cornerstone.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Serilog;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Commands;

/// <summary>
/// Registers Code Cleanup menu commands (document + Solution Explorer hierarchy).
/// </summary>
internal sealed class CodeCleanupCommands
{
	#region Fields

	private readonly CodeCleanupService _cleanupService;
	private readonly AsyncPackage _package;

	#endregion

	#region Constructors

	private CodeCleanupCommands(AsyncPackage package, OleMenuCommandService commandService)
	{
		_package = package ?? throw new ArgumentNullException(nameof(package));
		_cleanupService = new CodeCleanupService(package);

		if (commandService == null)
		{
			throw new ArgumentNullException(nameof(commandService));
		}

		var documentCommandId = new CommandID(CornerstoneConstants.CommandSetGuid, CornerstoneConstants.CodeCleanupDocumentCommandId);
		var documentMenuItem = new OleMenuCommand(ExecuteCleanDocument, documentCommandId);
		commandService.AddCommand(documentMenuItem);

		var hierarchyCommandId = new CommandID(CornerstoneConstants.CommandSetGuid, CornerstoneConstants.CodeCleanupHierarchyCommandId);
		var hierarchyMenuItem = new OleMenuCommand(ExecuteCleanHierarchy, hierarchyCommandId);
		hierarchyMenuItem.BeforeQueryStatus += OnHierarchyBeforeQueryStatus;
		commandService.AddCommand(hierarchyMenuItem);
	}

	#endregion

	#region Methods

	public static async Task InitializeAsync(AsyncPackage package)
	{
		// TEMP: Code Cleanup UI disabled for release.
		if (!CornerstoneConstants.CodeCleanupUiEnabled)
		{
			return;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
		if (commandService == null)
		{
			// Do not throw — a missing menu service must not block the designer/previewer package init.
			Log.Warning("Code Cleanup commands not registered: IMenuCommandService unavailable");
			return;
		}

		_ = new CodeCleanupCommands(package, commandService);
	}

	private void ExecuteCleanDocument(object sender, EventArgs e)
	{
		_ = _package.JoinableTaskFactory.RunAsync(async () =>
		{
			try
			{
				await _cleanupService.CleanActiveDocumentAsync();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Code Cleanup Document command failed");
			}
		});
	}

	private void ExecuteCleanHierarchy(object sender, EventArgs e)
	{
		_ = _package.JoinableTaskFactory.RunAsync(async () =>
		{
			try
			{
				await _cleanupService.CleanHierarchySelectionAsync();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Code Cleanup hierarchy command failed");
			}
		});
	}

	private void OnHierarchyBeforeQueryStatus(object sender, EventArgs e)
	{
		if (sender is not OleMenuCommand command)
		{
			return;
		}

		// Visible by default when on SE context menus; enable if we can resolve work.
		// Keep QueryStatus cheap: always visible/enabled; empty selection no-ops with a status message.
		command.Visible = true;
		command.Enabled = true;
	}

	#endregion
}