#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal class XamlCompletionSource : ICompletionSource
{
	#region Fields

	private readonly ITextBuffer _buffer;
	private readonly CompletionEngineSource _engine;

	#endregion

	#region Constructors

	public XamlCompletionSource(ITextBuffer textBuffer, CompletionEngineSource completionEngineSource)
	{
		_buffer = textBuffer;
		_engine = completionEngineSource;
	}

	#endregion

	#region Methods

	public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
	{
		if (_buffer.Properties.TryGetProperty<XamlBufferMetadata>(typeof(XamlBufferMetadata), out var metadata) &&
			(metadata.CompletionMetadata != null))
		{
			var sw = Stopwatch.StartNew();
			var pos = session.TextView.Caret.Position.BufferPosition;
			var text = pos.Snapshot.GetText();
			_buffer.Properties.TryGetProperty("AssemblyName", out string assemblyName);
			var completions = _engine.CompletionEngine.GetCompletions(metadata.CompletionMetadata, text, pos, assemblyName);

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

				var span = new SnapshotSpan(pos.Snapshot, start, caret - start);
				var applicableTo = pos.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

				var xamlCompletions = XamlCompletion.Create(completions.Completions).ToList();
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

			sw.Stop();
		}
	}

	public void Dispose()
	{
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