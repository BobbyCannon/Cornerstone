#region References

using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Text element that is supposed to be replaced by the user.
/// Will register an <see cref="IReplaceableActiveElement" />.
/// </summary>
public class SnippetReplaceableTextElement : SnippetTextElement
{
	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		var start = context.InsertionPosition;
		base.Insert(context);
		var end = context.InsertionPosition;
		context.RegisterActiveElement(this, new ReplaceableActiveElement(context, start, end));
	}

	#endregion

	///// <inheritdoc/>
	//public override Inline ToTextRun()
	//{
	//	return new Italic(base.ToTextRun());
	//}
}

/// <summary>
/// Interface for active element registered by <see cref="SnippetReplaceableTextElement" />.
/// </summary>
public interface IReplaceableActiveElement : IActiveElement
{
	#region Properties

	/// <summary>
	/// Gets the current text inside the element.
	/// </summary>
	string Text { get; }

	#endregion

	#region Events

	/// <summary>
	/// Occurs when the text inside the element changes.
	/// </summary>
	event EventHandler TextChanged;

	#endregion
}

internal sealed class ReplaceableActiveElement : IReplaceableActiveElement
{
	#region Fields

	private Renderer _background, _foreground;
	private readonly InsertionContext _context;
	private TextAnchor _end;
	private readonly int _endOffset;

	private bool _isCaretInside;
	private TextAnchor _start;
	private readonly int _startOffset;

	#endregion

	#region Constructors

	public ReplaceableActiveElement(InsertionContext context, int startOffset, int endOffset)
	{
		_context = context;
		_startOffset = startOffset;
		_endOffset = endOffset;
	}

	#endregion

	#region Properties

	public bool IsEditable => true;

	public ISegment Segment
	{
		get
		{
			if (_start.IsDeleted || _end.IsDeleted)
			{
				return null;
			}
			return new SimpleSegment(_start.Offset, Math.Max(0, _end.Offset - _start.Offset));
		}
	}

	public string Text { get; private set; }

	#endregion

	#region Methods

	public void Deactivate(SnippetEventArgs e)
	{
		TextDocumentWeakEventManager.TextChanged.RemoveHandler(_context.Document, OnDocumentTextChanged);
		_context.TextArea.TextView.BackgroundRenderers.Remove(_background);
		_context.TextArea.TextView.BackgroundRenderers.Remove(_foreground);
		_context.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
	}

	public void OnInsertionCompleted()
	{
		// anchors must be created in OnInsertionCompleted because they should move only
		// due to user insertions, not due to insertions of other snippet parts
		_start = _context.Document.CreateAnchor(_startOffset);
		_start.MovementType = AnchorMovementType.BeforeInsertion;
		_end = _context.Document.CreateAnchor(_endOffset);
		_end.MovementType = AnchorMovementType.AfterInsertion;
		_start.Deleted += AnchorDeleted;
		_end.Deleted += AnchorDeleted;

		// Be careful with references from the document to the editing/snippet layer - use weak events
		// to prevent memory leaks when the text area control gets dropped from the UI while the snippet is active.
		// The InsertionContext will keep us alive as long as the snippet is in interactive mode.
		TextDocumentWeakEventManager.TextChanged.AddHandler(_context.Document, OnDocumentTextChanged);

		_background = new Renderer { Layer = KnownLayer.Background, Element = this };
		_foreground = new Renderer { Layer = KnownLayer.Text, Element = this };
		_context.TextArea.TextView.BackgroundRenderers.Add(_background);
		_context.TextArea.TextView.BackgroundRenderers.Add(_foreground);
		_context.TextArea.Caret.PositionChanged += Caret_PositionChanged;
		Caret_PositionChanged(null, null);

		Text = GetText();
	}

	private void AnchorDeleted(object sender, EventArgs e)
	{
		_context.Deactivate(new SnippetEventArgs(DeactivateReason.Deleted));
	}

	private void Caret_PositionChanged(object sender, EventArgs e)
	{
		var s = Segment;
		if (s != null)
		{
			var newIsCaretInside = s.Contains(_context.TextArea.Caret.Offset, 0);
			if (newIsCaretInside != _isCaretInside)
			{
				_isCaretInside = newIsCaretInside;
				_context.TextArea.TextView.InvalidateLayer(_foreground.Layer);
			}
		}
	}

	private string GetText()
	{
		if (_start.IsDeleted || _end.IsDeleted)
		{
			return string.Empty;
		}
		return _context.Document.GetText(_start.Offset, Math.Max(0, _end.Offset - _start.Offset));
	}

	private void OnDocumentTextChanged(object sender, EventArgs e)
	{
		var newText = GetText();
		if (Text != newText)
		{
			Text = newText;
			TextChanged?.Invoke(this, e);
		}
	}

	#endregion

	#region Events

	public event EventHandler TextChanged;

	#endregion

	#region Classes

	private sealed class Renderer : IBackgroundRenderer
	{
		#region Fields

		internal ReplaceableActiveElement Element;
		private static readonly IBrush BackgroundBrush = CreateBackgroundBrush();
		private static readonly Pen ActiveBorderPen = CreateBorderPen();

		#endregion

		#region Properties

		public KnownLayer Layer { get; set; }

		#endregion

		#region Methods

		public void Draw(TextView textView, DrawingContext drawingContext)
		{
			var s = Element.Segment;
			if (s != null)
			{
				var geoBuilder = new BackgroundGeometryBuilder
				{
					AlignToWholePixels = true,
					BorderThickness = ActiveBorderPen?.Thickness ?? 0
				};
				if (Layer == KnownLayer.Background)
				{
					geoBuilder.AddSegment(textView, s);
					var geometry = geoBuilder.CreateGeometry();
					if (geometry != null)
					{
						drawingContext.DrawGeometry(BackgroundBrush, null, geometry);
					}
				}
				else
				{
					// draw foreground only if active
					if (Element._isCaretInside)
					{
						geoBuilder.AddSegment(textView, s);
						foreach (var boundElement in Element._context.ActiveElements.OfType<BoundActiveElement>())
						{
							if (boundElement.TargetElement == Element)
							{
								geoBuilder.AddSegment(textView, boundElement.Segment);
								geoBuilder.CloseFigure();
							}
						}
						var geometry = geoBuilder.CreateGeometry();
						if (geometry != null)
						{
							drawingContext.DrawGeometry(null, ActiveBorderPen, geometry);
						}
					}
				}
			}
		}

		private static IBrush CreateBackgroundBrush()
		{
			var b = new ImmutableSolidColorBrush(Colors.LimeGreen, 0.4);
			return b;
		}

		private static Pen CreateBorderPen()
		{
			var p = new Pen(Brushes.Black, dashStyle: DashStyle.Dot);
			return p;
		}

		#endregion
	}

	#endregion
}