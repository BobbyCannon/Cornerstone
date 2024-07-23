#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Avalonia.AvaloniaEdit.Highlighting;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Avalonia.AvaloniaEdit.Search;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Internal;
using Cornerstone.Presentation;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit;

/// <summary>
/// The text editor control.
/// Contains a scrollable TextArea.
/// </summary>
public class TextEditor : CornerstoneTemplatedControl, ITextEditorComponent
{
	#region Fields

	/// <summary>
	/// Document property.
	/// </summary>
	public static readonly StyledProperty<TextDocument> DocumentProperty
		= TextView.DocumentProperty.AddOwner<TextEditor>();

	/// <summary>
	/// Encoding dependency property.
	/// </summary>
	public static readonly StyledProperty<Encoding> EncodingProperty =
		AvaloniaProperty.Register<TextEditor, Encoding>(nameof(Encoding));

	/// <summary>
	/// Dependency property for <see cref="HorizontalScrollBarVisibility" />
	/// </summary>
	public static readonly AttachedProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
		ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner<TextEditor>();

	/// <summary>
	/// Dependency property for <see cref="IsDataLoaded" />
	/// </summary>
	public static readonly StyledProperty<bool> IsDataLoadedProperty =
		AvaloniaProperty.Register<TextEditor, bool>(nameof(IsDataLoaded));

	/// <summary>
	/// Dependency property for <see cref="IsModified" />
	/// </summary>
	public static readonly StyledProperty<bool> IsModifiedProperty =
		AvaloniaProperty.Register<TextEditor, bool>(nameof(IsModified));

	/// <summary>
	/// IsReadOnly dependency property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty =
		AvaloniaProperty.Register<TextEditor, bool>(nameof(IsReadOnly));

	/// <summary>
	/// LineNumbersBackground dependency property.
	/// </summary>
	public static readonly StyledProperty<IBrush> LineNumbersBackgroundProperty =
		AvaloniaProperty.Register<TextEditor, IBrush>(nameof(LineNumbersBackground), Brushes.Yellow);

	/// <summary>
	/// LineNumbersForeground dependency property.
	/// </summary>
	public static readonly StyledProperty<IBrush> LineNumbersForegroundProperty =
		AvaloniaProperty.Register<TextEditor, IBrush>(nameof(LineNumbersForeground), ColorExtensions.ConsoleColorMap[ConsoleColor.Gray].ToBrush());

	/// <summary>
	/// LineNumbersMargin dependency property.
	/// </summary>
	public static readonly StyledProperty<Thickness> LineNumbersMarginProperty =
		AvaloniaProperty.Register<TextEditor, Thickness>(nameof(LineNumbersMargin), new Thickness(0, 0, 8, 0));

	/// <summary>
	/// Options property.
	/// </summary>
	public static readonly StyledProperty<TextEditorOptions> OptionsProperty =
		TextView.OptionsProperty.AddOwner<TextEditor>();

	/// <summary>
	/// the pointerHover event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PointerHoverEvent =
		TextView.PointerHoverEvent;

	/// <summary>
	/// the pointerHoverStopped event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PointerHoverStoppedEvent =
		TextView.PointerHoverStoppedEvent;

	/// <summary>
	/// The PreviewPointerHover event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PreviewPointerHoverEvent =
		TextView.PreviewPointerHoverEvent;

	/// <summary>
	/// The PreviewPointerHoverStopped event.
	/// </summary>
	public static readonly RoutedEvent<PointerEventArgs> PreviewPointerHoverStoppedEvent =
		TextView.PreviewPointerHoverStoppedEvent;

	/// <summary>
	/// Dependency property for <see cref="SearchResultsBrush" />.
	/// </summary>
	public static readonly StyledProperty<IBrush> SearchResultsBrushProperty =
		AvaloniaProperty.Register<TextEditor, IBrush>(nameof(SearchResultsBrush), new SolidColorBrush(Color.FromRgb(81, 92, 106)));

	/// <summary>
	/// ShowLineNumbers dependency property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowLineNumbersProperty =
		AvaloniaProperty.Register<TextEditor, bool>(nameof(ShowLineNumbers));

	/// <summary>
	/// The <see cref="SyntaxHighlighting" /> property.
	/// </summary>
	public static readonly StyledProperty<IHighlightingDefinition> SyntaxHighlightingProperty =
		AvaloniaProperty.Register<TextEditor, IHighlightingDefinition>(nameof(SyntaxHighlighting));

	/// <summary>
	/// Dependency property for <see cref="VerticalScrollBarVisibility" />
	/// </summary>
	public static readonly AttachedProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
		ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner<TextEditor>();

	/// <summary>
	/// Word wrap dependency property.
	/// </summary>
	public static readonly StyledProperty<bool> WordWrapProperty =
		AvaloniaProperty.Register<TextEditor, bool>(nameof(WordWrap));

	private IVisualLineTransformer _colorizer;
	private ScrollBarVisibility _horizontalScrollBarVisibilityBck;
	private bool _wasSearchPanelOpened;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new TextEditor instance.
	/// </summary>
	public TextEditor() : this(new TextArea())
	{
	}

