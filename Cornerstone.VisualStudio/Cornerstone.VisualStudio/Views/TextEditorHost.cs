#region References

using System;
using System.Runtime.CompilerServices;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

#endregion

namespace Cornerstone.VisualStudio.Views;

internal class TextEditorHost : IVsTextBufferDataEvents
{
	#region Fields

	private readonly IComponentModel _componentModel;
	private readonly IConnectionPoint _connectionPoint;
	private readonly uint _cookie;
	private readonly IOleServiceProvider _oleServiceProvider;
	private readonly Guid _xmlLanguageServiceGuid;

	#endregion

	#region Constructors

	public TextEditorHost(IVsTextLines textLines, string fileName, IComponentModel componentModel, IOleServiceProvider serviceProvider)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		_xmlLanguageServiceGuid = new("f6819a78-a205-47b5-be1c-675b3c7f0b8e");
		_componentModel = componentModel;
		_oleServiceProvider = serviceProvider;

		TextBuffer = textLines;
		FileName = fileName;
		OleServiceProvider = serviceProvider;

		// This allows us to subscribe to the "event" for when the text buffer is finally loaded
		// and ready to be used, COM event style
		var connectionPointContainer = textLines as IConnectionPointContainer;
		var bufferEventsGuid = typeof(IVsTextBufferDataEvents).GUID;
		connectionPointContainer?.FindConnectionPoint(ref bufferEventsGuid, out _connectionPoint);
		_connectionPoint?.Advise(this, out _cookie);
	}

	#endregion

	#region Properties

	public string FileName { get; }

	public IOleServiceProvider OleServiceProvider { get; private set; }

	public IVsTextLines TextBuffer { get; }

	public IVsCodeWindow VsCodeWindow { get; private set; }

	public IVsTextView WpfTextView { get; private set; }

	public IWpfTextViewHost WpfTextViewHost { get; private set; }

	#endregion

	#region Methods

	public int OnLoadCompleted(int fReload)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		// We no longer need to be notified, release this
		_connectionPoint.Unadvise(_cookie);

		// Set up the language service - this will activate intellisense and syntax highlighting
		TextBuffer.SetLanguageServiceID(ref Unsafe.AsRef(_xmlLanguageServiceGuid));

		// Now we can create the IVsCodeWindow
		// If we don't wait until the text buffer is fully initialized before creating the IVsCodeWindow
		// it will fail completely and VS will abort loading our designer
		CreateCodeWindow();

		return VSConstants.S_OK;
	}

	private void CreateCodeWindow()
	{
		var eafs = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
		var window = eafs.CreateVsCodeWindowAdapter(_oleServiceProvider);

		// IMPORTANT: do NOT pass CWB_DISABLESPLITTER.
		// AvaloniaVS historically disabled the splitter because reparenting only the
		// WpfTextViewHost.HostControl (without the code-window chrome) made split crashy.
		// We host the full IVsCodeWindow via VsCodeWindowHost so Window → Split / the
		// scrollbar split grip can create and show a secondary view.
		//
		// CWB_DEFAULT (0) keeps: splitter, dropdown bar (unless disabled separately).
		((IVsCodeWindowEx) window).Initialize(
			(uint) _codewindowbehaviorflags.CWB_DEFAULT,
			VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter,
			"",
			"",
			0,
			new INITVIEW[1]);

		ErrorHandler.ThrowOnFailure(window.SetBuffer(TextBuffer));

		var buffer = eafs.GetDocumentBuffer(TextBuffer);
		buffer?.Properties.GetOrCreateSingletonProperty(() => new XamlBufferMetadata());

		var primaryView = window.GetPrimaryView(out var ppView);
		var textViewHost = eafs.GetWpfTextViewHost(ppView);

		VsCodeWindow = window;
		WpfTextView = ppView;
		WpfTextViewHost = textViewHost;

		CodeWindowCreated?.Invoke(this, EventArgs.Empty);
	}

	void IVsTextBufferDataEvents.OnFileChanged(uint grfChange, uint dwFileAttrs)
	{
	}

	#endregion

	#region Events

	public event EventHandler CodeWindowCreated;

	#endregion
}