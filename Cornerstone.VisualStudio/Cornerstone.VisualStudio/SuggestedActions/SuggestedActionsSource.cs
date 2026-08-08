#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Models;
using Cornerstone.VisualStudio.SuggestedActions.Actions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

#endregion

namespace Cornerstone.VisualStudio.SuggestedActions;

internal class SuggestedActionsSource : ISuggestedActionsSource
{
	#region Fields

	private readonly ITextBufferFactoryService _bufferFactory;
	private readonly IDifferenceBufferFactoryService _diffBufferFactory;
	private readonly IWpfDifferenceViewerFactoryService _diffFactory;
	private readonly SuggestedActionsSourceProvider _factory;
	private readonly ITextBuffer _textBuffer;
	private readonly ITextEditorFactoryService _textEditorFactoryService;
	private readonly ITextView _textView;

	#endregion

	#region Constructors

	public SuggestedActionsSource(SuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer,
		IWpfDifferenceViewerFactoryService diffFactory, IDifferenceBufferFactoryService diffBufferFactory, ITextBufferFactoryService bufferFactory,
		ITextEditorFactoryService textEditorFactoryService)
	{
		_factory = testSuggestedActionsSourceProvider;
		_textBuffer = textBuffer;
		_diffFactory = diffFactory;
		_diffBufferFactory = diffBufferFactory;
		_bufferFactory = bufferFactory;
		_textEditorFactoryService = textEditorFactoryService;
		_textView = textView;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range,
		CancellationToken cancellationToken)
	{
		var availableSuggestedActions = SuggestedActionsAreAvailable(range);
		if (TryGetWordUnderCaret(out var extent) && (availableSuggestedActions.Item1 || availableSuggestedActions.Item2 || availableSuggestedActions.Item3))
		{
			extent.Span.Snapshot.TextBuffer.Properties.TryGetProperty<XamlBufferMetadata>(typeof(XamlBufferMetadata), out var metadata);
			var trackingSpan = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
			ISuggestedAction suggestedAction = null;
			if (availableSuggestedActions.Item1)
			{
				suggestedAction = new MissingNamespaceAndAliasSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
					metadata.CompletionMetadata.InverseNamespace, CompletionEngine.GetNamespaceAliases(extent.Span.Snapshot.TextBuffer.CurrentSnapshot.GetText()));
			}
			else if (availableSuggestedActions.Item2)
			{
				suggestedAction = new MissingAliasSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
					metadata.CompletionMetadata.InverseNamespace);
			}
			else if (availableSuggestedActions.Item3)
			{
				HasAlias(out var alias);
				suggestedAction = new MissingNamespaceSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
					metadata.CompletionMetadata.InverseNamespace, CompletionEngine.GetNamespaceAliases(extent.Span.Snapshot.TextBuffer.CurrentSnapshot.GetText()), alias);
			}
			return [new SuggestedActionSet(null, [suggestedAction])];
		}
		return [];
	}

	public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
	{
		var availableSuggestedActions = SuggestedActionsAreAvailable(range);
		if (availableSuggestedActions.Item1 || availableSuggestedActions.Item2 || availableSuggestedActions.Item3)
		{
			return Task.FromResult(true);
		}

		return Task.FromResult(false);
	}

	public bool TryGetTelemetryId(out Guid telemetryId)
	{
		telemetryId = Guid.Empty;
		return false;
	}

	protected virtual void OnSuggestedActionsChanged()
	{
		SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
	}

	private bool HasAlias(out string alias)
	{
		var span = _textView.Caret.ContainingTextViewLine.Extent.GetText().Trim();
		var xmlReader = XmlReader.Create(new StringReader(span));
		try
		{
			xmlReader.Read();
		}
		catch
		{
			if ((xmlReader.NodeType == XmlNodeType.Element) && !string.IsNullOrEmpty(xmlReader.Prefix))
			{
				alias = xmlReader.Prefix;
				return true;
			}
		}
		alias = null;
		return false;
	}

	/// <returns>
	/// This method returns 3 bool values. First one defines whether MissingNamespaceAndAliasSuggestedAction should be applied
	/// Second one defines whether MissingAliasSuggestedAction should be applied.
	/// Third one defines whether MissingNamespaceSuggestedAction should be applied.
	/// </returns>
	private (bool, bool, bool) SuggestedActionsAreAvailable(SnapshotSpan range)
	{
		if (TryGetWordUnderCaret(out var extent))
		{
			var span = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
			var snapshot = span.TextBuffer.CurrentSnapshot;
			var targetClassName = span.GetText(snapshot);
			span.TextBuffer.Properties.TryGetProperty<XamlBufferMetadata>(typeof(XamlBufferMetadata), out var metadata);
			if ((metadata == null) || (metadata.CompletionMetadata?.InverseNamespace == null))
			{
				return (false, false, false);
			}
			var targetClassMetadata = metadata.CompletionMetadata.InverseNamespace.FirstOrDefault(x => x.Key.Split('.').Last() == targetClassName);

			// Exclude all classes from avaloniaui namespace because controls from this namespace are included by default.
			if ((targetClassMetadata.Value != null) && (targetClassMetadata.Key != null) && !metadata.CompletionMetadata.Namespaces.First(x => x.Key == "https://github.com/avaloniaui").Value.ContainsKey(targetClassName))
			{
				if (!CompletionEngine.GetNamespaceAliases(span.TextBuffer.CurrentSnapshot.GetText()).ContainsValue(targetClassMetadata.Value))
				{
					if (!HasAlias(out _))
					{
						return (true, false, false);
					}
					return (false, false, true);
				}
				if (!HasAlias(out _))
				{
					return (false, true, false);
				}
			}
		}
		return (false, false, false);
	}

	private bool TryGetWordUnderCaret(out TextExtent wordExtent)
	{
		var caret = _textView.Caret;
		SnapshotPoint point;

		if (caret.Position.BufferPosition > 0)
		{
			point = caret.Position.BufferPosition - 1;
		}
		else
		{
			wordExtent = default;
			return false;
		}

		var navigator = _factory.NavigatorService.GetTextStructureNavigator(_textBuffer);

		wordExtent = navigator.GetExtentOfWord(point);
		return true;
	}

	#endregion

	#region Events

	public event EventHandler<EventArgs> SuggestedActionsChanged;

	#endregion
}