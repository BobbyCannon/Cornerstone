#region References

using System;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Documentation;

public class App : CornerstoneApplication
{
	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		base.Initialize();
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var contentRoot = AppContext.BaseDirectory;
			var catalog = DocumentationCatalog.FromDirectory(contentRoot, "Readme.md");

			// Optional CLI: open a specific relative path
			var args = desktop.Args ?? [];
			var entry = args.FirstOrDefault(a => a.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
			if (!string.IsNullOrEmpty(entry))
			{
				var normalized = DocumentationDocument.NormalizeId(entry);
				if (catalog.TryGet(normalized, out _))
				{
					catalog = new DocumentationCatalog(catalog.Documents, normalized);
				}
			}

			desktop.MainWindow = new MainWindow(catalog);
		}

		base.OnFrameworkInitializationCompleted();
	}

	#endregion
}
