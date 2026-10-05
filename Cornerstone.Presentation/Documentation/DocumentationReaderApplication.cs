#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Desktop <see cref="Application" /> that hosts <see cref="DocumentationReader" />
/// using <see cref="DocumentationReaderHost.CurrentOptions" />.
/// </summary>
public abstract class DocumentationReaderApplication : Application
{
	#region Methods

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var options = DocumentationReaderHost.CurrentOptions
				?? throw new InvalidOperationException(
					"DocumentationReaderHost.Run must be called before starting the application.");

			DocumentationReaderHost.LoadSettings();
			var catalog = DocumentationReaderHost.BuildCatalog(options);
			catalog = DocumentationReaderHost.ApplyOpenDocumentArgument(catalog, desktop.Args, options);
			desktop.MainWindow = new DocumentationReaderHostWindow(catalog, options);
			desktop.Exit += OnDesktopExit;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static void OnDesktopExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		DocumentationReaderHost.SaveSettings();
	}

	#endregion
}