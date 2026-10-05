#region References

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Cornerstone.VisualStudio.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Shows every format. The buffer already holds the default guid, so the normal
/// prefix filter would hide the other formats.
/// </summary>
internal sealed class NewGuidFormatCompletionSet : CompletionSet
{
	public NewGuidFormatCompletionSet(ITrackingSpan applicableTo, IList<Completion> completions)
		: base("Cornerstone.NewGuid", "GUID", applicableTo, completions, null)
	{
	}

	public override void Filter()
	{
	}

	public override void SelectBestMatch()
	{
		if ((SelectionStatus != null) && (SelectionStatus.Completion != null))
		{
			return;
		}

		if (Completions.Count > 0)
		{
			SelectionStatus = new CompletionSelectionStatus(Completions[0], true, false);
		}
	}
}

internal sealed class NewGuidFormatCompletionSource : ICompletionSource
{
	public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
	{
		NewGuidFormat[] formats;
		if ((session == null) || !session.Properties.TryGetProperty(typeof(NewGuidFormats), out formats) ||
			(formats == null) || (formats.Length == 0))
		{
			return;
		}

		var completions = new List<Completion>(formats.Length);
		for (var i = 0; i < formats.Length; i++)
		{
			var format = formats[i];
			completions.Add(new Completion(format.Text, format.Text, format.Description, null, null));
		}

		var snapshot = session.TextView.TextSnapshot;
		var caret = session.TextView.Caret.Position.BufferPosition.Position;
		var length = formats[0].Text.Length;
		var start = caret - length;
		if ((start < 0) || (start + length > snapshot.Length))
		{
			return;
		}

		var applicableTo = snapshot.CreateTrackingSpan(start, length, SpanTrackingMode.EdgeInclusive);
		completionSets.Add(new NewGuidFormatCompletionSet(applicableTo, completions));
	}

	public void Dispose()
	{
	}
}

[Export(typeof(ICompletionSourceProvider))]
[Name("Cornerstone new guid formats")]
[ContentType("text")]
internal sealed class NewGuidFormatCompletionSourceProvider : ICompletionSourceProvider
{
	public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
	{
		return new NewGuidFormatCompletionSource();
	}
}

[Export(typeof(IVsTextViewCreationListener))]
[Name("Cornerstone new guid")]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Editable)]
internal sealed class NewGuidTabCommandProvider : IVsTextViewCreationListener
{
	private readonly IVsEditorAdaptersFactoryService _adapterService;
	private readonly ICompletionBroker _completionBroker;

	[ImportingConstructor]
	public NewGuidTabCommandProvider(
		IVsEditorAdaptersFactoryService adapterService,
		ICompletionBroker completionBroker)
	{
		_adapterService = adapterService;
		_completionBroker = completionBroker;
	}

	public void VsTextViewCreated(IVsTextView textViewAdapter)
	{
		var textView = _adapterService.GetWpfTextView(textViewAdapter);
		if ((textView == null) || textView.Properties.ContainsProperty(typeof(NewGuidTabCommand)))
		{
			return;
		}

		textView.Properties.AddProperty(
			typeof(NewGuidTabCommand),
			new NewGuidTabCommand(textView, textViewAdapter, _completionBroker));
	}
}

/// <summary>
/// Tab after "nguid" inserts a new guid and offers the other formats.
/// Works in any editable document.
/// </summary>
internal sealed class NewGuidTabCommand : IOleCommandTarget
{
	private readonly IVsTextView _adapter;
	private readonly ICompletionBroker _completionBroker;
	private readonly ITextView _textView;
	private IOleCommandTarget _next;
	private ICompletionSession _session;

	public NewGuidTabCommand(ITextView textView, IVsTextView textViewAdapter, ICompletionBroker completionBroker)
	{
		_textView = textView;
		_adapter = textViewAdapter;
		_completionBroker = completionBroker;
		textViewAdapter.AddCommandFilter(this, out _next);
		_textView.Closed += OnClosed;
	}

	public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
	{
		if (TryCommit(ref pguidCmdGroup, nCmdID) || TryInsert(ref pguidCmdGroup, nCmdID))
		{
			return VSConstants.S_OK;
		}

		return _next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
	}

	public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
	{
		return _next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
	}

	private void OnClosed(object sender, EventArgs e)
	{
		_textView.Closed -= OnClosed;
	}

