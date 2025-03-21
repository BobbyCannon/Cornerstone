#region References

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia.HexEditor.Document;
using Cornerstone.Avalonia.HexEditor.Editing;
using Cornerstone.Avalonia.HexEditor.Rendering;

#endregion

namespace Cornerstone.Avalonia.HexEditor;

/// <summary>
/// A control that allows for displaying and editing binary data in columns.
/// </summary>
/// <remarks>
/// Forked from https://github.com/wieslawsoltes/HexView
/// </remarks>
public class HexEditor : CornerstoneTemplatedControl
{
	#region Fields

	/// <summary>
	/// Dependency property for <see cref="ColumnPadding" />
	/// </summary>
	public static readonly DirectProperty<HexEditor, double> ColumnPaddingProperty;

	/// <summary>
	/// Dependency property for <see cref="Columns" />.
	/// </summary>
	public static readonly DirectProperty<HexEditor, HexView.ColumnCollection> ColumnsProperty;

	/// <summary>
	/// Dependency property for <see cref="Document" />.
	/// </summary>
	public static readonly StyledProperty<IBinaryDocument> DocumentProperty;

	/// <summary>
	/// Dependency property for <see cref="HorizontalScrollBarVisibility" />
	/// </summary>
	public static readonly AttachedProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty;

	/// <summary>
	/// Dependency property for <see cref="VerticalScrollBarVisibility" />
	/// </summary>
	public static readonly AttachedProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty;

	private bool _isMouseDragging;
	private ScrollViewer _scrollViewer;
	private BitLocation? _selectionAnchorPoint;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new empty hex editor.
	/// </summary>
	public HexEditor()
	{
		HexView = new HexView();
		Caret = new Caret(HexView);
		Selection = new Selection(HexView);

		AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
		AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

		HexView.Layers.InsertBefore<TextLayer>(new CurrentLineLayer(Caret, Selection));
		HexView.Layers.InsertBefore<TextLayer>(new SelectionLayer(Caret, Selection));
		HexView.Layers.Add(new CaretLayer(Caret));
		HexView.DocumentChanged += HexViewOnDocumentChanged;

		Caret.PrimaryColumnIndex = 1;
		Caret.LocationChanged += CaretOnLocationChanged;

		Document = new DynamicBinaryDocument();
	}

