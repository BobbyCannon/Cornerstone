#region References

using System;
using System.Linq;
using Avalonia.Media;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Search;

internal class SearchResultBackgroundRenderer : IBackgroundRenderer
{
	#region Constructors

	public SearchResultBackgroundRenderer(IBrush brush)
	{
		MarkerBrush = brush;
	}

	#endregion

	#region Properties

	public TextSegmentCollection<SearchResult> CurrentResults { get; } = [];

	public KnownLayer Layer => KnownLayer.Background;

	public IBrush MarkerBrush { get; set; }

	#endregion

	#region Methods

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		if (textView == null)
		{
			throw new ArgumentNullException(nameof(textView));
		}
		if (drawingContext == null)
		{
			throw new ArgumentNullException(nameof(drawingContext));
		}

		if ((CurrentResults == null) || !textView.VisualLinesValid)
		{
			return;
		}

		var visualLines = textView.VisualLines;
		if (visualLines.Count == 0)
		{
			return;
		}

		var viewStart = visualLines.First().FirstDocumentLine.Offset;
		var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

		foreach (var result in CurrentResults.FindOverlappingSegments(viewStart, viewEnd - viewStart))
		{
			var geoBuilder = new BackgroundGeometryBuilder
			{
				AlignToWholePixels = true,
				CornerRadius = 0
			};
			geoBuilder.AddSegment(textView, result);
			var geometry = geoBuilder.CreateGeometry();
			if (geometry != null)
			{
				drawingContext.DrawGeometry(MarkerBrush, null, geometry);
			}
		}
	}

	#endregion
}