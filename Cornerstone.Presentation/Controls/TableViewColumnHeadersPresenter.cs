using System.ComponentModel;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using static Cornerstone.Presentation.Controls.Presenters.TableViewLayoutHelper;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Displays the column headers for a <see cref="TableView"/>.
/// Computes column widths (pixel and star) and exposes them via
/// <see cref="TableViewColumn.ActualWidth"/> so that <see cref="TableViewCellsPresenter"/>
/// can align cells with the headers.
/// </summary>
public class TableViewColumnHeadersPresenter : Panel
{
    internal TableView? TableView { get; set; }

    internal void RebuildHeaders()
    {
        Children.Clear();

        if (TableView is null)
            return;

        foreach (var column in TableView.Columns)
        {
            var header = new TableViewColumnHeader { Column = column };
            Children.Add(header);
        }

        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        TableView = this.FindLogicalAncestorOfType<TableView>();

        RebuildHeaders();

        if (TableView is not null)
        {
            TableView.HeadersPresenter = this;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged += OnScrollPropertyChanged;
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (TableView is not null)
        {
            TableView.HeadersPresenter = null;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged -= OnScrollPropertyChanged;

            TableView = null;
            RebuildHeaders();
        }

        base.OnDetachedFromLogicalTree(e);
    }

    private void OnScrollPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IScrollable.Offset))
            InvalidateArrange();
    }

    internal void RefreshHeader(int columnIndex)
        => ((TableViewColumnHeader)Children[columnIndex]).Refresh();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is null)
            return default;

        if (UpdateActualWidths(TableView.Columns, availableSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this)))
            TableView.InvalidateCellsMeasure();

        return MeasureRow(TableView.Columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (TableView is null)
            return finalSize;

        var columns = TableView.Columns;

        if (UpdateActualWidths(columns, finalSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this)))
            TableView.InvalidateCellsMeasure();

        var offset = TableView.Scroll?.Offset.X ?? 0;
        return ArrangeRow(columns, Children, finalSize, -offset);
    }
}
