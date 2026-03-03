#region References

using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;

#endregion

namespace Avalonia.Diagnostics.Controls;

public class ControlHighlightAdorner : Control
{
	#region Fields

	private static readonly Panel _layoutHighlightAdorner;

	private readonly IPen _pen;

	#endregion

	#region Constructors

	static ControlHighlightAdorner()
	{
		_layoutHighlightAdorner = new Panel
		{
			ClipToBounds = false,
			Children =
			{
				//Padding frame
				new Border { BorderBrush = new SolidColorBrush(Colors.Green, 0.5) },
				//Content frame
				new Border { Background = new SolidColorBrush(Color.FromRgb(160, 197, 232), 0.5) },
				//Margin frame
				new Border { BorderBrush = new SolidColorBrush(Colors.Yellow, 0.5) }
			}
		};
		AdornerLayer.SetIsClipEnabled(_layoutHighlightAdorner, false);
	}

	private ControlHighlightAdorner(IPen pen)
	{
		_pen = pen;
		Clip = null;
	}

	#endregion

	#region Methods

	public static IDisposable Add(InputElement owner, IBrush highlightBrush)
	{
		if (AdornerLayer.GetAdornerLayer(owner) is { } layer)
		{
			var pen = new Pen(highlightBrush, 2).ToImmutable();
			var adorner = new ControlHighlightAdorner(pen)
			{
				[AdornerLayer.AdornedElementProperty] = owner
			};
			layer.Children.Add(adorner);

			return Disposable.Create((layer, adorner), state => { state.layer.Children.Remove(state.adorner); });
		}
		return null;
	}

	public static IDisposable Add(Visual visual, bool visualizeMarginPadding)
	{
		if (AdornerLayer.GetAdornerLayer(visual) is { } layer)
		{
			if (layer.Children.Contains(_layoutHighlightAdorner))
			{
				return null;
			}
			layer.Children.Add(_layoutHighlightAdorner);
			AdornerLayer.SetAdornedElement(_layoutHighlightAdorner, visual);
			var paddingBorder = (Border) _layoutHighlightAdorner.Children[0];
			var contentBorder = (Border) _layoutHighlightAdorner.Children[1];
			var marginBorder = (Border) _layoutHighlightAdorner.Children[2];
			if (visualizeMarginPadding)
			{
				paddingBorder.BorderThickness = visual.GetValue(TemplatedControl.PaddingProperty);
				contentBorder.Margin = visual.GetValue(TemplatedControl.PaddingProperty);
				marginBorder.BorderThickness = visual.GetValue(MarginProperty);
				marginBorder.Margin = InvertThickness(visual.GetValue(MarginProperty));
			}
			else
			{
				paddingBorder.BorderThickness = default;
				contentBorder.Margin = default;
				marginBorder.BorderThickness = default;
				marginBorder.Margin = default;
			}
			return Disposable.Create((Layer: layer, Adorner: _layoutHighlightAdorner), state => { state.Layer.Children.Remove(state.Adorner); });
		}
		return null;
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		context.DrawRectangle(_pen, Bounds.Deflate(2));
	}

	private static Thickness InvertThickness(Thickness input)
	{
		return new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
	}

	#endregion
}