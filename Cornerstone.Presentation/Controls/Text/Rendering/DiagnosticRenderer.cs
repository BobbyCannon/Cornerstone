#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Rendering;

internal class DiagnosticRenderer : IRenderer
{
	#region Fields

	private static readonly ImmutableSolidColorBrush ErrorBrush = new(Color.FromRgb(0xE5, 0x14, 0x00));
	private static readonly ImmutableSolidColorBrush WarningBrush = new(Color.FromRgb(0xCA, 0x51, 0x00));
	private readonly Pen _errorPen;
	private readonly Pen _warningPen;
	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public DiagnosticRenderer(TextRenderer textRenderer)
	{
		_renderer = textRenderer;
		_errorPen = new Pen(ErrorBrush, 1.2);
		_warningPen = new Pen(WarningBrush, 1.2);
	}

	#endregion

	#region Methods

	public void Draw(TextRenderer renderer, DrawingContext drawingContext)
	{
		var vm = _renderer.ViewModel;
		var diagnostics = vm?.Diagnostics.Items;
		if ((diagnostics == null) || (diagnostics.Count == 0))
		{
			return;
		}

		var offset = _renderer.Offset;
		var topY = offset.Y;
		var bottomY = offset.Y + _renderer.Bounds.Height;

		for (var i = 0; i < diagnostics.Count; i++)
		{
			try
			{
				var diagnostic = diagnostics[i];
				var start = diagnostic.StartOffset;
				var end = diagnostic.EndOffset;
				if (end <= start)
				{
					end = start + 1;
				}

				var pen = diagnostic.IsError ? _errorPen : _warningPen;
				var line = vm.Lines.GetLineFromOffset(start);
				if (line == null)
				{
					continue;
				}

				foreach (var documentRect in line.GetSelectionRects(start, end))
				{
					if ((documentRect.Bottom < topY) || (documentRect.Y > bottomY))
					{
						continue;
					}

					DrawWave(
						drawingContext,
						pen,
						documentRect.X - offset.X,
						documentRect.Bottom - offset.Y - 1.5,
						documentRect.Width);
				}
			}
			catch
			{
				// Stale diagnostic spans must not abort paint.
			}
		}
	}

	private static void DrawWave(DrawingContext context, Pen pen, double x, double y, double width)
	{
		if (width <= 0)
		{
			return;
		}

		const double step = 3;
		const double amplitude = 1.6;
		var xEnd = x + width;
		var up = true;
		var current = x;
		var y0 = y;
		while (current < xEnd)
		{
			var next = Math.Min(current + step, xEnd);
			var y1 = up ? y - amplitude : y + amplitude;
			context.DrawLine(pen, new Point(current, y0), new Point(next, y1));
			current = next;
			y0 = y1;
			up = !up;
		}
	}

	#endregion
}
