using System.Diagnostics;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// A row container in a <see cref="TableView"/>.
/// </summary>
[TemplatePart(PartCellsPresenter, typeof(TableViewCellsPresenter))]
public class TableViewRow : ListBoxItem
{
    private const string PartCellsPresenter = "PART_CellsPresenter";

    private TableViewCellsPresenter? _cellsPresenter;

    internal OldPresentationList<TableViewColumn>? Columns { get; set; }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_cellsPresenter is not null)
        {
            Debug.Assert(_cellsPresenter.Row == this);
            _cellsPresenter.RemoveCells();
            _cellsPresenter.Row = null;
        }

        _cellsPresenter = e.NameScope.Find<TableViewCellsPresenter>(PartCellsPresenter);

        if (_cellsPresenter is not null)
        {
            Debug.Assert(_cellsPresenter.Row is null);
            _cellsPresenter.Row = this;
            _cellsPresenter.RebuildCells();
        }
    }

    internal void ClearCells()
        => _cellsPresenter?.ClearCells();

    internal void InvalidateCellsMeasure()
        => _cellsPresenter?.InvalidateMeasure();

    internal void RebuildCells()
        => _cellsPresenter?.RebuildCells();

    internal void RefreshCell(int columnIndex)
        => _cellsPresenter?.RefreshCell(columnIndex);
}
