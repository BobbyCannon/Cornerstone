#region References

using Avalonia.Controls;
using Avalonia.Media;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Represents a single layer in the hex view rendering.
/// </summary>
[DoNotNotify]
public abstract class Layer : Control
{
	#region Properties

	/// <summary>
	/// Gets the parent hex view the layer is added to.
	/// </summary>
	public HexView HexView { get; internal set; }

	/// <summary>
	/// Gets a value indicating when the layer should be rendered.
	/// </summary>
	public virtual LayerRenderMoments UpdateMoments => LayerRenderMoments.Always;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		base.Render(context);
	}

	#endregion
}