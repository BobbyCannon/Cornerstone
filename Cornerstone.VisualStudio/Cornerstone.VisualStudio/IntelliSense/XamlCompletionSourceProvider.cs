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
	private readonly StyleClassNameIndex _styleClassNameIndex;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public XamlCompletionSourceProvider(
		[Import] CompletionEngineSource completionEngineSource,
		[Import(AllowDefault = true)] StyleClassNameIndex styleClassNameIndex)
	{
		_completionEngineSource = completionEngineSource;
		_styleClassNameIndex = styleClassNameIndex;
	}

	#endregion

	#region Methods

	public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
	{
		// Never return null: VS caches a null source and never asks again, even after
		// the designer stamps XamlBufferMetadata / ITextDocument on this buffer.
		if (XamlBufferMetadataHelper.IsCornerstoneXamlBuffer(textBuffer))
		{
			XamlBufferMetadataHelper.Ensure(textBuffer);
		}

		return new XamlCompletionSource(textBuffer, _completionEngineSource, _styleClassNameIndex);
	}

	#endregion
}