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

internal class ConvertGridDefinitionsSuggestedAction : BaseSuggestedAction, ISuggestedAction
{
	#region Fields

	private readonly string _attributeName;
	private readonly string _attributeValue;
	private readonly ITextBufferFactoryService _bufferFactory;
	private readonly string _insertion;
	private readonly IDifferenceBufferFactoryService _diffBufferFactory;
	private readonly IWpfDifferenceViewerFactoryService _diffFactory;
	private readonly int _insertAt;
	private readonly ITextViewRoleSet _previewRoleSet;
	private readonly int _removeLength;
	private readonly int _removeStart;
	private readonly ITrackingSpan _span;

	#endregion

	#region Constructors

	public ConvertGridDefinitionsSuggestedAction(
		ITrackingSpan span,
		IWpfDifferenceViewerFactoryService diffFactory,
		IDifferenceBufferFactoryService diffBufferFactory,
		ITextBufferFactoryService bufferFactory,
		ITextEditorFactoryService textEditorFactoryService,
		string attributeName,
		string attributeValue,
		int removeStart,
		int removeLength,
		int insertAt,
		string insertion,
		string displayText)
	{
		_span = span;
		_diffFactory = diffFactory;
		_diffBufferFactory = diffBufferFactory;
		_bufferFactory = bufferFactory;
		_attributeName = attributeName ?? string.Empty;
		_attributeValue = attributeValue ?? string.Empty;
		_insertion = insertion ?? string.Empty;
		_removeStart = removeStart;
		_removeLength = removeLength;
		_insertAt = insertAt;
		_previewRoleSet = textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable);
		DisplayText = string.IsNullOrEmpty(displayText) ? "Convert to attribute" : displayText;
	}

	#endregion

	#region Properties

	public string DisplayText { get; }

	#endregion

	#region Methods

	public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
	{
		return Task.FromResult<object>(PreviewProvider.GetPreview(_bufferFactory, _span, _diffBufferFactory, _diffFactory, _previewRoleSet, Apply));
	}

	public void Invoke(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		Apply(_span.TextBuffer);
	}

	private void Apply(ITextBuffer buffer)
	{
		var snapshot = buffer.CurrentSnapshot;
		if ((_removeStart < 0) || (_insertAt < 0) || (_removeLength < 0) ||
			(_removeStart + _removeLength > snapshot.Length) || (_insertAt > snapshot.Length))
		{
			return;
		}

		var insertion = _insertion;
		if (insertion.Length == 0)
		{
			insertion = " " + _attributeName + "=\"" + _attributeValue + "\"";
		}

		var edit = buffer.CreateEdit();
		if (_insertAt == _removeStart)
		{
			edit.Replace(_removeStart, _removeLength, insertion);
		}
		else
		{
			edit.Delete(_removeStart, _removeLength);
			edit.Insert(_insertAt, insertion);
		}

		edit.Apply();
	}

	#endregion
}
