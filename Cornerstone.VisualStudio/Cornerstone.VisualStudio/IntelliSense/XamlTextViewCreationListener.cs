#region References

using System;
using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Shell;
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
	#region Fields

	private readonly IServiceProvider _serviceProvider;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public XamlTextViewCreationListener(
		[Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	#endregion

	#region Methods

	public void TextViewCreated(IWpfTextView textView)
	{
		// If the buffer contains Avalonia XAML, register a completion handler on it.
		if (textView.TextBuffer.Properties.ContainsProperty(typeof(XamlBufferMetadata)))
		{
			new XamlTextManipulatorRegistrar(textView);
		}
	}

	#endregion
}