	static HexEditor()
	{
		ColumnPaddingProperty = AvaloniaProperty.RegisterDirect<HexEditor, double>(nameof(ColumnPadding),
			editor => editor.ColumnPadding, (editor, value) => editor.ColumnPadding = value);
		ColumnsProperty = AvaloniaProperty.RegisterDirect<HexEditor, HexView.ColumnCollection>(nameof(Columns), o => o.Columns);
		DocumentProperty = AvaloniaProperty.Register<HexEditor, IBinaryDocument>(nameof(Document));
		HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner<HexEditor>();
		VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner<HexEditor>();
		FocusableProperty.OverrideDefaultValue<HexEditor>(true);
		HorizontalScrollBarVisibilityProperty.OverrideDefaultValue<HexEditor>(ScrollBarVisibility.Auto);
		VerticalScrollBarVisibilityProperty.OverrideDefaultValue<HexEditor>(ScrollBarVisibility.Auto);
		FontFamilyProperty.Changed.AddClassHandler<HexEditor, FontFamily>(ForwardToHexView);
		FontSizeProperty.Changed.AddClassHandler<HexEditor, double>(ForwardToHexView);
		ForegroundProperty.Changed.AddClassHandler<HexEditor, IBrush>(ForwardToHexView);
		DocumentProperty.Changed.AddClassHandler<HexEditor, IBinaryDocument>(OnDocumentChanged);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the caret object in the editor control.
	/// </summary>
	public Caret Caret { get; }

	/// <summary>
	/// Gets the amount of spacing in between columns.
	/// </summary>
	public double ColumnPadding
	{
		get => HexView.ColumnPadding;
		set => HexView.ColumnPadding = value;
	}

	/// <summary>
	/// Gets the columns displayed in the hex editor.
	/// </summary>
	public HexView.ColumnCollection Columns => HexView.Columns;

	/// <summary>
	/// Gets or sets the binary document that is currently being displayed.
	/// </summary>
	public IBinaryDocument Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <summary>
	/// Gets the embedded hex view control responsible for rendering the data.
	/// </summary>
	public HexView HexView { get; }

	/// <summary>
	/// Gets or sets the horizontal scroll bar visibility.
	/// </summary>
	public ScrollBarVisibility HorizontalScrollBarVisibility
	{
		get => GetValue(HorizontalScrollBarVisibilityProperty);
		set => SetValue(HorizontalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Gets the current selection in the editor control.
	/// </summary>
	public Selection Selection { get; }

	/// <summary>
	/// Gets or sets the horizontal scroll bar visibility.
	/// </summary>
	public ScrollBarVisibility VerticalScrollBarVisibility
	{
		get => GetValue(VerticalScrollBarVisibilityProperty);
		set => SetValue(VerticalScrollBarVisibilityProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Deletes the currently selected bytes and the previous byte from the document.
	/// </summary>
	public void Backspace()
	{
		if (Caret.PrimaryColumn is not { } column)
		{
			return;
		}

		if (Document is not { CanRemove: true } document)
		{
			return;
		}

		var selectionRange = Selection.Range;

		if (selectionRange.ByteLength <= 1)
		{
			if (Caret.Location.BitIndex == column.FirstBitIndex)
			{
				// If caret is at the left-most cell of a byte, it is more intuitive to have it remove the previous byte.
				// In this case, we can only perform the deletion if we're not at the beginning of the document.
				if (selectionRange.Start.ByteIndex != 0)
				{
					document.Remove(selectionRange.Start.ByteIndex - 1, 1);
					Caret.Location = new BitLocation(selectionRange.Start.ByteIndex - 1, column.FirstBitIndex);
				}
			}
			else
			{
				// If caret is not at a left-most cell of a byte, it is more intuitive to have it remove the current byte.
				document.Remove(selectionRange.Start.ByteIndex, 1);
				Caret.Location = selectionRange.Start.ByteIndex == 0
					? new BitLocation(0, column.FirstBitIndex)
					: new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
			}
		}
		else
		{
			// Otherwise, simply treat as a normal delete.
			document.Remove(selectionRange.Start.ByteIndex, selectionRange.ByteLength);
			Caret.Location = new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
		}

		Selection.Range = Caret.Location.ToSingleByteRange();
		_selectionAnchorPoint = null;
	}

	/// <summary>
	/// Copies the currently selected text to the clipboard.
	/// </summary>
	public async Task Copy()
	{
		if (Caret.PrimaryColumn is not { } column || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
		{
			return;
		}

		var text = column.GetText(Selection.Range);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		await clipboard.SetTextAsync(text);
	}

	/// <summary>
	/// Deletes the currently selected bytes from the document.
	/// </summary>
	public void Delete()
	{
		if (Caret.PrimaryColumn is not { } column)
		{
			return;
		}

		if (Document is not { CanRemove: true } document)
		{
			return;
		}

		var selectionRange = Selection.Range;
		document.Remove(selectionRange.Start.ByteIndex, selectionRange.ByteLength);

		Caret.Location = new BitLocation(selectionRange.Start.ByteIndex, column.FirstBitIndex);
		Selection.Range = Caret.Location.ToSingleByteRange();

		_selectionAnchorPoint = null;
	}

	/// <summary>
	/// Pastes text on the clipboard into the current column.
	/// </summary>
	public async Task Paste()
	{
		if ((Caret.Mode == EditingMode.Insert) && Document is not { CanInsert: true })
		{
			return;
		}

		var oldLocation = Caret.Location;
		if (Caret.PrimaryColumn is not { } column || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
		{
			return;
		}

		var text = await clipboard.GetTextAsync();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		var newLocation = oldLocation;
		if (!column.HandleTextInput(ref newLocation, text, Caret.Mode))
		{
			return;
		}

		Caret.Location = newLocation;
		UpdateSelection(oldLocation, false);
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

		if (_scrollViewer is not null)
		{
			_scrollViewer.Content = HexView;
		}
	}

	/// <summary>
	/// Fires the <see cref="DocumentChanged" /> event.
	/// </summary>
	/// <param name="e"> The arguments describing the event. </param>
	protected virtual void OnDocumentChanged(DocumentChangedEventArgs e)
	{
		DocumentChanged?.Invoke(this, e);
	}

	/// <inheritdoc />
	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		base.OnGotFocus(e);
		HexView.Focus();
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		Cursor = HexView.GetColumnByPoint(e.GetPosition(this)) is { } hoverColumn
			? hoverColumn.Cursor
			: null;

		if (_isMouseDragging
			&& _selectionAnchorPoint is { } anchorPoint
			&& Caret.PrimaryColumn is { } column)
		{
			var position = e.GetPosition(HexView);
			if (HexView.GetLocationByPoint(position, column) is { } location)
			{
				Selection.Range = new BitRange(
					location.Min(anchorPoint).AlignDown(),
					location.Max(anchorPoint).NextOrMax().AlignUp()
				);

				Caret.Location = location;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		var point = e.GetCurrentPoint(HexView);
		if (point.Properties.IsLeftButtonPressed)
		{
			var position = point.Position;

			if (HexView.GetColumnByPoint(position) is CellBasedColumn column)
			{
				Caret.PrimaryColumnIndex = column.Index;
				if (HexView.GetLocationByPoint(position) is { } location)
				{
					// Update selection when holding down the shift key.
					var isShiftDown = (e.KeyModifiers & KeyModifiers.Shift) != 0;
					if (isShiftDown)
					{
						_selectionAnchorPoint ??= Caret.Location;
						Selection.Range = new BitRange(
							location.Min(_selectionAnchorPoint.Value).AlignDown(),
							location.Max(_selectionAnchorPoint.Value).NextOrMax().AlignUp()
						);
					}
					else
					{
						Selection.Range = new BitRange(location.AlignDown(), location.NextOrMax().AlignUp());
						_selectionAnchorPoint = location;
					}

					// Actually update the caret.
					Caret.Location = location;
					_isMouseDragging = true;
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);

		_isMouseDragging = false;
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

	/// <inheritdoc />
	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		HexView.BringIntoView(Caret.Location);
		base.OnSizeChanged(e);
	}

	/// <inheritdoc />
	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);

		// Are we in a writeable document?
		if (Document is not { IsReadOnly: false })
		{
			return;
		}

		// Do we have any text to write into a column?
		if (string.IsNullOrEmpty(e.Text) || Caret.PrimaryColumn is null)
		{
			return;
		}

		if (Caret.Mode == EditingMode.Insert)
		{
			// Can we insert?
			if (!Document.CanInsert)
			{
				return;
			}

			// If we selected something while inserting, a natural expectation is that the selection is deleted first.
			if (Selection.Range.ByteLength > 1)
			{
				if (!Document.CanRemove)
				{
					return;
				}

				Delete();
			}
		}

		// Dispatch text input to the primary column.
		var location = Caret.Location;
		if (!Caret.PrimaryColumn.HandleTextInput(ref location, e.Text, Caret.Mode))
		{
			return;
		}

		// Update caret location.
		Caret.Location = location;
		UpdateSelection(Caret.Location, false);
	}

	private void CaretOnLocationChanged(object sender, EventArgs e)
	{
		HexView.BringIntoView(Caret.Location);
	}

	private static void ForwardToHexView<TValue>(HexEditor sender, AvaloniaPropertyChangedEventArgs<TValue> e)
	{
		sender.HexView.SetValue(e.Property, e.NewValue.Value);
	}

	private void HexViewOnDocumentChanged(object sender, DocumentChangedEventArgs e)
	{
		Document = e.New;
		Caret.Location = default;
		UpdateSelection(Caret.Location, false);
		OnDocumentChanged(e);
	}

	private static void OnDocumentChanged(HexEditor sender, AvaloniaPropertyChangedEventArgs<IBinaryDocument> e)
	{
		sender.HexView.Document = e.NewValue.Value;
	}

	private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		var oldLocation = Caret.Location;
		var isShiftDown = (e.KeyModifiers & KeyModifiers.Shift) != 0;

		switch (e.Key)
		{
			case Key.A when (e.KeyModifiers & KeyModifiers.Control) != 0:
				Selection.SelectAll();
				break;

			case Key.C when (e.KeyModifiers & KeyModifiers.Control) != 0:
				await Copy();
				break;

			case Key.V when (e.KeyModifiers & KeyModifiers.Control) != 0:
				await Paste();
				break;

			case Key.Home when (e.KeyModifiers & KeyModifiers.Control) != 0:
				Caret.GoToStartOfDocument();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.Home:
				Caret.GoToStartOfLine();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.End when (e.KeyModifiers & KeyModifiers.Control) != 0:
				Caret.GoToEndOfDocument();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.End:
				Caret.GoToEndOfLine();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.Left:
				Caret.GoLeft();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.Up when (e.KeyModifiers & KeyModifiers.Control) != 0:
				HexView.ScrollOffset = new Vector(
					HexView.ScrollOffset.X,
					Math.Max(0, HexView.ScrollOffset.Y - 1)
				);
				break;

			case Key.Up:
				Caret.GoUp();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.PageUp:
				Caret.GoPageUp();
				UpdateSelection(oldLocation, isShiftDown);
				e.Handled = true;
				break;

			case Key.Right:
				Caret.GoRight();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.Down when (e.KeyModifiers & KeyModifiers.Control) != 0:
				HexView.ScrollOffset = new Vector(
					HexView.ScrollOffset.X,
					Math.Min(HexView.Extent.Height - 1, HexView.ScrollOffset.Y + 1)
				);
				break;

			case Key.Down:
				Caret.GoDown();
				UpdateSelection(oldLocation, isShiftDown);
				break;

			case Key.PageDown:
				Caret.GoPageDown();
				UpdateSelection(oldLocation, isShiftDown);
				e.Handled = true;
				break;

			case Key.Insert:
				Caret.Mode = Caret.Mode == EditingMode.Overwrite
					? EditingMode.Insert
					: EditingMode.Overwrite;
				break;

			case Key.Delete:
				Delete();
				break;

			case Key.Back:
				Backspace();
				break;
		}
	}

	private void UpdateSelection(BitLocation from, bool expand)
	{
		if (!expand)
		{
			_selectionAnchorPoint = null;
			Selection.Range = new BitRange(Caret.Location.AlignDown(), Caret.Location.NextOrMax().AlignUp());
		}
		else
		{
			_selectionAnchorPoint ??= from.AlignDown();
			Selection.Range = new BitRange(
				Caret.Location.Min(_selectionAnchorPoint.Value).AlignDown(),
				Caret.Location.Max(_selectionAnchorPoint.Value).NextOrMax().AlignUp()
			);
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Fires when the document in the hex editor has changed.
	/// </summary>
	public event EventHandler<DocumentChangedEventArgs> DocumentChanged;

	#endregion
}