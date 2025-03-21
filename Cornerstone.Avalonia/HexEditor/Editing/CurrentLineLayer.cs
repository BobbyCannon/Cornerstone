#region References

using System;
using Avalonia;
using Avalonia.Media;
using Cornerstone.Avalonia.HexEditor.Rendering;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Editing;

/// <summary>
/// Renders a highlight on the current active visual line.
/// </summary>
public class CurrentLineLayer : Layer
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="CurrentLineBackground" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush> CurrentLineBackgroundProperty =
		AvaloniaProperty.Register<CurrentLineLayer, IBrush>(
			nameof(CurrentLineBackground),
			new SolidColorBrush(Colors.DimGray, 0.1)
		);

	/// <summary>
	/// Defines the <see cref="CurrentLineBorder" /> property.
	/// </summary>
	public static readonly StyledProperty<IPen> CurrentLineBorderProperty =
		AvaloniaProperty.Register<CurrentLineLayer, IPen>(
			nameof(CurrentLineBorder),
			new Pen(new SolidColorBrush(Colors.DimGray), 1.5)
		);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new current line highlighting layer.
	/// </summary>
	/// <param name="caret"> The cursor to follow. </param>
	/// <param name="selection"> The selection to follow. </param>
	public CurrentLineLayer(Caret caret, Selection selection)
	{
		Caret = caret;
		Selection = selection;

		Caret.LocationChanged += OnCursorChanged;
		Selection.RangeChanged += OnCursorChanged;
	}

	static CurrentLineLayer()
	{
		AffectsRender<CurrentLineLayer>(
			CurrentLineBackgroundProperty,
			CurrentLineBorderProperty
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the cursor the highlighter is following.
	/// </summary>
	public Caret Caret { get; }

	/// <summary>
	/// Gets or sets the brush used to draw the background of the cursor in the secondary columns.
	/// </summary>
	public IBrush CurrentLineBackground
	{
		get => GetValue(CurrentLineBackgroundProperty);
		set => SetValue(CurrentLineBackgroundProperty, value);
	}

	/// <summary>
	/// Gets or sets the brush used to draw the background of the cursor in the secondary columns.
	/// </summary>
	public IPen CurrentLineBorder
	{
		get => GetValue(CurrentLineBorderProperty);
		set => SetValue(CurrentLineBorderProperty, value);
	}

	/// <summary>
	/// Gets the selection the highlighter is following.
	/// </summary>
	public Selection Selection { get; }

	/// <inheritdoc />
	public override LayerRenderMoments UpdateMoments => LayerRenderMoments.NoResizeRearrange;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		base.Render(context);

		if (HexView is null || !HexView.IsFocused)
		{
			return;
		}

		var line = HexView.GetVisualLineByLocation(Caret.Location);
		if (line is null)
		{
			return;
		}

		if (Selection.Range.ByteLength <= 1)
		{
			context.DrawRectangle(CurrentLineBackground, CurrentLineBorder, line.Bounds);
		}
	}

	private void OnCursorChanged(object sender, EventArgs e)
	{
		InvalidateVisual();
	}

	#endregion
}