	private bool TryInsert(ref Guid pguidCmdGroup, uint nCmdID)
	{
		if ((pguidCmdGroup != VSConstants.VSStd2K) || (nCmdID != (uint) VSConstants.VSStd2KCmdID.TAB))
		{
			return false;
		}

		if ((_textView.Caret == null) || !_textView.Selection.IsEmpty)
		{
			return false;
		}

		var caret = _textView.Caret.Position.BufferPosition;
		var snapshot = caret.Snapshot;
		var windowStart = caret.Position - NewGuidFormats.Token.Length - 1;
		if (windowStart < 0)
		{
			windowStart = 0;
		}

		var after = caret.Position < snapshot.Length ? 1 : 0;
		var window = snapshot.GetText(windowStart, caret.Position - windowStart + after);
		int tokenStart;
		int tokenLength;
		if (!NewGuidFormats.TryGetTokenSpan(window, caret.Position - windowStart, out tokenStart, out tokenLength))
		{
			return false;
		}

		tokenStart += windowStart;
		var formats = NewGuidFormats.Create(Guid.NewGuid());
		var inserted = formats[0].Text;
		var edit = _textView.TextBuffer.CreateEdit();
		edit.Replace(tokenStart, tokenLength, inserted);
		var applied = edit.Apply();
		var point = applied.CreateTrackingPoint(tokenStart, PointTrackingMode.Negative);

		_completionBroker.DismissAllSessions(_textView);
		var session = _completionBroker.CreateCompletionSession(_textView, point, true);
		session.Properties.AddProperty(typeof(NewGuidFormats), formats);
		session.Dismissed += SessionDismissed;
		_session = session;
		session.Start();
		SelectFormat(session);
		// The language service would otherwise take Enter and insert a newline
		// without committing this session.
		Promote();
		return true;
	}

	private bool TryCommit(ref Guid pguidCmdGroup, uint nCmdID)
	{
		var session = _session;
		if ((session == null) || session.IsDismissed || (pguidCmdGroup != VSConstants.VSStd2K))
		{
			return false;
		}

		var command = (VSConstants.VSStd2KCmdID) nCmdID;
		if (command == VSConstants.VSStd2KCmdID.CANCEL)
		{
			session.Dismiss();
			return true;
		}

		if ((command != VSConstants.VSStd2KCmdID.RETURN) && (command != VSConstants.VSStd2KCmdID.TAB))
		{
			return false;
		}

		var set = SelectFormat(session);
		if (set == null)
		{
			session.Dismiss();
			return true;
		}

		var selected = set.SelectionStatus.Completion;
		if ((selected == null) && (set.Completions.Count > 0))
		{
			selected = set.Completions[0];
		}

		if (selected != null)
		{
			set.SelectionStatus = new CompletionSelectionStatus(selected, true, false);
		}

		session.Commit();
		if (!session.IsDismissed)
		{
			WriteSelection(set, selected);
			session.Dismiss();
		}

		return true;
	}

	private static NewGuidFormatCompletionSet SelectFormat(ICompletionSession session)
	{
		NewGuidFormatCompletionSet found = null;
		for (var i = 0; i < session.CompletionSets.Count; i++)
		{
			var set = session.CompletionSets[i] as NewGuidFormatCompletionSet;
			if (set == null)
			{
				continue;
			}

			found = set;
			session.SelectedCompletionSet = set;
			if (((set.SelectionStatus == null) || (set.SelectionStatus.Completion == null)) && (set.Completions.Count > 0))
			{
				set.SelectionStatus = new CompletionSelectionStatus(set.Completions[0], true, false);
			}

			break;
		}

		return found;
	}

	private void WriteSelection(NewGuidFormatCompletionSet set, Completion selected)
	{
		if ((set == null) || (selected == null) || (set.ApplicableTo == null))
		{
			return;
		}

		var span = set.ApplicableTo.GetSpan(_textView.TextSnapshot);
		var insert = selected.InsertionText ?? string.Empty;
		using (var edit = _textView.TextBuffer.CreateEdit())
		{
			edit.Replace(span, insert);
			edit.Apply();
		}
	}

	private void SessionDismissed(object sender, EventArgs e)
	{
		var session = sender as ICompletionSession;
		if (session != null)
		{
			session.Dismissed -= SessionDismissed;
		}

		if (ReferenceEquals(_session, session))
		{
			_session = null;
		}
	}

	private void Promote()
	{
		try
		{
			_adapter.RemoveCommandFilter(this);
		}
		catch (Exception)
		{
			// The filter is already out of the chain.
		}

		_adapter.AddCommandFilter(this, out _next);
	}
}
