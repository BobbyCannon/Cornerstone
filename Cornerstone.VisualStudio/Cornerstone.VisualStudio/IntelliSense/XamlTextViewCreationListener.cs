#region References

using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Registers a <see cref="XamlCompletionCommandHandler" /> with newly-created text views.
/// </summary>
[Name("Avalonia XAML manupulator")]
[ContentType("xml")]
[Export(typeof(IWpfTextViewCreationListener))]
[TextViewRole(PredefinedTextViewRoles.Editable)]
[TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
internal sealed class XamlTextViewCreationListener : IWpfTextViewCreationListener
{
	#region Methods

	public void TextViewCreated(IWpfTextView textView)
	{
		if ((textView != null) && XamlBufferMetadataHelper.IsCornerstoneXamlBuffer(textView.TextBuffer))
		{
			textView.Properties.GetOrCreateSingletonProperty(() => new XamlTextManipulatorRegistrar(textView));
		}
	}

	#endregion
}