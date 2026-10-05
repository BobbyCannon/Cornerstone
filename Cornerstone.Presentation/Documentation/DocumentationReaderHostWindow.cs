#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Documentation;

public class DocumentationReaderHostWindow : Window
{
	#region Fields

	private DocumentationCatalog _catalog;
	private DocumentationReader _reader;

	#endregion

	#region Constructors

	public DocumentationReaderHostWindow()
	{
		_catalog = null;
		_reader = new DocumentationReader();
		// Same as DockingWindow: Content is the hosted control. The theme presents Reader.
		Content = _reader;
	}

	public DocumentationReaderHostWindow(DocumentationCatalog catalog, DocumentationReaderHostOptions options) : this()
	{
		if (options is not null)
		{
			if (!string.IsNullOrWhiteSpace(options.WindowTitle))
			{
				Title = options.WindowTitle;
			}

			ApplyWindowIcon(options);
		}

		ApplySettings();
		_catalog = catalog;
		ApplyCatalog();
	}

	#endregion

	#region Properties

	public DocumentationReader Reader => _reader;

	#endregion

	#region Methods

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		CaptureSettings();
		base.OnClosing(e);
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);
		ApplyCatalog();
	}

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);
		ApplyCatalog();
	}

	private void ApplySettings()
	{
		var settings = DocumentationReaderHost.Settings;
		if (settings is null)
		{
			return;
		}

		this.RestoreWindowLocation(settings.WindowLocation);
		if (_reader is not null)
		{
			_reader.IsFullWidth = settings.ReadingWidth == DocumentationReadingWidth.Full;
		}
	}

	private void CaptureSettings()
	{
		var settings = DocumentationReaderHost.Settings;
		if (settings is null)
		{
			return;
		}

		settings.WindowLocation ??= new WindowLocation();
		this.CaptureWindowLocation(settings.WindowLocation);

		var readingWidth = (_reader?.IsFullWidth == true)
			? DocumentationReadingWidth.Full
			: DocumentationReadingWidth.Column;
		if (settings.ReadingWidth != readingWidth)
		{
			settings.ReadingWidth = readingWidth;
		}
	}

	private void ApplyCatalog()
	{
		if ((_catalog is null) || (_reader is null))
		{
			return;
		}

		_reader.Catalog = _catalog;
	}

	private void ApplyWindowIcon(DocumentationReaderHostOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.WindowIcon) || (options.ApplicationAssembly is null))
		{
			return;
		}

		var assemblyName = options.ApplicationAssembly.GetName().Name;
		if (string.IsNullOrWhiteSpace(assemblyName))
		{
			return;
		}

		var path = options.WindowIcon.Trim();
		if (!path.StartsWith('/'))
		{
			path = "/" + path;
		}

		var uri = new Uri($"csres://{assemblyName}{path}");
		if (!AssetLoader.Exists(uri))
		{
			return;
		}

		using var stream = AssetLoader.Open(uri);
		var bitmap = new Bitmap(stream);
		Icon = new WindowIcon(bitmap);
		TitleBarIcon = bitmap;
	}

	#endregion
}