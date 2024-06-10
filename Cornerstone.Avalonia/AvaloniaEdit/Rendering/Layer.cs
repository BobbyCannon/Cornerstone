#region References

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using PropertyChanged;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// Base class for known layers.
/// </summary>
[DoNotNotify]
internal class Layer : Control
{
	#region Fields

	protected readonly KnownLayer KnownLayer;
	protected readonly TextView TextView;

	#endregion

	#region Constructors

	public Layer(TextView textView, KnownLayer knownLayer)
	{
		Debug.Assert(textView != null);
		TextView = textView;
		KnownLayer = knownLayer;
		Focusable = false;
		IsHitTestVisible = false;
	}

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		TextView.RenderBackground(context, KnownLayer);
	}

	#endregion
}