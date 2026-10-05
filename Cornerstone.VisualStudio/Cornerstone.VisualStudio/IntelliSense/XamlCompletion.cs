#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Completion = Cornerstone.VisualStudio.Core.Completion.Completion;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// XAML completion item. Implements ICustomCommit so Visual Studio calls
/// Commit instead of replacing ApplicableTo and parking the caret
/// at the end of InsertionText.
/// </summary>
internal class XamlCompletion : Completion4, ICustomCommit
{
	#region Fields

	private static ImageMoniker[] _images;
	private readonly ITrackingSpan _applicableTo;
	private readonly ITextView _textView;

	#endregion

	#region Constructors

	public XamlCompletion(Completion completion)
		: this(completion, null, null)
	{
	}

	public XamlCompletion(Completion completion, ITextView textView, ITrackingSpan applicableTo)
		: base(
			completion.DisplayText,
			completion.InsertText,
			completion.Description,
			GetImage(completion.Kind),
			completion.Kind.ToString(),
			suffix: string.IsNullOrWhiteSpace(completion.Suffix) ? string.Empty : $"({completion.Suffix})")
	{
		_textView = textView;
		_applicableTo = applicableTo;

		if (completion.RecommendedCursorOffset is int idx &&
			(idx >= 0) &&
			(idx <= (completion.InsertText?.Length ?? 0)))
		{
			CaretIndexInInsert = idx;
			CursorOffset = completion.InsertText.Length - idx;
		}
		else if (completion.RecommendedCursorOffset.HasValue)
		{
			CursorOffset = Math.Max(0, completion.InsertText.Length - completion.RecommendedCursorOffset.Value);
			CaretIndexInInsert = completion.InsertText.Length - CursorOffset;
		}

		TriggerCompletion = completion.TriggerCompletionAfterInsert;
		Kind = completion.Kind;
		DeleteTextOffset = completion.DeleteTextOffset;
		if (completion.Priority < 255)
		{
			AttributeIcons = new CompletionIcon2[]
			{
				new(KnownMonikers.OverlayProtected, "", "")
			};
		}
	}

	#endregion

	#region Properties

	public int? CaretIndexInInsert { get; }

	public int CursorOffset { get; }

	public int? DeleteTextOffset { get; }

	public override string InsertionText
	{
		get
		{
			if (HasFlag(Kind, CompletionKind.Name) && !string.IsNullOrEmpty(Suffix))
			{
				return $"{Suffix.Substring(1, Suffix.Length - 2)}#{base.InsertionText}";
			}
			return base.InsertionText;
		}

		set => base.InsertionText = value;
	}

	public CompletionKind Kind { get; }

	public bool TriggerCompletion { get; }

	#endregion

	#region Methods

	public void Commit()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if ((_textView == null) || (_applicableTo == null))
		{
			return;
		}

		var insert = InsertionText ?? string.Empty;
		var snapshot = _textView.TextSnapshot;
		SnapshotSpan span;
		try
		{
			span = _applicableTo.GetSpan(snapshot);
		}
		catch
		{
			return;
		}

		if ((span.Start.Position < 0) || (span.End.Position > snapshot.Length))
		{
			return;
		}

		var startTracker = snapshot.CreateTrackingPoint(span.Start.Position, PointTrackingMode.Negative);
		var options = _textView.Options;
		var previousIndent = options.GetOptionValue(DefaultOptions.IndentStyleId);
		ITextSnapshot newSnapshot;
		try
		{
			options.SetOptionValue(DefaultOptions.IndentStyleId, IndentingStyle.None);
			using (XamlTextManipulatorRegistrar.Suppress())
			using (var edit = _textView.TextBuffer.CreateEdit())
			{
				edit.Replace(span, insert);
				newSnapshot = edit.Apply();
			}
		}
		finally
		{
			options.SetOptionValue(DefaultOptions.IndentStyleId, previousIndent);
		}

		if (newSnapshot == null)
		{
			return;
		}

		newSnapshot = _textView.TextSnapshot;
		var insertStart = startTracker.GetPosition(newSnapshot);
		var caretPos = CompletionCaretPlacement.GetCaretAfterReplace(
			insertStart, insert, CaretIndexInInsert);
		caretPos = Math.Max(0, Math.Min(caretPos, newSnapshot.Length));
		_textView.Caret.MoveTo(new SnapshotPoint(newSnapshot, caretPos));

		if (DeleteTextOffset is int deleteOffset && (deleteOffset != 0))
		{
			var caret = _textView.Caret.Position.BufferPosition;
			var other = caret.Add(deleteOffset);
			var deleteSpan = other < caret ? new SnapshotSpan(other, -deleteOffset) : new SnapshotSpan(caret, deleteOffset);
			_textView.TextBuffer.Delete(deleteSpan);
		}
	}

	public static IEnumerable<XamlCompletion> Create(IEnumerable<Completion> source)
	{
		return Create(source, null, null);
	}

	public static IEnumerable<XamlCompletion> Create(
		IEnumerable<Completion> source,
		ITextView textView,
		ITrackingSpan applicableTo)
	{
		return source.Select(x => new XamlCompletion(x, textView, applicableTo));
	}

	private static ImageMoniker GetImage(CompletionKind kind)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (_images == null)
		{
			LoadImages();
		}
		if (HasFlag(kind, CompletionKind.DataProperty))
		{
			return _images[(int) CompletionKind.DataProperty];
		}
		if (HasFlag(kind, CompletionKind.TargetTypeClass))
		{
			return _images[(int) CompletionKind.TargetTypeClass];
		}
		if (HasFlag(kind, CompletionKind.VsXmlns))
		{
			return _images[(int) CompletionKind.Enum];
		}
		if (HasFlag(kind, CompletionKind.Selector))
		{
			return _images[(int) CompletionKind.Enum];
		}
		if (HasFlag(kind, CompletionKind.Name))
		{
			return _images[(int) CompletionKind.Class];
		}
		if (HasFlag(kind, CompletionKind.Comment))
		{
			return _images[(int) CompletionKind.Comment];
		}
		return _images[(int) kind];
	}

	private static bool HasFlag(CompletionKind test, CompletionKind expected)
	{
		return (test & expected) == expected;
	}

	private static void LoadImages()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var capacity = Enum.GetValues(typeof(CompletionKind)).Cast<int>().Max() + 1;

		_images = new ImageMoniker[capacity];
		_images[(int) CompletionKind.Property] = KnownMonikers.Property;
		_images[(int) CompletionKind.Event] = KnownMonikers.Event;
		_images[(int) CompletionKind.Class] = KnownMonikers.METATag;
		_images[(int) CompletionKind.Enum] = KnownMonikers.EnumerationItemPublic;
		_images[(int) CompletionKind.Namespace] = KnownMonikers.Namespace;

		_images[(int) CompletionKind.AttachedEvent] = KnownMonikers.Event;
		_images[(int) CompletionKind.AttachedProperty] = KnownMonikers.Property;
		_images[(int) CompletionKind.StaticProperty] = KnownMonikers.EnumerationItemPublic;
		_images[(int) CompletionKind.MarkupExtension] = KnownMonikers.Namespace;
		_images[(int) CompletionKind.DataProperty] = KnownMonikers.DatabaseProperty;
		_images[(int) CompletionKind.TargetTypeClass] = KnownMonikers.ClassPublic;
		_images[(int) CompletionKind.Selector] = KnownMonikers.Namespace;
		_images[(int) CompletionKind.Comment] = KnownMonikers.XMLCommentTag;
	}

	#endregion
}
