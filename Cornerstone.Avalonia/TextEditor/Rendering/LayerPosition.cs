#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// An enumeration of well-known layers.
/// </summary>
public enum KnownLayer
{
	/// <summary>
	/// This layer is in the background.
	/// There is no UIElement to represent this layer, it is directly drawn in the TextView.
	/// It is not possible to replace the background layer or insert new layers below it.
	/// </summary>
	/// <remarks> This layer is below the Selection layer. </remarks>
	Background,

	/// <summary>
	/// This layer contains the selection rectangle.
	/// </summary>
	/// <remarks> This layer is between the Background and the Text layers. </remarks>
	Selection,

	/// <summary>
	/// This layer contains the text and inline UI elements.
	/// </summary>
	/// <remarks> This layer is between the Selection and the Caret layers. </remarks>
	Text,

	/// <summary>
	/// This layer contains the blinking caret.
	/// </summary>
	/// <remarks> This layer is above the Text layer. </remarks>
	Caret
}

/// <summary>
/// Specifies where a new layer is inserted, in relation to an old layer.
/// </summary>
public enum LayerInsertionPosition
{
	/// <summary>
	/// The new layer is inserted below the specified layer.
	/// </summary>
	Below,

	/// <summary>
	/// The new layer replaces the specified layer. The old layer is removed
	/// from the <see cref="TextView.Layers" /> collection.
	/// </summary>
	Replace,

	/// <summary>
	/// The new layer is inserted above the specified layer.
	/// </summary>
	Above
}

internal sealed class LayerPosition : IComparable<LayerPosition>
{
	#region Fields

	internal readonly KnownLayer KnownLayer;

	internal static readonly AttachedProperty<LayerPosition> LayerPositionProperty =
		AvaloniaProperty.RegisterAttached<LayerPosition, Control, LayerPosition>("LayerPosition");

	internal readonly LayerInsertionPosition Position;

	#endregion

	#region Constructors

	public LayerPosition(KnownLayer knownLayer, LayerInsertionPosition position)
	{
		KnownLayer = knownLayer;
		Position = position;
	}

	#endregion

	#region Methods

	[SuppressMessage("ReSharper", "ImpureMethodCallOnReadonlyValueField")]
	public int CompareTo(LayerPosition other)
	{
		var r = KnownLayer.CompareTo(other.KnownLayer);
		return r != 0 ? r : Position.CompareTo(other.Position);
	}

	public static LayerPosition GetLayerPosition(Control layer)
	{
		return layer.GetValue(LayerPositionProperty);
	}

	public static void SetLayerPosition(Control layer, LayerPosition value)
	{
		layer.SetValue(LayerPositionProperty, value);
	}

	#endregion
}