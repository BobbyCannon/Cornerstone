#region References

using System;
using Avalonia.Controls.ApplicationLifetimes;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Desktop <see cref="CornerstoneApplication" /> that hosts <see cref="DocumentationReader" />
/// using <see cref="DocumentationReaderHost.CurrentOptions" />.
/// </summary>
public abstract class DocumentationReaderApplication : CornerstoneApplication
{
	#region Methods

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var options = DocumentationReaderHost.CurrentOptions
				?? throw new InvalidOperationException(
					"DocumentationReaderHost.Run must be called before starting the application.");

			var catalog = DocumentationReaderHost.BuildCatalog(options);
			catalog = DocumentationReaderHost.ApplyOpenDocumentArgument(catalog, desktop.Args, options);
			desktop.MainWindow = new DocumentationReaderHostWindow(catalog, options.WindowTitle);
		}

		base.OnFrameworkInitializationCompleted();
	}

	#endregion
}