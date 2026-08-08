#region References

using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

[Export(typeof(ICompletionSourceProvider))]
[ContentType("xml")]
[Name("Avalonia XAML Completion")]
internal class XamlCompletionSourceProvider : ICompletionSourceProvider
{
	#region Fields

	private readonly CompletionEngineSource _completionEngineSource;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public XamlCompletionSourceProvider([Import] CompletionEngineSource completionEngineSource)
	{
		_completionEngineSource = completionEngineSource;
	}

	#endregion

	#region Methods

	public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
	{
		if (textBuffer.Properties.ContainsProperty(typeof(XamlBufferMetadata)))
		{
			return new XamlCompletionSource(textBuffer, _completionEngineSource);
		}

		return null;
	}

	#endregion
}