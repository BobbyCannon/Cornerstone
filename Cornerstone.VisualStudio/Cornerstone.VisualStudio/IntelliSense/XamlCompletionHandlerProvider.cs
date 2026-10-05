#region References

using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Registers a <see cref="XamlCompletionCommandHandler" /> with newly-created text views.
/// </summary>
[Export(typeof(IVsTextViewCreationListener))]
[Name("Avalonia XAML completion handler")]
[ContentType("xml")]
[Order(After = "default")]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal class XamlCompletionHandlerProvider : IVsTextViewCreationListener
{
	#region Fields

	private readonly IVsEditorAdaptersFactoryService _adapterService;
	private readonly XamlIntelliSenseRegistrar _registrar;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public XamlCompletionHandlerProvider(
		IVsEditorAdaptersFactoryService adapterService,
		XamlIntelliSenseRegistrar registrar)
	{
		_adapterService = adapterService;
		_registrar = registrar;
	}

	#endregion

	#region Methods

	public void VsTextViewCreated(IVsTextView textViewAdapter)
	{
		var textView = _adapterService.GetWpfTextView(textViewAdapter);
		if ((textView != null) && XamlBufferMetadataHelper.IsCornerstoneXamlBuffer(textView.TextBuffer))
		{
			_registrar.Register(textViewAdapter, textView);
		}
	}

	#endregion
}