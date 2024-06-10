#region References

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Folding;

/// <summary>
/// A <see cref="VisualLineElementGenerator" /> that produces line elements for folded <see cref="FoldingSection" />s.
/// </summary>
public sealed class FoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
{
	#region Fields

	private FoldingManager _foldingManager;
	private readonly List<TextView> _textViews = [];

	#endregion

	#region Properties

	/// <summary>
	/// Default brush for folding element text. Value: Brushes.Gray
	/// </summary>
	public static IBrush DefaultTextBrush { get; } = Brushes.Gray;

	/// <summary>
	/// Gets/Sets the folding manager from which the foldings should be shown.
	/// </summary>
	public FoldingManager FoldingManager
	{
		get => _foldingManager;
		set
		{
			if (_foldingManager != value)
			{
				if (_foldingManager != null)
				{
					foreach (var v in _textViews)
					{
						_foldingManager.RemoveFromTextView(v);
					}
				}
				_foldingManager = value;
				if (_foldingManager != null)
				{
					foreach (var v in _textViews)
					{
						_foldingManager.AddToTextView(v);
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets/sets the brush used for folding element text.
	/// </summary>
	public static IBrush TextBrush { get; set; } = DefaultTextBrush;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override VisualLineElement ConstructElement(int offset)
	{
		if (_foldingManager == null)
		{
			return null;
		}
		var foldedUntil = -1;
		FoldingSection foldingSection = null;
		foreach (var fs in _foldingManager.GetFoldingsContaining(offset))
		{
			if (fs.IsFolded)
			{
				if (fs.EndOffset > foldedUntil)
				{
					foldedUntil = fs.EndOffset;
					foldingSection = fs;
				}
			}
		}
		if ((foldedUntil > offset) && (foldingSection != null))
		{
			// Handle overlapping foldings: if there's another folded folding
			// (starting within the foldingSection) that continues after the end of the folded section,
			// then we'll extend our fold element to cover that overlapping folding.
			bool foundOverlappingFolding;
			do
			{
				foundOverlappingFolding = false;
				foreach (var fs in FoldingManager.GetFoldingsContaining(foldedUntil))
				{
					if (fs.IsFolded && (fs.EndOffset > foldedUntil))
					{
						foldedUntil = fs.EndOffset;
						foundOverlappingFolding = true;
					}
				}
			} while (foundOverlappingFolding);

			var title = foldingSection.Title;
			if (string.IsNullOrEmpty(title))
			{
				title = "...";
			}
			var p = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
			p.SetForegroundBrush(TextBrush);
			var textFormatter = TextFormatterFactory.Create(CurrentContext.TextView);
			var text = FormattedTextElement.PrepareText(textFormatter, title, p);
			return new FoldingLineElement(foldingSection, text, foldedUntil - offset, TextBrush);
		}
		return null;
	}

	/// <inheritdoc />
	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (_foldingManager != null)
		{
			foreach (var fs in _foldingManager.GetFoldingsContaining(startOffset))
			{
				// Test whether we're currently within a folded folding (that didn't just end).
				// If so, create the fold marker immediately.
				// This is necessary if the actual beginning of the fold marker got skipped due to another VisualElementGenerator.
				if (fs.IsFolded && (fs.EndOffset > startOffset))
				{
					//return startOffset;
				}
			}
			return _foldingManager.GetNextFoldedFoldingStart(startOffset);
		}
		return -1;
	}

	/// <inheritdoc />
	public override void StartGeneration(ITextRunConstructionContext context)
	{
		base.StartGeneration(context);
		if (_foldingManager != null)
		{
			if (!_foldingManager.TextViews.Contains(context.TextView))
			{
				throw new ArgumentException("Invalid TextView");
			}
			if (context.Document != _foldingManager.Document)
			{
				throw new ArgumentException("Invalid document");
			}
		}
	}

	void ITextViewConnect.AddToTextView(TextView textView)
	{
		_textViews.Add(textView);
		_foldingManager?.AddToTextView(textView);
	}

	void ITextViewConnect.RemoveFromTextView(TextView textView)
	{
		_textViews.Remove(textView);
		_foldingManager?.RemoveFromTextView(textView);
	}

	#endregion

	#region Classes

	private sealed class FoldingLineElement : FormattedTextElement
	{
		#region Fields

		private readonly FoldingSection _fs;
		private readonly IBrush _textBrush;

		#endregion

		#region Constructors

		public FoldingLineElement(FoldingSection fs, TextLine text, int documentLength, IBrush textBrush) : base(text, documentLength)
		{
			_fs = fs;
			_textBrush = textBrush;
		}

		#endregion

		#region Methods

		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new FoldingLineTextRun(this, TextRunProperties, _textBrush);
		}

		//DOUBLETAP
		protected internal override void OnPointerPressed(PointerPressedEventArgs e)
		{
			_fs.IsFolded = false;
			e.Handled = true;
		}

		#endregion
	}

	private sealed class FoldingLineTextRun : FormattedTextRun
	{
		#region Fields

		private readonly IBrush _textBrush;

		#endregion

		#region Constructors

		public FoldingLineTextRun(FormattedTextElement element, TextRunProperties properties, IBrush textBrush)
			: base(element, properties)
		{
			_textBrush = textBrush;
		}

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			var (width, height) = Size;
			var r = new Rect(origin.X, origin.Y, width, height);
			drawingContext.DrawRectangle(new ImmutablePen(_textBrush.ToImmutable()), r);
			base.Draw(drawingContext, origin);
		}

		#endregion
	}

	#endregion
}