	/// <summary>
	/// Creates a new TextEditor instance.
	/// </summary>
	protected TextEditor(TextArea textArea) : this(textArea, new TextDocument())
	{
	}

	protected TextEditor(TextArea textArea, TextDocument document)
	{
		_horizontalScrollBarVisibilityBck = ScrollBarVisibility.Auto;

		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

		textArea.TextView.Services.AddService(this);

		SetValue(OptionsProperty, textArea.Options);
		SetValue(DocumentProperty, document);
		SetValue(IsModifiedProperty, (object) false);

		textArea[!BackgroundProperty] = this[!BackgroundProperty];

		AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
	}

	static TextEditor()
	{
		FocusableProperty.OverrideDefaultValue<TextEditor>(true);
		HorizontalScrollBarVisibilityProperty.OverrideDefaultValue<TextEditor>(ScrollBarVisibility.Auto);
		VerticalScrollBarVisibilityProperty.OverrideDefaultValue<TextEditor>(ScrollBarVisibility.Auto);

		OptionsProperty.Changed.Subscribe(OnOptionsChanged);
		DocumentProperty.Changed.Subscribe(OnDocumentChanged);
		SyntaxHighlightingProperty.Changed.Subscribe(OnSyntaxHighlightingChanged);
		IsReadOnlyProperty.Changed.Subscribe(OnIsReadOnlyChanged);
		IsModifiedProperty.Changed.Subscribe(OnIsModifiedChanged);
		ShowLineNumbersProperty.Changed.Subscribe(OnShowLineNumbersChanged);
		LineNumbersBackgroundProperty.Changed.Subscribe(OnLineNumbersBackgroundChanged);
		LineNumbersForegroundProperty.Changed.Subscribe(OnLineNumbersForegroundChanged);
		FontFamilyProperty.Changed.Subscribe(OnFontFamilyPropertyChanged);
		FontSizeProperty.Changed.Subscribe(OnFontSizePropertyChanged);
		SearchResultsBrushProperty.Changed.Subscribe(SearchResultsBrushChangedCallback);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets if text in editor can be copied
	/// </summary>
	public bool CanCopy => ApplicationCommands.Copy.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if text in editor can be cut
	/// </summary>
	public bool CanCut => ApplicationCommands.Cut.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if selected text in editor can be deleted
	/// </summary>
	public bool CanDelete => ApplicationCommands.Delete.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if text in editor can be pasted
	/// </summary>
	public bool CanPaste => ApplicationCommands.Paste.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if the most recent undone command can be redone.
	/// </summary>
	public bool CanRedo => ApplicationCommands.Redo.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if text editor can activate the search panel
	/// </summary>
	public bool CanSearch => SearchPanel != null;

	/// <summary>
	/// Gets if text the editor can select all
	/// </summary>
	public bool CanSelectAll => ApplicationCommands.SelectAll.CanExecute(null, TextArea);

	/// <summary>
	/// Gets if the most recent command can be undone.
	/// </summary>
	public bool CanUndo => ApplicationCommands.Undo.CanExecute(null, TextArea);

	/// <summary>
	/// Gets/sets the caret position.
	/// </summary>
	public int CaretOffset
	{
		get => TextArea.Caret.Offset;
		set => TextArea.Caret.Offset = value;
	}

	public ICompletionProvider CompletionProvider { get; set; }

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// This is a dependency property.
	/// </summary>
	public TextDocument Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <summary>
	/// Gets/sets the encoding used when the file is saved.
	/// </summary>
	/// <remarks>
	/// The <see cref="Load(Stream)" /> method auto-detects the encoding of the file and sets this property accordingly.
	/// The <see cref="Save(Stream)" /> method uses the encoding specified in this property.
	/// </remarks>
	public Encoding Encoding
	{
		get => GetValue(EncodingProperty);
		set => SetValue(EncodingProperty, value);
	}

	/// <summary>
	/// Gets the vertical size of the document.
	/// </summary>
	public double ExtentHeight => ScrollViewer?.Extent.Height ?? 0;

	/// <summary>
	/// Gets the horizontal size of the current document region.
	/// </summary>
	public double ExtentWidth => ScrollViewer?.Extent.Width ?? 0;

	/// <summary>
	/// Gets the horizontal scroll position.
	/// </summary>
	public double HorizontalOffset => ScrollViewer?.Offset.X ?? 0;

	/// <summary>
	/// Gets/Sets the horizontal scroll bar visibility.
	/// </summary>
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => GetValue(HorizontalScrollBarVisibilityProperty);
		set => SetValue(HorizontalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Gets/Sets the 'data loaded' flag.
	/// </summary>
	public bool IsDataLoaded
	{
		get => GetValue(IsDataLoadedProperty);
		set => SetValue(IsDataLoadedProperty, value);
	}

	/// <summary>
	/// Gets/Sets the 'modified' flag.
	/// </summary>
	public bool IsModified
	{
		get => GetValue(IsModifiedProperty);
		set => SetValue(IsModifiedProperty, value);
	}

	/// <summary>
	/// Specifies whether the user can change the text editor content.
	/// Setting this property will replace the
	/// <see cref="TextArea.ReadOnlySectionProvider"> TextArea.ReadOnlySectionProvider </see>.
	/// </summary>
	public bool IsReadOnly
	{
		get => GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// Gets the number of lines in the document.
	/// </summary>
	public int LineCount
	{
		get
		{
			var document = Document;
			return document?.LineCount ?? 1;
		}
	}

	/// <summary>
	/// Gets/sets the Brush used for displaying the background color of line numbers.
	/// </summary>
	public IBrush LineNumbersBackground
	{
		get => GetValue(LineNumbersBackgroundProperty);
		set => SetValue(LineNumbersBackgroundProperty, value);
	}

	/// <summary>
	/// Gets/sets the Brush used for displaying the foreground color of line numbers.
	/// </summary>
	public IBrush LineNumbersForeground
	{
		get => GetValue(LineNumbersForegroundProperty);
		set => SetValue(LineNumbersForegroundProperty, value);
	}

	/// <summary>
	/// Gets or sets the margin that is used to display the vertical line to the right of the line numbers.
	/// </summary>
	public Thickness LineNumbersMargin
	{
		get => GetValue(LineNumbersMarginProperty);
		set => SetValue(LineNumbersMarginProperty, value);
	}

	/// <summary>
	/// Gets/Sets the options currently used by the text editor.
	/// </summary>
	public TextEditorOptions Options
	{
		get => GetValue(OptionsProperty);
		set => SetValue(OptionsProperty, value);
	}

	/// <summary>
	/// Gets the search panel.
	/// </summary>
	public SearchPanel SearchPanel { get; private set; }

	/// <summary>
	/// Gets/sets the Brush used for marking search results in the TextView.
	/// </summary>
	public IBrush SearchResultsBrush
	{
		get => GetValue(SearchResultsBrushProperty);
		set => SetValue(SearchResultsBrushProperty, value);
	}

	/// <summary>
	/// Gets/Sets the selected text.
	/// </summary>
	public string SelectedText
	{
		get
		{
			// We'll get the text from the whole surrounding segment.
			// This is done to ensure that SelectedText.Length == SelectionLength.
			if ((TextArea.Document != null) && !TextArea.Selection.IsEmpty)
			{
				return TextArea.Document.GetText(TextArea.Selection.SurroundingSegment);
			}
			return string.Empty;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			var textArea = TextArea;
			if (textArea.Document != null)
			{
				var offset = SelectionStart;
				var length = SelectionLength;
				textArea.Document.Replace(offset, length, value);
				// keep inserted text selected
				textArea.Selection = Selection.Create(textArea, offset, offset + value.Length);
			}
		}
	}

	/// <summary>
	/// Gets the end position of the selection.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionEnd => SelectionStart + SelectionLength;

	/// <summary>
	/// Gets/sets the length of the selection.
	/// </summary>
	public int SelectionLength
	{
		get
		{
			if (!TextArea.Selection.IsEmpty)
			{
				return TextArea.Selection.SurroundingSegment.Length;
			}
			return 0;
		}
		set => Select(SelectionStart, value);
	}

	/// <summary>
	/// Gets/sets the start position of the selection.
	/// </summary>
	public int SelectionStart
	{
		get
		{
			if (TextArea.Selection.IsEmpty)
			{
				return TextArea.Caret.Offset;
			}
			return TextArea.Selection.SurroundingSegment.Offset;
		}
		set => Select(value, SelectionLength);
	}

	/// <summary>
	/// Specifies whether line numbers are shown on the left to the text view.
	/// </summary>
	public bool ShowLineNumbers
	{
		get => GetValue(ShowLineNumbersProperty);
		set => SetValue(ShowLineNumbersProperty, value);
	}

	/// <summary>
	/// Gets/sets the syntax highlighting definition used to colorize the text.
	/// </summary>
	public IHighlightingDefinition SyntaxHighlighting
	{
		get => GetValue(SyntaxHighlightingProperty);
		set => SetValue(SyntaxHighlightingProperty, value);
	}

	/// <summary>
	/// Gets/Sets the text of the current document.
	/// </summary>
	public string Text
	{
		get
		{
			var document = Document;
			return document != null ? document.Text : string.Empty;
		}
		set
		{
			var document = GetDocument();
			document.Text = value ?? string.Empty;
			// after replacing the full text, the caret is positioned at the end of the document
			// - reset it to the beginning.
			CaretOffset = 0;
			document.UndoStack.ClearAll();
		}
	}

	/// <summary>
	/// Gets the text area.
	/// </summary>
	public TextArea TextArea { get; }

	/// <summary>
	/// Gets the vertical scroll position.
	/// </summary>
	public double VerticalOffset => ScrollViewer?.Offset.Y ?? 0;

	/// <summary>
	/// Gets/Sets the vertical scroll bar visibility.
	/// </summary>
	public ScrollBarVisibility VerticalScrollBarVisibility
	{
		get => GetValue(VerticalScrollBarVisibilityProperty);
		set => SetValue(VerticalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Gets the horizontal size of the viewport.
	/// </summary>
	public double ViewportHeight => ScrollViewer?.Viewport.Height ?? 0;

	/// <summary>
	/// Gets the horizontal size of the viewport.
	/// </summary>
	public double ViewportWidth => ScrollViewer?.Viewport.Width ?? 0;

	/// <summary>
	/// Specifies whether the text editor uses word wrapping.
	/// </summary>
	/// <remarks>
	/// Setting WordWrap=true has the same effect as setting HorizontalScrollBarVisibility=Disabled and will override the
	/// HorizontalScrollBarVisibility setting.
	/// </remarks>
	public bool WordWrap
	{
		get => GetValue(WordWrapProperty);
		set => SetValue(WordWrapProperty, value);
	}

	protected CompletionWindow CompletionWindow { get; set; }

	/// <summary>
	/// Gets the scroll viewer used by the text editor.
	/// This property can return null if the template has not been applied / does not contain a scroll viewer.
	/// </summary>
	internal ScrollViewer ScrollViewer { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Appends text to the end of the document.
	/// </summary>
	public void AppendText(string textData)
	{
		var document = GetDocument();
		document.Insert(document.TextLength, textData);
	}

	/// <summary>
	/// Begins a group of document changes.
	/// </summary>
	public void BeginChange()
	{
		GetDocument().BeginUpdate();
	}

	/// <summary>
	/// Clears the text.
	/// </summary>
	public virtual void Clear()
	{
		Text = string.Empty;
	}

	/// <summary>
	/// Copies the current selection to the clipboard.
	/// </summary>
	public void Copy()
	{
		if (CanCopy)
		{
			ApplicationCommands.Copy.Execute(null, TextArea);
		}
	}

	/// <summary>
	/// Removes the current selection and copies it to the clipboard.
	/// </summary>
	public void Cut()
	{
		if (CanCut)
		{
			ApplicationCommands.Cut.Execute(null, TextArea);
		}
	}

	/// <summary>
	/// Begins a group of document changes and returns an object that ends the group of document
	/// changes when it is disposed.
	/// </summary>
	public IDisposable DeclareChangeBlock()
	{
		return GetDocument().RunUpdate();
	}

	/// <summary>
	/// Removes the current selection without copying it to the clipboard.
	/// </summary>
	public void Delete()
	{
		if (CanDelete)
		{
			ApplicationCommands.Delete.Execute(null, TextArea);
		}
	}

	/// <summary>
	/// Ends the current group of document changes.
	/// </summary>
	public void EndChange()
	{
		GetDocument().EndUpdate();
	}

	/// <summary>
	/// Gets the text view position from a point inside the editor.
	/// </summary>
	/// <param name="point">
	/// The position, relative to top left
	/// corner of TextEditor control
	/// </param>
	/// <returns> The text view position, or null if the point is outside the document. </returns>
	public TextViewPosition? GetPositionFromPoint(Point point)
	{
		if (Document == null)
		{
			return null;
		}
		var textView = TextArea.TextView;
		var tpoint = (Point) this.TranslatePoint(point + new Point(textView.ScrollOffset.X, Math.Floor(textView.ScrollOffset.Y)), textView);
		return textView.GetPosition(tpoint);
	}

	/// <summary>
	/// Loads the text from the stream, auto-detecting the encoding.
	/// </summary>
	/// <remarks>
	/// This method sets <see cref="IsModified" /> to false.
	/// </remarks>
	public void Load(Stream stream)
	{
		using var reader = FileReader.OpenStream(stream, Encoding ?? Encoding.UTF8);
		LoadData(reader.ReadToEnd(), reader.CurrentEncoding, null);
	}

	/// <summary>
	/// Loads the text from the stream, auto-detecting the encoding.
	/// </summary>
	public void LoadData(string data, Encoding encoding, string extension)
	{
		Document.Replace(0, Document.TextLength, data);
		SetValue(EncodingProperty, encoding);
		SetValue(IsDataLoadedProperty, true);
		SetValue(IsModifiedProperty, false);
		SetSyntaxHighlighterByExtension(extension);
	}

	/// <summary>
	/// Loads the text from the stream, auto-detecting the encoding.
	/// </summary>
	public void LoadFilePath(string filePath)
	{
		if ((filePath == null) || !File.Exists(filePath))
		{
			return;
		}

		using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		Load(fs);
		SetSyntaxHighlighter(filePath);
	}

	/// <summary>
	/// Pastes the clipboard content.
	/// </summary>
	public void Paste()
	{
		if (CanPaste)
		{
			ApplicationCommands.Paste.Execute(null, TextArea);
		}
	}

	/// <summary>
	/// Redoes the most recent undone command.
	/// </summary>
	/// <returns> True is the redo operation was successful, false is the redo stack is empty. </returns>
	public bool Redo()
	{
		if (CanRedo)
		{
			ApplicationCommands.Redo.Execute(null, TextArea);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Saves the text to the stream.
	/// </summary>
	/// <remarks>
	/// This method sets <see cref="IsModified" /> to false.
	/// </remarks>
	public void Save(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException(nameof(stream));
		}

		var encoding = Encoding;
		var document = Document;
		var writer = encoding != null ? new StreamWriter(stream, encoding) : new StreamWriter(stream);
		document?.WriteTextTo(writer);
		writer.Flush();

		// do not close the stream
		IsModified = false;
	}

	/// <summary>
	/// Saves the text to the file.
	/// </summary>
	public void Save(string fileName)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException(nameof(fileName));
		}

		using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
		Save(fs);
	}

	/// <summary>
	/// Scrolls to the specified line/column.
	/// This method requires that the TextEditor was already assigned a size (layout engine must have run prior).
	/// </summary>
	public void ScrollTo(int line, int column)
	{
		const double minimumScrollFraction = 0.3;
		ScrollTo(line, column, VisualYPosition.LineMiddle,
			null != ScrollViewer
				? ScrollViewer.Viewport.Height / 2
				: 0.0,
			minimumScrollFraction
		);
	}

	/// <summary>
	/// Scrolls to the specified line/column.
	/// This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).
	/// </summary>
	/// <param name="line"> Line to scroll to. </param>
	/// <param name="column"> Column to scroll to (important if wrapping is 'on', and for the horizontal scroll position). </param>
	/// <param name="yPositionMode"> The mode how to reference the Y position of the line. </param>
	/// <param name="referencedVerticalViewPortOffset"> Offset from the top of the viewport to where the referenced line/column should be positioned. </param>
	/// <param name="minimumScrollFraction"> The minimum vertical and/or horizontal scroll offset, expressed as fraction of the height or width of the viewport window, respectively. </param>
	public void ScrollTo(int line, int column, VisualYPosition yPositionMode,
		double referencedVerticalViewPortOffset, double minimumScrollFraction)
	{
		var textView = TextArea.TextView;
		var document = textView.Document;
		if ((ScrollViewer != null) && (document != null))
		{
			if (line < 1)
			{
				line = 1;
			}
			if (line > document.LineCount)
			{
				line = document.LineCount;
			}

			ILogicalScrollable scrollInfo = textView;
			if (!scrollInfo.CanHorizontallyScroll)
			{
				// Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
				// to the correct position.
				// This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
				var vl = textView.GetOrConstructVisualLine(document.GetLineByNumber(line));
				var remainingHeight = referencedVerticalViewPortOffset;

				while (remainingHeight > 0)
				{
					var prevLine = vl.FirstDocumentLine.PreviousLine;
					if (prevLine == null)
					{
						break;
					}
					vl = textView.GetOrConstructVisualLine(prevLine);
					remainingHeight -= vl.Height;
				}
			}

			var p = TextArea.TextView.GetVisualPosition(
				new TextViewPosition(line, Math.Max(1, column)),
				yPositionMode);

			var targetX = ScrollViewer.Offset.X;
			var targetY = ScrollViewer.Offset.Y;

			var verticalPos = p.Y - referencedVerticalViewPortOffset;
			if (Math.Abs(verticalPos - ScrollViewer.Offset.Y) >
				(minimumScrollFraction * ScrollViewer.Viewport.Height))
			{
				targetY = Math.Max(0, verticalPos);
			}

			if (column > 0)
			{
				if (p.X > (ScrollViewer.Viewport.Width - (Caret.MinimumDistanceToViewBorder * 2)))
				{
					var horizontalPos = Math.Max(0, p.X - (ScrollViewer.Viewport.Width / 2));
					if (Math.Abs(horizontalPos - ScrollViewer.Offset.X) >
						(minimumScrollFraction * ScrollViewer.Viewport.Width))
					{
						targetX = 0;
					}
				}
				else
				{
					targetX = 0;
				}
			}

			if ((targetX != ScrollViewer.Offset.X) || (targetY != ScrollViewer.Offset.Y))
			{
				ScrollViewer.Offset = new Vector(targetX, targetY);
			}
		}
	}

	/// <summary>
	/// Scrolls to the end of the document.
	/// </summary>
	public void ScrollToEnd()
	{
		ApplyTemplate(); // ensure scrollViewer is created
		ScrollViewer?.ScrollToEnd();
	}

	/// <summary>
	/// Scrolls to the start of the document.
	/// </summary>
	public void ScrollToHome()
	{
		ApplyTemplate(); // ensure scrollViewer is created
		ScrollViewer?.ScrollToHome();
	}

	/// <summary>
	/// Scrolls to the specified position in the document.
	/// </summary>
	public void ScrollToHorizontalOffset(double offset)
	{
		ApplyTemplate(); // ensure scrollViewer is created
		//if (scrollViewer != null)
		//    scrollViewer.ScrollToHorizontalOffset(offset);
	}

	/// <summary>
	/// Scrolls to the specified line.
	/// This method requires that the TextEditor was already assigned a size (layout engine must have run prior).
	/// </summary>
	public void ScrollToLine(int line)
	{
		ScrollTo(line, -1);
	}

	/// <summary>
	/// Scrolls to the specified position in the document.
	/// </summary>
	public void ScrollToVerticalOffset(double offset)
	{
		ApplyTemplate(); // ensure scrollViewer is created
		//if (scrollViewer != null)
		//    scrollViewer.ScrollToVerticalOffset(offset);
	}

	/// <summary>
	/// Selects the specified text section.
	/// </summary>
	public void Select(int start, int length)
	{
		var documentLength = Document?.TextLength ?? 0;
		if ((start < 0) || (start > documentLength))
		{
			throw new ArgumentOutOfRangeException(nameof(start), start, "Value must be between 0 and " + documentLength);
		}
		if ((length < 0) || ((start + length) > documentLength))
		{
			throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be between 0 and " + (documentLength - start));
		}
		TextArea.Selection = Selection.Create(TextArea, start, start + length);
		TextArea.Caret.Offset = start + length;
	}

	/// <summary>
	/// Selects the entire text.
	/// </summary>
	public void SelectAll()
	{
		if (CanSelectAll)
		{
			ApplicationCommands.SelectAll.Execute(null, TextArea);
		}
	}

	public void SetSyntaxHighlighter(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return;
		}

		var fileInfo = new FileInfo(filePath);
		SetSyntaxHighlighterByExtension(fileInfo.Extension);
	}

	public void SetSyntaxHighlighterByExtension(string fileExtension)
	{
		if (string.IsNullOrWhiteSpace(fileExtension))
		{
			return;
		}

		var definition = HighlightingManager.Instance.GetDefinitionByExtension(fileExtension);

		if (definition != null)
		{
			SyntaxHighlighting = definition;
		}
	}

	/// <summary>
	/// Undoes the most recent command.
	/// </summary>
	/// <returns> True is the undo operation was successful, false is the undo stack is empty. </returns>
	public bool Undo()
	{
		if (CanUndo)
		{
			ApplicationCommands.Undo.Execute(null, TextArea);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Creates the highlighting colorizer for the specified highlighting definition.
	/// Allows derived classes to provide custom colorizer implementations for special highlighting definitions.
	/// </summary>
	/// <returns> </returns>
	protected virtual IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
	{
		if (highlightingDefinition == null)
		{
			throw new ArgumentNullException(nameof(highlightingDefinition));
		}
		return new HighlightingColorizer(highlightingDefinition);
	}

	protected virtual string GetCompletionText()
	{
		return Document.GetText(0, CaretOffset);
	}

	protected TextDocument GetDocument()
	{
		var document = Document;
		if (document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		return document;
	}

	protected virtual IReadOnlySectionProvider GetReadOnlyProvider()
	{
		return IsReadOnly
			? ReadOnlySectionDocument.Instance
			: NoReadOnlySections.Instance;
	}

	protected void MoveCaret(CaretMovementType direction)
	{
		TextArea.MoveCaret(direction);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		ScrollViewer = (ScrollViewer) e.NameScope.Find("PART_ScrollViewer");
		if (ScrollViewer != null)
		{
			ScrollViewer.Content = TextArea;
		}
		SearchPanel = SearchPanel.Install(this);
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnAttachedToLogicalTree(e);

		if ((SearchPanel != null) && _wasSearchPanelOpened)
		{
			SearchPanel.Open();
		}
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		HighlightingManager.Instance.RegisterForUpdates(this);
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromLogicalTree(e);

		if (SearchPanel != null)
		{
			_wasSearchPanelOpened = SearchPanel.IsOpened;
			if (SearchPanel.IsOpened)
			{
				SearchPanel.Close();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		HighlightingManager.Instance.UnregisterForUpdates(this);
		base.OnDetachedFromVisualTree(e);
	}

	/// <summary>
	/// Raises the <see cref="DocumentChanged" /> event.
	/// </summary>
	protected virtual void OnDocumentChanged(DocumentChangedEventArgs e)
	{
		DocumentChanged?.Invoke(this, e);
	}

	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		base.OnGotFocus(e);
		TextArea.Focus();
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (CompletionProvider?.ShouldTrigger(e.Key, e.KeyModifiers, out var silent) == true)
		{
			e.Handled = true;
			base.OnKeyDown(e);

			if (!silent && (e.KeySymbol != null))
			{
				Document.Insert(CaretOffset, e.KeySymbol);
			}

			var line = GetCompletionText();

			Task.Run(() =>
			{
				if (CompletionProvider.TryGetAutoComplete(line, out var prefix, out var data))
				{
					this.Dispatch(() => CompletionWindowOpen(prefix, data));
				}
			});

			return;
		}

		base.OnKeyDown(e);
	}

	/// <summary>
	/// Raises the <see cref="OptionChanged" /> event.
	/// </summary>
	protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
	{
		OptionChanged?.Invoke(this, e);
	}

	protected void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
	{
		if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			if ((e.Delta.Y > 0) && (FontSize < 40))
			{
				FontSize += 1;
				e.Handled = true;
			}

			if ((e.Delta.Y < 0) && (FontSize > 12))
			{
				FontSize -= 1;
				e.Handled = true;
			}
		}
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == WordWrapProperty)
		{
			if (WordWrap)
			{
				_horizontalScrollBarVisibilityBck = HorizontalScrollBarVisibility;
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
			}
			else
			{
				HorizontalScrollBarVisibility = _horizontalScrollBarVisibilityBck;
			}
		}
	}

	protected void OnPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
	{
		OnOptionChanged(e);
	}

	/// <summary>
	/// Raises the <see cref="TextChanged" /> event.
	/// </summary>
	protected virtual void OnTextChanged(EventArgs e)
	{
		TextChanged?.Invoke(this, e);
	}

	/// <inheritdoc />
	protected override void OnTextInput(TextInputEventArgs e)
	{
		if (!e.Handled)
		{
			TextArea.OnTextInputFromTextEditor(e);
			e.Handled = true;
		}
		base.OnTextInput(e);
	}

	private void CompletionWindowOnClosed(object sender, EventArgs e)
	{
		if (sender is not CompletionWindow completionWindow)
		{
			return;
		}

		completionWindow.Closed -= CompletionWindowOnClosed;

		if (completionWindow == CompletionWindow)
		{
			// Only clear if the windows closing is our window
			// it may have been replaced with a different autocomplete?
			CompletionWindow = null;
		}
	}

	private void CompletionWindowOpen(string prefix, ICompletionData[] suggestions)
	{
		CompletionWindowOnClosed(CompletionWindow, EventArgs.Empty);

		CompletionWindow = new CompletionWindow(TextArea, prefix, null, suggestions);
		CompletionWindow.Closed += CompletionWindowOnClosed;
		CompletionWindow.Show();
	}

	object IServiceProvider.GetService(Type serviceType)
	{
		return TextArea.GetService(serviceType);
	}

	private void HandleIsOriginalChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "IsOriginalFile")
		{
			var document = Document;
			if (document != null)
			{
				SetValue(IsModifiedProperty, (object) !document.UndoStack.IsOriginalFile);
			}
		}
	}

	private static void OnDocumentChanged(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as TextEditor)?.OnDocumentChanged((TextDocument) e.OldValue, (TextDocument) e.NewValue);
	}

	private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
	{
		if (oldValue != null)
		{
			TextDocumentWeakEventManager.TextChanged.RemoveHandler(oldValue, OnTextChanged);
			PropertyChangedWeakEventManager.RemoveHandler(oldValue.UndoStack, OnUndoStackPropertyChangedHandler);
		}
		TextArea.Document = newValue;
		if (newValue != null)
		{
			TextDocumentWeakEventManager.TextChanged.AddHandler(newValue, OnTextChanged);
			PropertyChangedWeakEventManager.AddHandler(newValue.UndoStack, OnUndoStackPropertyChangedHandler);
		}
		OnDocumentChanged(new DocumentChangedEventArgs(oldValue, newValue));
		OnTextChanged(EventArgs.Empty);
	}

	private static void OnFontFamilyPropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var editor = e.Sender as TextEditor;
		editor?.TextArea.TextView.SetValue(FontFamilyProperty, e.NewValue);
	}

	private static void OnFontSizePropertyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		switch (e.Sender)
		{
			case TextEditor editor:
			{
				editor.TextArea.TextView.SetValue(FontSizeProperty, e.NewValue);

				var lineNumberMargin = editor.TextArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;
				lineNumberMargin?.SetValue(LineNumberMargin.FontSizeProperty, e.NewValue);
				break;
			}
		}
	}

	private static void OnIsModifiedChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var editor = e.Sender as TextEditor;
		var document = editor?.Document;
		if (document == null)
		{
			return;
		}

		var undoStack = document.UndoStack;

		if ((e.NewValue != null) && (bool) e.NewValue)
		{
			if (undoStack.IsOriginalFile)
			{
				undoStack.DiscardOriginalFileMarker();
			}
		}
		else
		{
			undoStack.MarkAsOriginalFile();
		}
	}

