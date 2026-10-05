#region References

using System;
using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Attaches completion and paste command filters to a CXAML/AXAML text view.
/// Called from MEF view-created listeners and from TextEditorHost after SetBuffer
/// so VS 18 cannot skip the listener while the buffer is still Inert.
/// </summary>
[Export]
internal sealed class XamlIntelliSenseRegistrar
{
	#region Fields

	private readonly IVsEditorAdaptersFactoryService _adapterService;
	private readonly ICompletionBroker _completionBroker;
	private readonly CompletionEngineSource _completionEngineSource;
	private readonly IServiceProvider _serviceProvider;
	private readonly StyleClassNameIndex _styleClassNameIndex;
	private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public XamlIntelliSenseRegistrar(
		[Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
		IVsEditorAdaptersFactoryService adapterService,
		ICompletionBroker completionBroker,
		ITextUndoHistoryRegistry textUndoHistoryRegistry,
		CompletionEngineSource completionEngineSource,
		[Import(AllowDefault = true)] StyleClassNameIndex styleClassNameIndex)
	{
		_serviceProvider = serviceProvider;
		_adapterService = adapterService;
		_completionBroker = completionBroker;
		_textUndoHistoryRegistry = textUndoHistoryRegistry;
		_completionEngineSource = completionEngineSource;
		_styleClassNameIndex = styleClassNameIndex;
	}

	#endregion

	#region Methods

	public void Register(IVsTextView textViewAdapter, IWpfTextView textView)
	{
		if (textViewAdapter == null)
		{
			return;
		}

		if (textView == null)
		{
			textView = _adapterService.GetWpfTextView(textViewAdapter);
		}

		if (textView?.TextBuffer == null)
		{
			return;
		}

		XamlBufferMetadataHelper.Ensure(textView.TextBuffer);

		textView.Properties.GetOrCreateSingletonProperty(() =>
		{
			Log.Logger.Debug("Registered XAML completion command handler for {ContentType}",
				textView.TextBuffer.ContentType?.TypeName);
			return new XamlCompletionCommandHandler(
				_serviceProvider,
				_completionBroker,
				textView,
				textViewAdapter,
				_completionEngineSource.CompletionEngine,
				_textUndoHistoryRegistry,
				_styleClassNameIndex);
		});

		textView.Properties.GetOrCreateSingletonProperty(() => new XamlPasteCommandHandler(
			_serviceProvider,
			textView,
			textViewAdapter,
			_textUndoHistoryRegistry,
			_completionEngineSource.CompletionEngine));

		textView.Properties.GetOrCreateSingletonProperty(() => new XamlTextManipulatorRegistrar(textView));
	}

	#endregion
}
