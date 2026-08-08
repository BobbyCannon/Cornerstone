#region References

using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.ProjectSystem;

[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(CornerstoneConstants.AvaloniaCapability)]
internal class AvaloniaProject : IProjectDynamicLoadComponent
{
	#region Fields

	private IAsyncServiceProvider _asyncServiceProvider;

	#endregion

	#region Methods

	public async Task LoadAsync()
	{
		if (_asyncServiceProvider is null)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (ServiceProvider.GlobalProvider.GetService(typeof(IVsShell)) is IVsShell shell)
			{
				if (shell.IsPackageLoaded(CornerstoneConstants.PackageGuid, out var vsPackage)
					!= VSConstants.S_OK)
				{
					shell.LoadPackage(CornerstoneConstants.PackageGuid, out vsPackage);
				}
				_asyncServiceProvider = (IAsyncServiceProvider) vsPackage;
			}
		}
	}

	public async Task UnloadAsync()
	{
		// Unload the feature
		await Task.CompletedTask;
	}

	#endregion
}