	private static void OnIsReadOnlyChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is TextEditor editor)
		{
			var isReadonly = e.GetNewValue<bool>();

			editor.TextArea.ReadOnlySectionProvider = editor.GetReadOnlyProvider();

			if (editor.SearchPanel != null)
			{
				editor.SearchPanel.IsReplaceMode = !isReadonly && editor.SearchPanel.IsReplaceMode;
			}
		}
	}

	private static void OnLineNumbersBackgroundChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var editor = e.Sender as TextEditor;
		var lineNumberMargin = editor?.TextArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;
		lineNumberMargin?.SetValue(BackgroundProperty, e.NewValue);
	}

	private static void OnLineNumbersForegroundChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var editor = e.Sender as TextEditor;
		var lineNumberMargin = editor?.TextArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;
		lineNumberMargin?.SetValue(ForegroundProperty, e.NewValue);
	}

	private static void OnOptionsChanged(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as TextEditor)?.OnOptionsChanged((TextEditorOptions) e.OldValue, (TextEditorOptions) e.NewValue);
	}

	private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
	{
		if (oldValue != null)
		{
			PropertyChangedWeakEventManager.RemoveHandler(oldValue, OnPropertyChangedHandler);
		}
		TextArea.Options = newValue;
		if (newValue != null)
		{
			PropertyChangedWeakEventManager.AddHandler(newValue, OnPropertyChangedHandler);
		}
		OnOptionChanged(new PropertyChangedEventArgs(null));
	}

	private static void OnShowLineNumbersChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is not TextEditor editor)
		{
			return;
		}

		var leftMargins = editor.TextArea.LeftMargins;
		if ((e.NewValue != null) && (bool) e.NewValue)
		{
			var lineNumbers = new LineNumberMargin
			{
				Margin = editor.LineNumbersMargin
			};
			var line = (Line) DottedLineMargin.Create();
			leftMargins.Insert(0, lineNumbers);
			leftMargins.Insert(1, line);

			var lineNumbersForeground = new Binding(nameof(LineNumbersForeground), BindingMode.OneWay) { Source = editor };
			var lineNumbersFontSize = new Binding(nameof(FontSize), BindingMode.OneWay) { Source = editor };
			line.Bind(Shape.StrokeProperty, lineNumbersForeground);
			lineNumbers.Bind(TextElement.ForegroundProperty, lineNumbersForeground);
			lineNumbers.Bind(LineNumberMargin.FontSizeProperty, lineNumbersFontSize);
		}
		else
		{
			for (var i = 0; i < leftMargins.Count; i++)
			{
				if (leftMargins[i] is not LineNumberMargin)
				{
					continue;
				}

				leftMargins.RemoveAt(i);

				if ((i < leftMargins.Count)
					&& DottedLineMargin.IsDottedLineMargin(leftMargins[i]))
				{
					leftMargins.RemoveAt(i);
				}
				break;
			}
		}
	}

	private static void OnSyntaxHighlightingChanged(AvaloniaPropertyChangedEventArgs e)
	{
		(e.Sender as TextEditor)?.OnSyntaxHighlightingChanged(e.NewValue as IHighlightingDefinition);
	}

	private void OnSyntaxHighlightingChanged(IHighlightingDefinition newValue)
	{
		if (_colorizer != null)
		{
			TextArea.TextView.LineTransformers.Remove(_colorizer);
			_colorizer = null;
		}

		if (newValue == null)
		{
			return;
		}

		_colorizer = CreateColorizer(newValue);

		if (_colorizer != null)
		{
			TextArea.TextView.LineTransformers.Insert(0, _colorizer);
		}
	}

	private void OnTextChanged(object sender, EventArgs e)
	{
		OnTextChanged(e);
	}

	private void OnUndoStackPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(UndoStack.IsOriginalFile))
		{
			HandleIsOriginalChanged(e);
		}
	}

	private static void SearchResultsBrushChangedCallback(AvaloniaPropertyChangedEventArgs e)
	{
		var editor = e.Sender as TextEditor;

		editor?.SearchPanel?.SetSearchResultsBrush(e.GetNewValue<IBrush>());
	}

	#endregion

	#region Events

	/// <summary>
	/// Occurs when the document property has changed.
	/// </summary>
	public event EventHandler<DocumentChangedEventArgs> DocumentChanged;

	/// <summary>
	/// Occurs when a text editor option has changed.
	/// </summary>
	public event PropertyChangedEventHandler OptionChanged;

	/// <summary>
	/// Occurs when the pointer has hovered over a fixed location for some time.
	/// </summary>
	public event EventHandler<PointerEventArgs> PointerHover
	{
		add => AddHandler(PointerHoverEvent, value);
		remove => RemoveHandler(PointerHoverEvent, value);
	}

	/// <summary>
	/// Occurs when the pointer had previously hovered but now started moving again.
	/// </summary>
	public event EventHandler<PointerEventArgs> PointerHoverStopped
	{
		add => AddHandler(PointerHoverStoppedEvent, value);
		remove => RemoveHandler(PointerHoverStoppedEvent, value);
	}

	/// <summary>
	/// Occurs when the pointer has hovered over a fixed location for some time.
	/// </summary>
	public event EventHandler<PointerEventArgs> PreviewPointerHover
	{
		add => AddHandler(PreviewPointerHoverEvent, value);
		remove => RemoveHandler(PreviewPointerHoverEvent, value);
	}

	/// <summary>
	/// Occurs when the pointer had previously hovered but now started moving again.
	/// </summary>
	public event EventHandler<PointerEventArgs> PreviewPointerHoverStopped
	{
		add => AddHandler(PreviewPointerHoverStoppedEvent, value);
		remove => RemoveHandler(PreviewPointerHoverStoppedEvent, value);
	}

	/// <summary>
	/// Occurs when the Text property changes.
	/// </summary>
	public event EventHandler TextChanged;

	#endregion
}