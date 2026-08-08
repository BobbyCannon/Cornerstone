#region References

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.SuggestedActions.Actions.Base;
using Cornerstone.VisualStudio.SuggestedActions.Helpers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;

#endregion

namespace Cornerstone.VisualStudio.SuggestedActions.Actions;

internal class MissingNamespaceSuggestedAction : BaseSuggestedAction, ISuggestedAction
{
	#region Fields

	private readonly string _alias;
	private readonly Dictionary<string, string> _aliases;
	private readonly ITextBufferFactoryService _bufferFactory;
	private readonly IDifferenceBufferFactoryService _diffBufferFactory;
	private readonly IWpfDifferenceViewerFactoryService _diffFactory;
	private readonly ITextViewRoleSet _previewRoleSet;
	private readonly ITrackingSpan _span;
	private readonly KeyValuePair<string, string> _targetClassMetadata;

	#endregion

	#region Constructors

	public MissingNamespaceSuggestedAction(ITrackingSpan span, IWpfDifferenceViewerFactoryService diffFactory, IDifferenceBufferFactoryService diffBufferFactory,
		ITextBufferFactoryService bufferFactory, ITextEditorFactoryService textEditorFactoryService, IReadOnlyDictionary<string, string> inverseNamespaces,
		Dictionary<string, string> aliases, string alias)
	{
		_span = span;
		_targetClassMetadata = inverseNamespaces.FirstOrDefault(x => x.Key.Split('.').Last() == _span.GetText(_span.TextBuffer.CurrentSnapshot));
		DisplayText = $"Add xmlns {alias}";
		_diffFactory = diffFactory;
		_diffBufferFactory = diffBufferFactory;
		_bufferFactory = bufferFactory;
		_aliases = aliases;
		_alias = alias;
		_previewRoleSet = textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable);
	}

	#endregion

	#region Properties

	public string DisplayText { get; }

	#endregion

	#region Methods

	public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
	{
		return Task.FromResult<object>(PreviewProvider.GetPreview(_bufferFactory, _span, _diffBufferFactory, _diffFactory, _previewRoleSet, ApplySuggestion));
	}

	public void Invoke(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		ApplySuggestion(_span.TextBuffer);
	}

	private void ApplySuggestion(ITextBuffer buffer)
	{
		var lastNs = _aliases.Last().Value;

		buffer.Insert(buffer.CurrentSnapshot.GetText().IndexOf(lastNs) + lastNs.Length + 2, $"xmlns:{_alias}=\"{_targetClassMetadata.Value}\"");
	}

	#endregion
}