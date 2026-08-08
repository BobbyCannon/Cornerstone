#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Completion = Cornerstone.VisualStudio.Core.Completion.Completion;

#endregion


namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// An Avalonia XAML intellisense completion suggestion.
/// </summary>
internal class XamlCompletion : Completion4
{
	#region Fields

	private static ImageMoniker[] _images;

	#endregion

	#region Constructors

	public XamlCompletion(Completion completion)
		: base(
			completion.DisplayText,
			completion.InsertText,
			completion.Description,
			GetImage(completion.Kind),
			completion.Kind.ToString(),
			suffix: string.IsNullOrWhiteSpace(completion.Suffix) ? string.Empty : $"({completion.Suffix})")
	{
		// Index within InsertionText where the caret should land after commit
		// (e.g. after "TextBlock" in "TextBlock />", or after "Grid>" in "Grid></Grid>").
		if (completion.RecommendedCursorOffset is int idx &&
			(idx >= 0) &&
			(idx <= (completion.InsertText?.Length ?? 0)))
		{
			CaretIndexInInsert = idx;
			CursorOffset = completion.InsertText.Length - idx;
		}
		else if (completion.RecommendedCursorOffset.HasValue)
		{
			// Legacy: treat as "chars from end" only if out of range as an index.
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

	/// <summary>
	/// Caret position measured from the start of <see cref="InsertionText"/> after commit.
	/// Null means leave caret at the end of the insert.
	/// </summary>
	public int? CaretIndexInInsert { get; }

	/// <summary>
	/// Chars to move left from the end of the insert (derived from <see cref="CaretIndexInInsert"/>).
	/// </summary>
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

	public static IEnumerable<XamlCompletion> Create(
		IEnumerable<Completion> source)
	{
		return source.Select(x => new XamlCompletion(x));
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