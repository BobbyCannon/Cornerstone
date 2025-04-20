#region References

using System;
using System.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.HexEditor.Document;
using Cornerstone.Avalonia.HexEditor.Rendering;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabHexEditor : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "HexEditor";

	#endregion

	#region Fields

	private readonly RangesHighlighter _changesHighlighter;
	private readonly ILineTransformer _invalidRangesHighlighter;

	#endregion

	#region Constructors

	public TabHexEditor()
	{
		DataContext = this;
		InitializeComponent();

		_changesHighlighter = new RangesHighlighter { Foreground = Brushes.Red };
		_invalidRangesHighlighter = new InvalidRangesHighlighter { Foreground = new SolidColorBrush(Colors.Gray, 0.5) };

		// Enable the changes highlighter.
		HexEditor.HexView.LineTransformers.Add(_changesHighlighter);
		HexEditor.HexView.LineTransformers.Add(_invalidRangesHighlighter);
		HexEditor.HexView.BytesPerLine = 16;

		// Divide each 8 bytes with a dashed line and separate colors.
		var layer = HexEditor.HexView.Layers.Get<CellGroupsLayer>();
		layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
		layer.Backgrounds.Add(null);
		layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);
		layer.BytesPerGroup = 8;

		HexEditor.DocumentChanged += HexEditorOnDocumentChanged;
		HexEditor.Selection.RangeChanged += SelectionOnRangeChanged;
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		var bytes = "Hello World"u8.ToArray();
		var document = new DynamicBinaryDocument(bytes);
		HexEditor.Document = document;
		base.OnLoaded(e);
	}

	private void DocumentOnChanged(object sender, BinaryDocumentChange change)
	{
		switch (change.Type)
		{
			case BinaryDocumentChangeType.Modify:
			{
				_changesHighlighter.Ranges.Add(change.AffectedRange);
				break;
			}
			case BinaryDocumentChangeType.Insert:
			case BinaryDocumentChangeType.Remove:
			{
				_changesHighlighter.Ranges.Add(change.AffectedRange.ExtendTo(HexEditor.Document!.ValidRanges.EnclosingRange.End));
				break;
			}
			default:
			{
				Debugger.Break();
				break;
			}
		}
	}

	private void HexEditorOnDocumentChanged(object sender, DocumentChangedEventArgs e)
	{
		_changesHighlighter.Ranges.Clear();

		if (e.Old is not null)
		{
			e.Old.Changed -= DocumentOnChanged;
		}
		if (e.New is not null)
		{
			e.New.Changed += DocumentOnChanged;
		}
	}

	private void ResetChanges(object sender, RoutedEventArgs e)
	{
		_changesHighlighter.Ranges.Clear();

		HexEditor.HexView.InvalidateArrange();
	}

	private void SelectionOnRangeChanged(object sender, EventArgs e)
	{
	}

	#endregion
}