#region References

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

internal class MissingAliasSuggestedAction : BaseSuggestedAction, ISuggestedAction
{
	#region Fields

	private readonly ITextBufferFactoryService _bufferFactory;
	private readonly IDifferenceBufferFactoryService _diffBufferFactory;
	private readonly IWpfDifferenceViewerFactoryService _diffFactory;
	private readonly string _namespaceAlias;
	private readonly ITextViewRoleSet _previewRoleSet;
	private readonly ITextSnapshot _snapshot;
	private readonly ITrackingSpan _span;
	private readonly string _targetClassName;

	#endregion

	#region Constructors

	public MissingAliasSuggestedAction(ITrackingSpan span, IWpfDifferenceViewerFactoryService diffFactory, IDifferenceBufferFactoryService diffBufferFactory, ITextBufferFactoryService bufferFactory, ITextEditorFactoryService textEditorFactoryService, string xmlNamespace, string alias)
	{
		_span = span;
		_snapshot = _span.TextBuffer.CurrentSnapshot;
		_targetClassName = _span.GetText(_snapshot);
		_namespaceAlias = alias ?? string.Empty;
		_diffFactory = diffFactory;
		_diffBufferFactory = diffBufferFactory;
		_bufferFactory = bufferFactory;
		_previewRoleSet = textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable);
		DisplayText = "Use " + _namespaceAlias.ToLower() + " (" + (xmlNamespace ?? string.Empty) + ")";
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
		ApplySuggestion(_span.TextBuffer);
	}

	private void ApplySuggestion(ITextBuffer buffer)
	{
		buffer.Replace(_span.GetSpan(_snapshot), $"{_namespaceAlias.ToLower()}:{_targetClassName}");
	}

	#endregion
}