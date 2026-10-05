#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls;

public partial class CaretVisual : Control
{
	#region Fields

	private static readonly IBrush ExtraCaretBrush = new SolidColorBrush(Colors.Blue);
	private static readonly IBrush ExtraOverstrikeBrush = new SolidColorBrush(Color.FromArgb(100, Colors.Blue.R, Colors.Blue.G, Colors.Blue.B));
	private static readonly IBrush PrimaryCaretBrush = new SolidColorBrush(Colors.Red);
	private static readonly IBrush PrimaryOverstrikeBrush = new SolidColorBrush(Color.FromArgb(100, Colors.Red.R, Colors.Red.G, Colors.Red.B));
	private readonly TextRenderer _renderer;

	#endregion

	#region Constructors

	public CaretVisual(TextRenderer renderer)
	{
		_renderer = renderer;
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		var viewModel = _renderer.ViewModel;
		if ((viewModel == null) || !viewModel.ShowCaret)
		{
			base.Render(context);
			return;
		}

		var primary = viewModel.Caret;
		if (primary == null)
		{
			base.Render(context);
			return;
		}

		var overstrike = primary.OverstrikeMode;
		var caretWidth = overstrike ? viewModel.ViewMetrics.CharacterWidth : 1;
		var multiCaret = viewModel.Carets.Count > 1;
		var primaryVisible = primary.IsVisible;
		foreach (var caret in viewModel.Carets.All)
		{
			var isPrimary = caret == primary;
			if (isPrimary)
			{
				if (caret.IsDrawn != true)
				{
					continue;
				}
			}
			else if (!primaryVisible)
			{
				continue;
			}

			var line = caret.Line;
			if (line == null)
			{
				continue;
			}

			var caretRect = caret.VisualLayout;
			var renderX = caretRect.X - _renderer.Offset.X;
			var renderY = caretRect.Y - _renderer.Offset.Y;
			var finalRect = new Rect(renderX, renderY, caretWidth, caret.VisualLayout.Height);
			var brush = GetCaretBrush(isPrimary, multiCaret, overstrike, _renderer.Foreground);
			context.FillRectangle(brush, finalRect);
		}

		base.Render(context);
	}

	/// <summary>
	/// Single caret uses the editor foreground. Several carets: primary red, extras blue.
	/// </summary>
	internal static IBrush GetCaretBrush(bool isPrimary, bool multiCaret, bool overstrike, IBrush foreground)
	{
		if (multiCaret)
		{
			if (isPrimary)
			{
				return overstrike ? PrimaryOverstrikeBrush : PrimaryCaretBrush;
			}

			return overstrike ? ExtraOverstrikeBrush : ExtraCaretBrush;
		}

		if (foreground is not ISolidColorBrush solid)
		{
			return foreground;
		}

		if (!overstrike)
		{
			return foreground;
		}

		return new SolidColorBrush(Color.FromArgb(100, solid.Color.R, solid.Color.G, solid.Color.B));
	}

	#endregion
}
