#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Provides a render target for binary data.
/// </summary>
public class HexView : CornerstoneControl, ILogicalScrollable
{
	#region Fields

	/// <summary>
	/// Dependency property for <see cref="ActualBytesPerLine" />.
	/// </summary>
	public static readonly DirectProperty<HexView, int> ActualBytesPerLineProperty =
		AvaloniaProperty.RegisterDirect<HexView, int>(nameof(ActualBytesPerLine), o => o.ActualBytesPerLine);

	/// <summary>
	/// Dependency property for <see cref="BytesPerLine" />.
	/// </summary>
	public static readonly StyledProperty<int?> BytesPerLineProperty =
		AvaloniaProperty.Register<HexView, int?>(nameof(BytesPerLine));

	/// <summary>
	/// Dependency property for <see cref="ColumnPadding" />.
	/// </summary>
	public static readonly StyledProperty<double> ColumnPaddingProperty =
		AvaloniaProperty.Register<HexView, double>(nameof(ColumnPadding), 5D);

	/// <summary>
	/// Dependency property for <see cref="Columns" />.
	/// </summary>
	public static readonly DirectProperty<HexView, ColumnCollection> ColumnsProperty =
		AvaloniaProperty.RegisterDirect<HexView, ColumnCollection>(nameof(Columns), o => o.Columns);

	/// <summary>
	/// Dependency property for <see cref="Document" />.
	/// </summary>
	public static readonly StyledProperty<IBinaryDocument> DocumentProperty =
		AvaloniaProperty.Register<HexView, IBinaryDocument>(nameof(Document));

	private int _actualBytesPerLine;
	private Size _extent;
	private Vector _scrollOffset;

	private readonly VisualBytesLinesBuffer _visualLines;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new hex view control.
	/// </summary>
	public HexView()
	{
		Columns = new ColumnCollection(this);

		_visualLines = new VisualBytesLinesBuffer(this);

		EnsureTextProperties();

		Layers = new LayerCollection(this)
		{
			new ColumnBackgroundLayer(),
			new CellGroupsLayer(),
			new TextLayer()
		};
	}

	static HexView()
	{
		FocusableProperty.OverrideDefaultValue<HexView>(true);

		TemplatedControl.FontFamilyProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
		TemplatedControl.FontSizeProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
		TemplatedControl.ForegroundProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);

		DocumentProperty.Changed.AddClassHandler<HexView>(OnDocumentChanged);

