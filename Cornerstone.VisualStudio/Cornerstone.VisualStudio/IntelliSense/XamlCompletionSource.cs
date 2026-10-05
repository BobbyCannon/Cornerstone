#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.VisualStudio.Models;
using Cornerstone.VisualStudio.Protocol;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal class XamlCompletionSource : ICompletionSource
{
	#region Fields

	internal const string PreparedCompletionsProperty = "Cornerstone.VisualStudio.PreparedCompletions";

	private readonly ITextBuffer _buffer;
	private readonly CompletionEngineSource _engine;
	private readonly StyleClassNameIndex _styleClassNameIndex;

	#endregion

	#region Constructors

	public XamlCompletionSource(
		ITextBuffer textBuffer,
		CompletionEngineSource completionEngineSource,
		StyleClassNameIndex styleClassNameIndex)
	{
		_buffer = textBuffer;
		_engine = completionEngineSource;
		_styleClassNameIndex = styleClassNameIndex;
	}

	#endregion

	#region Methods

	public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
	{
		Core.Completion.CompletionSet prepared = null;
		if (_buffer.Properties.TryGetProperty(PreparedCompletionsProperty, out Core.Completion.CompletionSet stored))
		{
			_buffer.Properties.RemoveProperty(PreparedCompletionsProperty);
			prepared = stored;
		}

		XamlBufferMetadata metadata = null;
		if ((prepared == null) &&
			(!_buffer.Properties.TryGetProperty<XamlBufferMetadata>(typeof(XamlBufferMetadata), out metadata) ||
				(metadata.CompletionMetadata == null)))
		{
			Log.Logger.Error("XAML autocomplete failed to start: no completion metadata on the buffer (designer metadata not built yet or failed)");
			return;
		}

		var sw = Stopwatch.StartNew();
		try
		{
			var pos = session.TextView.Caret.Position.BufferPosition;
			var text = pos.Snapshot.GetText();
			_buffer.Properties.TryGetProperty("AssemblyName", out string assemblyName);
			var extraClasses = _styleClassNameIndex?.GetNames();
			var completions = prepared ?? Complete(metadata, text, pos, assemblyName, extraClasses);

			if (completions?.Completions.Count > 0)
			{
				var caret = pos.Position;
				var start = completions.StartPosition;

				// TODO: this should be handled in the completion engine
				// pseudoclasses should only be returned in a Selector, so this is an easy filter
				// We need to offset the start though for pseudoclasses to remove what they're 
				// attached to: Control:pointerover -> :pointerover
				if (completions.Completions[0].DisplayText.StartsWith(":"))
				{
					for (var i = caret - 1; i >= 0; i--)
					{
						if (char.IsWhiteSpace(text[i]) || (text[i] == ':'))
						{
							start = i;
							break;
						}
					}
				}

				// Clamp: ApplicableTo must cover [start, caret). Wrong/empty spans leave typed
				// filter text (e.g. "TextB") in the buffer and insert the completion elsewhere —
				// which looks like spaces/indent + a stuck caret when Enter also leaks through.
				if (start < 0)
				{
					start = 0;
				}
				if (start > caret)
				{
					start = caret;
				}

				// Value completion with the caret on the closing quote must not replace that quote.
				if ((caret > start) && (text[caret - 1] == '"') &&
					(completions.Completions[0].InsertText != null) &&
					(completions.Completions[0].InsertText.IndexOf("=\"", StringComparison.Ordinal) < 0))
				{
					caret = caret - 1;
				}

				var span = new SnapshotSpan(pos.Snapshot, start, caret - start);
				var applicableTo = pos.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

				var xamlCompletions = XamlCompletion.Create(
					completions.Completions, session.TextView, applicableTo).ToList();
				completionSets.Insert(0, new CompletionSet(
					"Avalonia",
					"Avalonia",
					applicableTo,
					xamlCompletions,
					null));

				// Select best match for the text already in ApplicableTo (e.g. "TextB" → TextBlock),
				// not always Completions[0] (often a high-priority closing tag like /Grid>).
				var filterText = span.GetText();
				var best = FindBestCompletionMatch(xamlCompletions, filterText) ?? xamlCompletions[0];
				completionSets[0].SelectionStatus = new CompletionSelectionStatus(best, true, false);

				var completionHint =
					$"{xamlCompletions.Count} completions found (Selected:{best.DisplayText}, Filter:'{filterText}')";

				Log.Logger.Verbose("XAML completion took {Time}, {CompletionHint}", sw.Elapsed, completionHint);
			}
			else
			{
				Log.Logger.Error(
					"XAML autocomplete failed to start: engine returned no completions at caret {Caret} (assembly {AssemblyName})",
					pos.Position,
					assemblyName ?? "(none)");
			}
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "XAML autocomplete failed to start: exception while building the completion set");
		}
		finally
		{
			sw.Stop();
		}
	}

	public void Dispose()
	{
	}

	internal static Core.Completion.CompletionSet ToCompletionSet(GetCompletionsResponseMessage response)
	{
		if ((response == null) || (response.Items == null) || (response.Items.Count == 0))
		{
			return null;
		}

		var items = new List<Core.Completion.Completion>();
		foreach (var item in response.Items)
		{
			int? cursor = item.RecommendedCursorOffset >= 0 ? item.RecommendedCursorOffset : null;
			int? delete = item.DeleteTextOffset >= 0 ? item.DeleteTextOffset : null;
			items.Add(new Core.Completion.Completion(
				item.DisplayText,
				item.InsertText,
				item.Description,
				(Core.Completion.CompletionKind) item.Kind,
				cursor,
				item.Suffix,
				delete,
				item.Priority)
			{
				TriggerCompletionAfterInsert = item.TriggerCompletionAfterInsert
			});
		}

		return new Core.Completion.CompletionSet
		{
			Completions = items,
			StartPosition = response.StartPosition
		};
	}

	private Core.Completion.CompletionSet Complete(
		XamlBufferMetadata metadata,
		string text,
		int caret,
		string assemblyName,
		IReadOnlyList<string> extraClasses)
	{
		var paths = metadata.AssemblyPaths;
		if ((paths != null) && (paths.Count > 0))
		{
			// The command handler asks the editor host and stores the set before Start.
			// Augment must not wait on that process.
			Log.Logger.Error("XAML autocomplete failed to start: editor host result was not ready");
			return null;
		}

		return _engine.CompletionEngine.GetCompletions(
			metadata.CompletionMetadata, text, caret, assemblyName, extraClasses);
	}

	/// <summary>
	/// Picks the completion that best matches typed filter text (prefix, then contains).
	/// </summary>
	private static XamlCompletion FindBestCompletionMatch(
		IList<XamlCompletion> completions,
		string filterText)
	{
		if (completions == null || completions.Count == 0)
		{
			return null;
		}

		if (string.IsNullOrEmpty(filterText))
		{
			return completions[0];
		}

		// Prefer prefix match on display/insert text (case-insensitive).
		var prefix = completions.FirstOrDefault(c =>
			c.DisplayText.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) ||
			(c.InsertionText?.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) == true));
		if (prefix != null)
		{
			return prefix;
		}

		return completions.FirstOrDefault(c =>
			c.DisplayText.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
			(c.InsertionText?.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0));
	}

	#endregion
}