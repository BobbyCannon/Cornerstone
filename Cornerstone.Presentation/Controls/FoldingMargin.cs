#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Text.Folding;
using Cornerstone.Presentation.Controls.Text;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Gutter between other left margins and the text. Draws the divider line and fold markers.
/// Markers use the same Icons.Angle.Right toggle as TreeDataGridExpandCollapseChevron.
/// </summary>
public class FoldingMargin<T> : Margin
	where T : TextEditorViewModel, new()
{
	#region Fields

	private const double MutedOpacity = 0.4;
	private static StreamGeometry _angleRight;
	private readonly TextEditor<T> _editor;
	private FoldingSection _hoveredSection;

	#endregion

	#region Constructors

	public FoldingMargin(TextEditor<T> editor)
	{
		_editor = editor;
		ClipToBounds = false;
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext drawingContext)
	{
		drawingContext.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

		var dividerBrush = _editor.BorderBrush ?? Brushes.Gray;
		var pen = new Pen(dividerBrush, 1);
		var renderer = _editor.Renderer;
		var folding = _editor.ViewModel?.FoldingManager;
		var geometry = (renderer != null) && (folding != null) && (folding.AllFoldings.Count > 0) ? GetAngleRightGeometry() : null;
		var lineHeight = _editor.ViewModel?.ViewMetrics.CharacterHeight ?? 12;
		var size = lineHeight * 0.7;
		var slot = GetFoldSlot(geometry, size);
		var x = slot.LineX;
		var lineTop = -_editor.Padding.Top;
		var lineBottom = Bounds.Height + _editor.Padding.Bottom;
		var lineStart = lineTop;
		var topY = renderer?.Offset.Y ?? 0;
		var markers = new List<(double CenterY, bool Expanded, FoldingSection Section)>();

		if ((renderer != null) && (folding != null) && (geometry != null))
		{
			foreach (var line in renderer.GetVisualLines())
			{
				var section = folding.GetFoldingStartingOnLine(line);
				if (section == null)
				{
					continue;
				}

				var centerY = (line.VisualLayout.Top - topY) + (_editor.ViewModel.ViewMetrics.CharacterHeight / 2);
				markers.Add((centerY, !section.IsFolded, section));

				var gapTop = line.VisualLayout.Top - topY;
				var gapBottom = gapTop + line.VisualLayout.Height;
				if (gapTop > lineStart)
				{
					drawingContext.DrawLine(pen, new Point(x, lineStart), new Point(x, gapTop));
				}

				lineStart = Math.Max(lineStart, gapBottom);
			}
		}

		if (lineStart < lineBottom)
		{
			drawingContext.DrawLine(pen, new Point(x, lineStart), new Point(x, lineBottom));
		}

		if (geometry == null)
		{
			return;
		}

		var fill = _editor.Foreground ?? dividerBrush;
		var centerX = slot.CenterX;
		var hoverPen = new Pen(fill, 1);

		foreach (var marker in markers)
		{
			var hovered = ReferenceEquals(marker.Section, _hoveredSection);
			if (hovered)
			{
				DrawFoldRangeLine(drawingContext, hoverPen, x, marker.Section, topY);
				DrawToggleArrow(drawingContext, geometry, fill, centerX, marker.CenterY, size, marker.Expanded);
				continue;
			}

			using (drawingContext.PushOpacity(MutedOpacity))
			{
				DrawToggleArrow(drawingContext, geometry, fill, centerX, marker.CenterY, size, marker.Expanded);
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var metrics = _editor.ViewModel?.ViewMetrics;
		var characterHeight = metrics?.CharacterHeight ?? _editor.FontSize;
		var arrowSize = (double.IsFinite(characterHeight) ? characterHeight : 12) * 0.7;
		var geometry = _editor.ViewModel?.FoldingManager.AllFoldings.Count > 0 ? GetAngleRightGeometry() : null;
		var width = GetFoldSlot(geometry, arrowSize).Width;

		var height = availableSize.Height;
		if (!double.IsFinite(height))
		{
			height = metrics?.DocumentSize.Height ?? 0;
			if ((height <= 0) && (metrics != null) && double.IsFinite(metrics.CharacterHeight))
			{
				height = metrics.CharacterHeight;
			}
		}

		if (!double.IsFinite(height) || (height < 0))
		{
			height = 0;
		}

		return new Size(width, height);
	}

	protected override void OnPointerExited(PointerEventArgs e)
	{
		SetHoveredSection(null);
		base.OnPointerExited(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		SetHoveredSection(GetFoldingAt(e.GetPosition(this)));
		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			base.OnPointerPressed(e);
			return;
		}

		var section = GetFoldingAt(e.GetPosition(this));
		if (section == null)
		{
			base.OnPointerPressed(e);
			return;
		}

		section.IsFolded = !section.IsFolded;
		e.Handled = true;
	}

	private FoldingSection GetFoldingAt(Point localPoint)
	{
		var folding = _editor.ViewModel?.FoldingManager;
		var renderer = _editor.Renderer;
		if ((folding == null) || (renderer == null) || (_editor.ViewModel == null))
		{
			return null;
		}

		var documentY = localPoint.Y + renderer.Offset.Y;
		if (!_editor.ViewModel.Lines.TryGetLineForOffset(0, documentY, out var line) || (line.VisualLayout.Height <= 0))
		{
			return null;
		}

		return folding.GetFoldingStartingOnLine(line);
	}

	private void SetHoveredSection(FoldingSection section)
	{
		if (ReferenceEquals(_hoveredSection, section))
		{
			return;
		}

		_hoveredSection = section;
		InvalidateVisual();
	}

	private void DrawFoldRangeLine(DrawingContext drawingContext, Pen pen, double x, FoldingSection section, double topY)
	{
		var lines = _editor.ViewModel?.Lines;
		if ((lines == null) || (section == null))
		{
			return;
		}

		if (!lines.TryGetLineForOffset(section.StartOffset, out var startLine))
		{
			return;
		}

		var endOffset = section.EndOffset > section.StartOffset
			? section.EndOffset - 1
			: section.StartOffset;
		if (!lines.TryGetLineForOffset(endOffset, out var endLine))
		{
			endLine = startLine;
		}

		var startY = startLine.VisualLayout.Top - topY + startLine.VisualLayout.Height;
		var endY = endLine.VisualLayout.Top - topY + endLine.VisualLayout.Height;
		if (endY <= startY)
		{
			return;
		}

		drawingContext.DrawLine(pen, new Point(x, startY), new Point(x, Math.Min(endY, Bounds.Height)));
	}

	private static void DrawToggleArrow(
		DrawingContext drawingContext,
		StreamGeometry geometry,
		IBrush fill,
		double centerX,
		double centerY,
		double size,
		bool expanded)
	{
		var bounds = geometry.Bounds;
		if ((bounds.Width <= 0) || (bounds.Height <= 0))
		{
			return;
		}

		var scale = Math.Min(size / bounds.Width, size / bounds.Height);
		var originX = bounds.X + (bounds.Width / 2);
		var originY = bounds.Y + (bounds.Height / 2);
		var rotation = expanded ? Math.PI / 2 : 0;
		var transform = Matrix.CreateTranslation(-originX, -originY)
			* Matrix.CreateScale(scale, scale)
			* Matrix.CreateRotation(rotation)
			* Matrix.CreateTranslation(centerX, centerY);

		using (drawingContext.PushTransform(transform))
		{
			drawingContext.DrawGeometry(fill, null, geometry);
		}
	}

	private StreamGeometry GetAngleRightGeometry()
	{
		if (_angleRight != null)
		{
			return _angleRight;
		}

		// StyledElement.TryGetResource only checks local resources. Icons.Angle.Right lives on the application theme.
		if (_editor.TryFindResource("Icons.Angle.Right", _editor.ActualThemeVariant, out var found))
		{
			_angleRight = found as StreamGeometry;
		}

		return _angleRight;
	}

	/// <summary>
	/// Column width is the expanded chevron so a collapsed marker does not shrink the gutter.
	/// The divider and both arrow states share that column's center.
	/// </summary>
	private static (double Width, double LineX, double CenterX) GetFoldSlot(StreamGeometry geometry, double arrowSize)
	{
		if (!double.IsFinite(arrowSize) || (arrowSize <= 0))
		{
			arrowSize = 12 * 0.7;
		}

		var arrowWidth = arrowSize;
		if (geometry != null)
		{
			var bounds = geometry.Bounds;
			if ((bounds.Width > 0) && (bounds.Height > 0))
			{
				var scale = Math.Min(arrowSize / bounds.Width, arrowSize / bounds.Height);
				arrowWidth = bounds.Height * scale;
			}
		}

		var centerX = arrowWidth / 2;
		return (arrowWidth, SnapToHalfPixel(centerX), centerX);
	}

	private static double SnapToHalfPixel(double value)
	{
		return Math.Floor(value) + 0.5;
	}

	#endregion
}