		AffectsArrange<HexView>(
			DocumentProperty,
			BytesPerLineProperty,
			ColumnPaddingProperty
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the total amount of bytes per line that are displayed in the control.
	/// </summary>
	public int ActualBytesPerLine
	{
		get => _actualBytesPerLine;
		private set
		{
			if (SetAndRaise(ActualBytesPerLineProperty, ref _actualBytesPerLine, value))
			{
				InvalidateVisualLines();
			}
		}
	}

	/// <summary>
	/// Gets or sets the fixed amount of bytes per line that should be displayed, or <c> null </c> if the number of
	/// bytes is proportional to the width of the control.
	/// </summary>
	public int? BytesPerLine
	{
		get => GetValue(BytesPerLineProperty);
		set => SetValue(BytesPerLineProperty, value);
	}

	/// <summary>
	/// Gets the amount of spacing in between columns.
	/// </summary>
	public double ColumnPadding
	{
		get => GetValue(ColumnPaddingProperty);
		set => SetValue(ColumnPaddingProperty, value);
	}

	/// <summary>
	/// Gets the columns displayed in the hex view.
	/// </summary>
	public ColumnCollection Columns { get; }

	/// <summary>
	/// Gets or sets the binary document that is currently being displayed.
	/// </summary>
	public IBinaryDocument Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <inheritdoc />
	public Size Extent
	{
		get => _extent;
		private set
		{
			if (_extent != value)
			{
				_extent = value;
				((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets the font family that is used for rendering the text in the hex view.
	/// </summary>
	public FontFamily FontFamily
	{
		get => GetValue(TemplatedControl.FontFamilyProperty);
		set => SetValue(TemplatedControl.FontFamilyProperty, value);
	}

	/// <summary>
	/// Gets the font size that is used for rendering the text in the hex view.
	/// </summary>
	public double FontSize
	{
		get => GetValue(TemplatedControl.FontSizeProperty);
		set => SetValue(TemplatedControl.FontSizeProperty, value);
	}

	/// <summary>
	/// Gets the base foreground brush that is used for rendering the text in the hex view.
	/// </summary>
	public IBrush Foreground
	{
		get => GetValue(TemplatedControl.ForegroundProperty);
		set => SetValue(TemplatedControl.ForegroundProperty, value);
	}

	/// <summary>
	/// Gets the binary range that is fully visible in the view, excluding lines that are only partially visible.
	/// </summary>
	public BitRange FullyVisibleRange { get; private set; }

	/// <summary>
	/// Gets a collection of render layers in the hex view.
	/// </summary>
	public LayerCollection Layers { get; }

	/// <summary>
	/// Gets a collection of line transformers that are applied to each line in the hex view.
	/// </summary>
	public ObservableCollection<ILineTransformer> LineTransformers { get; } = new();

	/// <inheritdoc />
	public Size PageScrollSize => new(0, VisualLines.Count);

	/// <summary>
	/// Gets or sets the current scroll offset.
	/// </summary>
	public Vector ScrollOffset
	{
		get => _scrollOffset;
		set
		{
			_scrollOffset = value;
			InvalidateArrange();
			((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
		}
	}

	/// <inheritdoc />
	public Size ScrollSize => new(0, 1);

	/// <summary>
	/// Gets the text run properties that are used for rendering the text in the hex view.
	/// </summary>
	public GenericTextRunProperties TextRunProperties { get; private set; }

	/// <summary>
	/// Gets the typeface that is used for rendering the text in the hex view.
	/// </summary>
	public Typeface Typeface { get; private set; }

	/// <summary>
	/// Gets the binary range that is currently visible in the view.
	/// </summary>
	public BitRange VisibleRange { get; private set; }

	/// <summary>
	/// Gets the current lines that are visible.
	/// </summary>
	public IReadOnlyList<VisualBytesLine> VisualLines => _visualLines;

	bool ILogicalScrollable.CanHorizontallyScroll { get; set; } = false;

	bool ILogicalScrollable.CanVerticallyScroll { get; set; } = true;

	bool ILogicalScrollable.IsLogicalScrollEnabled => true;

	/// <inheritdoc />
	Vector IScrollable.Offset
	{
		get => ScrollOffset;
		set => ScrollOffset = value;
	}

	Size IScrollable.Viewport => new(0, 1);

	#endregion

	#region Methods

	/// <summary>
	/// Ensures the provided bit location is put into view.
	/// </summary>
	/// <param name="location"> The location to scroll to. </param>
	/// <returns> <c> true </c> if the scroll offset has changed, <c> false </c> otherwise. </returns>
	public bool BringIntoView(BitLocation location)
	{
		if ((location.ByteIndex >= (Document?.Length + 1)) 
			|| FullyVisibleRange.Contains(location)
			|| (ActualBytesPerLine <= 0))
		{
			return false;
		}

		var firstLineIndex = FullyVisibleRange.Start.ByteIndex / (ulong) ActualBytesPerLine;
		var lastLineIndex = (FullyVisibleRange.End.ByteIndex - 1) / (ulong) ActualBytesPerLine;
		var targetLineIndex = location.ByteIndex / (ulong) ActualBytesPerLine;

		ulong newIndex;

		if (location > FullyVisibleRange.End)
		{
			var difference = targetLineIndex - lastLineIndex;
			newIndex = firstLineIndex + difference;
		}
		else if (location < FullyVisibleRange.Start)
		{
			var difference = firstLineIndex - targetLineIndex;
			newIndex = firstLineIndex - difference;
		}
		else
		{
			return false;
		}

		ScrollOffset = new Vector(0, newIndex);

		return true;
	}

	/// <summary>
	/// Gets the column containing the provided point.
	/// </summary>
	/// <param name="point"> The point. </param>
	/// <returns> The point, or <c> null </c> if the location does not fall inside a column. </returns>
	public Column GetColumnByPoint(Point point)
	{
		foreach (var column in Columns)
		{
			if (column.IsVisible && column.Bounds.Contains(point))
			{
				return column;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets the location of the cell under the provided point.
	/// </summary>
	/// <param name="point"> The point. </param>
	/// <returns> The location of the cell, or <c> null </c> if no cell is under the provided point. </returns>
	public BitLocation? GetLocationByPoint(Point point)
	{
		if (GetColumnByPoint(point) is not CellBasedColumn column)
		{
			return null;
		}

		return GetLocationByPoint(point, column);
	}

	/// <summary>
	/// Gets the location of the cell within a column under the provided point.
	/// </summary>
	/// <param name="point"> The point. </param>
	/// <param name="column"> The column </param>
	/// <returns> The location of the cell, or <c> null </c> if no cell is under the provided point. </returns>
	public BitLocation? GetLocationByPoint(Point point, CellBasedColumn column)
	{
		if (GetVisualLineByPoint(point) is not { } line)
		{
			return null;
		}

		return column.GetLocationByPoint(line, point);
	}

	/// <summary>
	/// Gets the visual line containing the provided location.
	/// </summary>
	/// <param name="location"> The location </param>
	/// <returns> The line, or <c> null </c> if the location is currently not visible. </returns>
	public VisualBytesLine GetVisualLineByLocation(BitLocation location)
	{
		if (!VisibleRange.Contains(location))
		{
			return null;
		}

		return _visualLines.GetVisualLineByLocation(location);
	}

	/// <summary>
	/// Gets the visual line containing the provided point.
	/// </summary>
	/// <param name="point"> The point </param>
	/// <returns> The line, or <c> null </c> if the location is currently not visible. </returns>
	public VisualBytesLine GetVisualLineByPoint(Point point)
	{
		for (var i = 0; i < VisualLines.Count; i++)
		{
			var line = VisualLines[i];
			if (line.Bounds.Contains(point))
			{
				return line;
			}
		}

		return null;
	}

	/// <summary>
	/// Enumerates all lines that overlap with the provided range.
	/// </summary>
	/// <param name="range"> The range. </param>
	/// <returns> The lines. </returns>
	public IEnumerable<VisualBytesLine> GetVisualLinesByRange(BitRange range)
	{
		if (!VisibleRange.OverlapsWith(range))
		{
			return [];
		}

		return _visualLines.GetVisualLinesByRange(range);
	}

	/// <summary>
	/// Invalidates the line that includes the provided location.
	/// </summary>
	/// <param name="location"> The location. </param>
	public void InvalidateVisualLine(BitLocation location)
	{
		var line = GetVisualLineByLocation(location);
		if (line is not null)
		{
			InvalidateVisualLine(line);
		}
	}

	/// <summary>
	/// Schedules a repaint of the provided visual line.
	/// </summary>
	/// <param name="line"> </param>
	public void InvalidateVisualLine(VisualBytesLine line)
	{
		line.Invalidate();
		InvalidateArrange();

		for (var i = 0; i < Layers.Count; i++)
		{
			if ((Layers[i].UpdateMoments & LayerRenderMoments.LineInvalidate) != 0)
			{
				Layers[i].InvalidateVisual();
			}
		}
	}

	/// <summary>
	/// Clears out all visual lines and schedules a new layout pass.
	/// </summary>
	public void InvalidateVisualLines()
	{
		_visualLines.Clear();
		InvalidateArrange();
	}

	/// <summary>
	/// Invalidates the lines that contain the bits in the provided range.
	/// </summary>
	/// <param name="range"> The range to invalidate. </param>
	public void InvalidateVisualLines(BitRange range)
	{
		if (!VisibleRange.OverlapsWith(range))
		{
			return;
		}

		foreach (var line in GetVisualLinesByRange(range))
		{
			line.Invalidate();
		}

		for (var i = 0; i < Layers.Count; i++)
		{
			if ((Layers[i].UpdateMoments & LayerRenderMoments.LineInvalidate) != 0)
			{
				Layers[i].InvalidateVisual();
			}
		}

		InvalidateArrange();
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		ComputeBytesPerLine(finalSize);
		UpdateColumnBounds();
		UpdateVisualLines(finalSize);

		Extent = Document is not null
			? new Size(0, Math.Ceiling((double) Document.Length / ActualBytesPerLine))
			: default;

		var hasResized = finalSize != Bounds.Size;

		for (var i = 0; i < Layers.Count; i++)
		{
			Layers[i].Arrange(new Rect(new Point(0, 0), finalSize));

			if (hasResized || ((Layers[i].UpdateMoments & LayerRenderMoments.NoResizeRearrange) != 0))
			{
				Layers[i].InvalidateVisual();
			}
		}

		return base.ArrangeOverride(finalSize);
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		for (var i = 0; i < Columns.Count; i++)
		{
			Columns[i].Measure();
		}

		for (var i = 0; i < Layers.Count; i++)
		{
			Layers[i].Measure(availableSize);
		}

		return base.MeasureOverride(availableSize);
	}

	/// <summary>
	/// Fires the <see cref="DocumentChanged" /> event.
	/// </summary>
	/// <param name="e"> The arguments describing the event. </param>
	protected virtual void OnDocumentChanged(DocumentChangedEventArgs e)
	{
		DocumentChanged?.Invoke(this, e);
	}

	bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
	{
		return false;
	}

	private void ComputeBytesPerLine(Size finalSize)
	{
		if (BytesPerLine is { } bytesPerLine)
		{
			ActualBytesPerLine = bytesPerLine;
			return;
		}

		// total                                            = minimum_width + n * word_width + (n - 1) * word_padding
		// 0                                                = total - (minimum_width + n * word_width + (n - 1) * word_padding)
		// n * word_width + (n - 1) * word_padding          = total - minimum_width
		// n * word_width + n * word_padding - word_padding = total - minimum_width
		// n * (word_width + word_padding) - word_padding   = total - minimum_width
		// n * (word_width + word_padding)                  = total - minimum_width + word_padding
		// n                                                = (total - minimum_width + word_padding) / (word_width + word_padding)

		double minimumWidth = 0;
		double wordWidth = 0;
		double wordPadding = 0;

		for (var i = 0; i < Columns.Count; i++)
		{
			var column = Columns[i];
			if (!column.IsVisible)
			{
				continue;
			}

			minimumWidth += column.MinimumSize.Width;
			if (i > 0)
			{
				minimumWidth += ColumnPadding;
			}

			if (column is CellBasedColumn x)
			{
				wordWidth += x.WordWidth;
				wordPadding += x.GroupPadding;
			}
		}

		var count = (int) (((finalSize.Width - minimumWidth) + wordPadding) / (wordWidth + wordPadding));
		ActualBytesPerLine = wordWidth != 0
			? Math.Max(1, count)
			: 16;
	}

	private void DocumentOnChanged(object sender, BinaryDocumentChange e)
	{
		switch (e.Type)
		{
			case BinaryDocumentChangeType.Modify:
				InvalidateVisualLines(e.AffectedRange);
				break;

			case BinaryDocumentChangeType.Insert:
			case BinaryDocumentChangeType.Remove:
				InvalidateVisualLines(e.AffectedRange.ExtendTo(Document!.ValidRanges.EnclosingRange.End));
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	[MemberNotNull(nameof(TextRunProperties))]
	private void EnsureTextProperties()
	{
		if (Typeface.FontFamily != FontFamily)
		{
			Typeface = new Typeface(FontFamily);
		}

		TextRunProperties = new GenericTextRunProperties(
			Typeface,
			FontSize,
			foregroundBrush: Foreground
		);
	}

	Control ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control from)
	{
		return null;
	}

	private static void OnDocumentChanged(HexView view, AvaloniaPropertyChangedEventArgs arg2)
	{
		view._scrollOffset = default;
		view.InvalidateVisualLines();

		var oldDocument = (IBinaryDocument) arg2.OldValue;
		if (oldDocument is not null)
		{
			oldDocument.Changed -= view.DocumentOnChanged;
		}

		var newDocument = (IBinaryDocument) arg2.NewValue;
		if (newDocument is not null)
		{
			newDocument.Changed += view.DocumentOnChanged;
		}

		view.OnDocumentChanged(new DocumentChangedEventArgs(
			oldDocument,
			newDocument
		));
	}

	private static void OnFontRelatedPropertyChanged(HexView arg1, AvaloniaPropertyChangedEventArgs arg2)
	{
		arg1.EnsureTextProperties();
		arg1.InvalidateMeasure();
		arg1.InvalidateVisualLines();
	}

	void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
	{
		ScrollInvalidated?.Invoke(this, e);
	}

	private void UpdateColumnBounds()
	{
		double currentX = 0;
		foreach (var column in Columns)
		{
			if (!column.IsVisible)
			{
				column.SetBounds(default);
			}
			else
			{
				var width = column.Width;
				column.SetBounds(new Rect(currentX, 0, width, Bounds.Height));
				currentX += width + ColumnPadding;
			}
		}
	}

	private void UpdateVisualLines(Size finalSize)
	{
		// No columns or no document means we need a completely empty control.
		if ((Columns.Count == 0) || Document is null)
		{
			_visualLines.Clear();

			VisibleRange = default;
			FullyVisibleRange = default;
			return;
		}

		// In case of an empty document, always ensure that there's at least one (empty) line rendered.
		if (Document.Length == 0)
		{
			_visualLines.Clear();

			var line = _visualLines.GetOrCreateVisualLine(new BitRange(0, 1));

			line.EnsureIsValid();
			line.Bounds = new Rect(0, 0, finalSize.Width, line.GetRequiredHeight());

			VisibleRange = line.VirtualRange;
			FullyVisibleRange = VisibleRange;
			return;
		}

		// Otherwise, ensure all visible lines are created.

		var startLocation = new BitLocation((ulong) ScrollOffset.Y * (ulong) ActualBytesPerLine);

		var currentRange = new BitRange(startLocation, startLocation);

		double currentY = 0;
		while ((currentY < finalSize.Height) && (currentRange.End.ByteIndex <= Document.Length))
		{
			// Get/create next visual line.
			var line = _visualLines.GetOrCreateVisualLine(new BitRange(
				currentRange.End.ByteIndex,
				Math.Min(Document.Length + 1, currentRange.End.ByteIndex + (ulong) ActualBytesPerLine)
			));

			line.EnsureIsValid();
			line.Bounds = new Rect(0, currentY, finalSize.Width, line.GetRequiredHeight());

			// Move to next line / range.
			currentY += line.Bounds.Height;
			currentRange = line.VirtualRange;
		}

		// Compute full visible range (including lines that are only slightly visible).
		VisibleRange = _visualLines.Count == 0
			? new BitRange(Document.Length, Document.Length)
			: new BitRange(startLocation, currentRange.End);

		// Get fully visible byte range.
		if ((_visualLines.Count == 0) || !(_visualLines[^1].Bounds.Bottom > finalSize.Height))
		{
			FullyVisibleRange = VisibleRange;
		}
		else
		{
			FullyVisibleRange = new BitRange(
				VisibleRange.Start,
				new BitLocation(VisibleRange.End.ByteIndex - (ulong) ActualBytesPerLine, 0)
			);
		}

		// Cut off excess visual lines.
		_visualLines.RemoveOutsideOfRange(VisibleRange);
	}

	#endregion

	#region Events

	/// <summary>
	/// Fires when the document in the hex editor has changed.
	/// </summary>
	public event EventHandler<DocumentChangedEventArgs> DocumentChanged;

	/// <inheritdoc />
	public event EventHandler ScrollInvalidated;

	#endregion

	#region Classes

	/// <summary>
	/// Represents a collection of columns that are added to a hex view.
	/// </summary>
	public class ColumnCollection : ObservableCollection<Column>
	{
		#region Fields

		private readonly HexView _owner;

		#endregion

		#region Constructors

		internal ColumnCollection(HexView owner)
		{
			_owner = owner;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets a single column by its type.
		/// </summary>
		/// <typeparam name="TColumn"> The column type. </typeparam>
		/// <returns> The column. </returns>
		public TColumn Get<TColumn>()
			where TColumn : Column
		{
			return Items.OfType<TColumn>().First();
		}

		/// <summary>
		/// Creates a new enumerator for the collection.
		/// </summary>
		/// <returns> The enumerator. </returns>
		public new Enumerator GetEnumerator()
		{
			return new(this);
		}

		/// <summary>
		/// Attempts to find a single column by its type.
		/// </summary>
		/// <typeparam name="TColumn"> The column type. </typeparam>
		/// <returns> The column, or <c> null </c> if no column of the provided type exists in the collection. </returns>
		public TColumn GetOrDefault<TColumn>()
			where TColumn : Column
		{
			return Items.OfType<TColumn>().FirstOrDefault();
		}

		/// <summary>
		/// Gets the index of a specific column.
		/// </summary>
		/// <typeparam name="TColumn"> The type of the column. </typeparam>
		/// <returns> The index, or <c> -1 </c> if the column is not present in the collection. </returns>
		public int IndexOf<TColumn>()
			where TColumn : Column
		{
			for (var i = 0; i < Count; i++)
			{
				if (Items[i] is TColumn)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inserts a column after another.
		/// </summary>
		/// <param name="column"> The column to insert. </param>
		/// <typeparam name="TColumn"> The type of column to insert after. </typeparam>
		public void InsertAfter<TColumn>(Column column)
			where TColumn : Column
		{
			var index = IndexOf<TColumn>();
			if (index == -1)
			{
				Add(column);
			}
			else
			{
				Insert(index + 1, column);
			}
		}

		/// <summary>
		/// Inserts a column before another.
		/// </summary>
		/// <param name="column"> The column to insert. </param>
		/// <typeparam name="TColumn"> The type of column to insert before. </typeparam>
		public void InsertBefore<TColumn>(Column column)
			where TColumn : Column
		{
			var index = IndexOf<TColumn>();
			if (index == -1)
			{
				Insert(0, column);
			}
			else
			{
				Insert(index, column);
			}
		}

		/// <inheritdoc />
		protected override void ClearItems()
		{
			foreach (var item in Items)
			{
				item.HexView = null;
				_owner.LogicalChildren.Remove(item);
			}

			base.ClearItems();
		}

		/// <inheritdoc />
		protected override void InsertItem(int index, Column item)
		{
			AssertNoOwner(item);
			base.InsertItem(index, item);
			item.HexView = _owner;
			_owner.LogicalChildren.Insert(index, item);
		}

		/// <inheritdoc />
		protected override void RemoveItem(int index)
		{
			Items[index].HexView = null;
			base.RemoveItem(index);
			_owner.LogicalChildren.RemoveAt(index);
		}

		/// <inheritdoc />
		protected override void SetItem(int index, Column item)
		{
			AssertNoOwner(item);

			Items[index].HexView = null;
			base.SetItem(index, item);
			item.HexView = _owner;
			_owner.LogicalChildren[index] = item;
		}

		private static void AssertNoOwner(Column column)
		{
			if (column.HexView is not null)
			{
				throw new ArgumentException("Column is already added to another hex view.");
			}
		}

		#endregion

		#region Structures

		/// <summary>
		/// Represents a column enumerator that enumerates all columns in a hex view from a left-to-right order.
		/// </summary>
		public struct Enumerator : IEnumerator<Column>
		{
			#region Fields

			private readonly ColumnCollection _collection;
			private int _index = -1;

			#endregion

			#region Constructors

			internal Enumerator(ColumnCollection collection)
			{
				_collection = collection;
			}

			#endregion

			#region Properties

			/// <inheritdoc />
			public Column Current => _collection[_index];

			object IEnumerator.Current => Current;

			#endregion

			#region Methods

			/// <inheritdoc />
			public void Dispose()
			{
			}

			/// <inheritdoc />
			public bool MoveNext()
			{
				_index++;
				return _index < _collection.Count;
			}

			/// <inheritdoc />
			public void Reset()
			{
				_index = 0;
			}

			#endregion
		}

		#endregion
	}

	/// <summary>
	/// Represents a collection of layers in a hex view.
	/// </summary>
	public sealed class LayerCollection : ObservableCollection<Layer>
	{
		#region Fields

		private readonly HexView _owner;

		#endregion

		#region Constructors

		internal LayerCollection(HexView owner)
		{
			_owner = owner;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets a single layer by its type.
		/// </summary>
		/// <typeparam name="TLayer"> The layer type. </typeparam>
		/// <returns> The layer. </returns>
		public TLayer Get<TLayer>()
			where TLayer : Layer
		{
			return Items.OfType<TLayer>().First();
		}

		/// <summary>
		/// Attempts to find a single layer by its type.
		/// </summary>
		/// <typeparam name="TLayer"> The layer type. </typeparam>
		/// <returns> The layer, or <c> null </c> if no layer of the provided type exists in the collection. </returns>
		public TLayer GetOrDefault<TLayer>()
			where TLayer : Layer
		{
			return Items.OfType<TLayer>().FirstOrDefault();
		}

		/// <summary>
		/// Gets the index of a specific layer.
		/// </summary>
		/// <typeparam name="TLayer"> The type of the layer. </typeparam>
		/// <returns> The index, or <c> -1 </c> if the layer is not present in the collection. </returns>
		public int IndexOf<TLayer>()
			where TLayer : Layer
		{
			for (var i = 0; i < Count; i++)
			{
				if (Items[i] is TLayer)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inserts a layer after another.
		/// </summary>
		/// <param name="layer"> The layer to insert. </param>
		/// <typeparam name="TLayer"> The type of layer to insert after. </typeparam>
		public void InsertAfter<TLayer>(Layer layer)
			where TLayer : Layer
		{
			var index = IndexOf<TLayer>();
			if (index == -1)
			{
				Add(layer);
			}
			else
			{
				Insert(index + 1, layer);
			}
		}

		/// <summary>
		/// Inserts a layer before another.
		/// </summary>
		/// <param name="layer"> The layer to insert. </param>
		/// <typeparam name="TLayer"> The type of layer to insert before. </typeparam>
		public void InsertBefore<TLayer>(Layer layer)
			where TLayer : Layer
		{
			var index = IndexOf<TLayer>();
			if (index == -1)
			{
				Insert(0, layer);
			}
			else
			{
				Insert(index, layer);
			}
		}

		/// <inheritdoc />
		protected override void ClearItems()
		{
			foreach (var item in Items)
			{
				item.HexView = null;
				_owner.LogicalChildren.Remove(item);
				_owner.VisualChildren.Remove(item);
			}

			base.ClearItems();
		}

		/// <inheritdoc />
		protected override void InsertItem(int index, Layer item)
		{
			AssertNoOwner(item);
			item.HexView = _owner;
			_owner.LogicalChildren.Insert(index + _owner.Columns.Count, item);
			_owner.VisualChildren.Insert(index, item);
			base.InsertItem(index, item);
		}

		/// <inheritdoc />
		protected override void RemoveItem(int index)
		{
			var item = Items[index];

			item.HexView = null;
			_owner.LogicalChildren.Remove(item);
			_owner.VisualChildren.Remove(item);

			base.RemoveItem(index);
		}

		/// <inheritdoc />
		protected override void SetItem(int index, Layer item)
		{
			Items[index].HexView = null;
			item.HexView = _owner;
			base.SetItem(index, item);

			_owner.LogicalChildren[index + _owner.Columns.Count] = item;
			_owner.VisualChildren[index] = item;
		}

		private static void AssertNoOwner(Layer item)
		{
			if (item.HexView is not null)
			{
				throw new InvalidOperationException("Layer is already added to another hex view.");
			}
		}

		#endregion
	}

	#endregion
}