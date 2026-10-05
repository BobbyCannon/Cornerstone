#region References

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Models;
using Cornerstone.VisualStudio.Protocol;
using Cornerstone.VisualStudio.Services;
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
	private readonly object _suggestionGate;
	private readonly ITextBuffer _textBuffer;
	private readonly ITextEditorFactoryService _textEditorFactoryService;
	private readonly ITextView _textView;
	private string _suggestionAlias;
	private string _suggestionType;
	private int _suggestionVersion;
	private LookupNamespaceResponseMessage _suggestion;
	private ConvertGridDefinitionsResponseMessage _grid;
	private int _gridCaret;
	private int _gridVersion;

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
		_suggestionGate = new object();
		_suggestionAlias = string.Empty;
		_suggestionType = string.Empty;
		_suggestionVersion = -1;
		_suggestion = null;
		_grid = null;
		_gridCaret = -1;
		_gridVersion = -1;
	}

	#endregion

	#region Methods

	public void Dispose()
	{
	}

	public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range,
		CancellationToken cancellationToken)
	{
		try
		{
			var actions = new List<ISuggestedAction>();
			if (TryGetWordUnderCaret(out var extent))
			{
				var suggestion = CurrentSuggestion(extent.Span.GetText(), extent.Span.Snapshot.Version.VersionNumber);
				if ((suggestion != null) && string.IsNullOrEmpty(suggestion.Error) && (suggestion.Kind != LookupNamespaceResponseMessage.KindNone))
				{
					var trackingSpan = range.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
					var aliases = CompletionEngine.GetNamespaceAliases(extent.Span.Snapshot.GetText());
					if (suggestion.Kind == LookupNamespaceResponseMessage.KindAddNamespaceAndAlias)
					{
						actions.Add(new MissingNamespaceAndAliasSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
							suggestion.XmlNamespace, suggestion.Alias, aliases));
					}
					else if (suggestion.Kind == LookupNamespaceResponseMessage.KindUseAlias)
					{
						actions.Add(new MissingAliasSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
							suggestion.XmlNamespace, suggestion.Alias));
					}
					else if (suggestion.Kind == LookupNamespaceResponseMessage.KindAddNamespace)
					{
						actions.Add(new MissingNamespaceSuggestedAction(trackingSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
							suggestion.XmlNamespace, aliases, suggestion.Alias));
					}
				}
			}

			var grid = CurrentGrid();
			if ((grid != null) && (grid.CanConvert != 0) && string.IsNullOrEmpty(grid.Error))
			{
				var gridSpan = range.Snapshot.CreateTrackingSpan(new Span(grid.RemoveStart, grid.RemoveLength), SpanTrackingMode.EdgeInclusive);
				actions.Add(new ConvertGridDefinitionsSuggestedAction(gridSpan, _diffFactory, _diffBufferFactory, _bufferFactory, _textEditorFactoryService,
					grid.AttributeName, grid.AttributeValue, grid.RemoveStart, grid.RemoveLength, grid.InsertAt, grid.Insertion, grid.DisplayText));
			}

			if (actions.Count == 0)
			{
				return [];
			}

			return [new SuggestedActionSet(null, actions)];
		}
		catch
		{
			// Lightbulb must not throw; VS queries this on caret/F12.
		}

		return [];
	}

	public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
	{
		try
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			var hasNamespace = false;
			if (TryGetWordUnderCaret(out var extent))
			{
				var suggestion = await LookupAsync(extent, cancellationToken).ConfigureAwait(true);
				hasNamespace = (suggestion != null) &&
					string.IsNullOrEmpty(suggestion.Error) &&
					(suggestion.Kind != LookupNamespaceResponseMessage.KindNone);
			}

			var grid = await LookupGridAsync(cancellationToken).ConfigureAwait(true);
			var hasGrid = (grid != null) && string.IsNullOrEmpty(grid.Error) && (grid.CanConvert != 0);
			return hasNamespace || hasGrid;
		}
		catch
		{
			// Lightbulb must not throw; VS queries this on caret/F12 and a debugger break blocks Go To Definition.
		}

		return false;
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
		alias = null;
		var span = _textView.Caret.ContainingTextViewLine.Extent.GetText();
		var start = span.IndexOf('<');
		if (start < 0)
		{
			return false;
		}

		var i = start + 1;
		if ((i < span.Length) && (span[i] == '/'))
		{
			i++;
		}

		var nameStart = i;
		while ((i < span.Length) && IsXmlNameChar(span[i]))
		{
			i++;
		}

		if ((i >= span.Length) || (span[i] != ':') || (i == nameStart))
		{
			return false;
		}

		alias = span.Substring(nameStart, i - nameStart);
		return alias.Length > 0;
	}

	private static bool IsXmlNameChar(char c)
	{
		return char.IsLetterOrDigit(c) || (c == '_') || (c == '.');
	}

	/// <returns>
	/// This method returns 3 bool values. First one defines whether MissingNamespaceAndAliasSuggestedAction should be applied
	/// Second one defines whether MissingAliasSuggestedAction should be applied.
	/// Third one defines whether MissingNamespaceSuggestedAction should be applied.
	/// </returns>
	private LookupNamespaceResponseMessage CurrentSuggestion(string typeName, int version)
	{
		lock (_suggestionGate)
		{
			if ((_suggestion == null) || (_suggestionVersion != version) || (_suggestionType != typeName))
			{
				return null;
			}

			return _suggestion;
		}
	}

	private async Task<LookupNamespaceResponseMessage> LookupAsync(TextExtent extent, CancellationToken cancellationToken)
	{
		var snapshot = extent.Span.Snapshot;
		var typeName = extent.Span.GetText();
		var version = snapshot.Version.VersionNumber;
		string alias;
		var hasAlias = HasAlias(out alias);
		var cached = CurrentSuggestion(typeName, version);
		if ((cached != null) && (_suggestionAlias == (alias ?? string.Empty)))
		{
			return cached;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return null;
		}

		snapshot.TextBuffer.Properties.TryGetProperty(typeof(XamlBufferMetadata), out XamlBufferMetadata metadata);
		var paths = metadata?.AssemblyPaths;
		if ((paths == null) || (paths.Count == 0))
		{
			return null;
		}

		var pathList = new List<string>(paths.Count);
		for (var i = 0; i < paths.Count; i++)
		{
			pathList.Add(paths[i]);
		}

		var response = await EditorHostSession.LookupNamespaceAsync(
			snapshot.GetText(),
			typeName,
			hasAlias,
			alias,
			pathList).ConfigureAwait(true);
		lock (_suggestionGate)
		{
			_suggestion = response;
			_suggestionType = typeName;
			_suggestionVersion = version;
			_suggestionAlias = alias ?? string.Empty;
		}

		return response;
	}

	private ConvertGridDefinitionsResponseMessage CurrentGrid()
	{
		var snapshot = _textBuffer.CurrentSnapshot;
		var caret = _textView.Caret.Position.BufferPosition.Position;
		lock (_suggestionGate)
		{
			if ((_grid == null) || (_gridVersion != snapshot.Version.VersionNumber) || (_gridCaret != caret))
			{
				return null;
			}

			return _grid;
		}
	}

	private async Task<ConvertGridDefinitionsResponseMessage> LookupGridAsync(CancellationToken cancellationToken)
	{
		var snapshot = _textBuffer.CurrentSnapshot;
		var caret = _textView.Caret.Position.BufferPosition.Position;
		var cached = CurrentGrid();
		if (cached != null)
		{
			return cached;
		}

		if (cancellationToken.IsCancellationRequested)
		{
			return null;
		}

		var response = await EditorHostSession.ConvertGridDefinitionsAsync(snapshot.GetText(), caret).ConfigureAwait(true);
		lock (_suggestionGate)
		{
			_grid = response;
			_gridVersion = snapshot.Version.VersionNumber;
			_gridCaret = caret;
		}

		return response;
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