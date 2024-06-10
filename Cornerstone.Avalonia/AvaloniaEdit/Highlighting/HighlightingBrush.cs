#region References

using Avalonia.Media;
using Avalonia.Media.Immutable;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting;

/// <summary>
/// A brush used for syntax highlighting. Can retrieve a real brush on-demand.
/// </summary>
public abstract class HighlightingBrush
{
	#region Methods

	/// <summary>
	/// Gets the real brush.
	/// </summary>
	/// <param name="context"> The construction context. context can be null! </param>
	public abstract IBrush GetBrush(ITextRunConstructionContext context);

	/// <summary>
	/// Gets the color of the brush.
	/// </summary>
	/// <param name="context"> The construction context. context can be null! </param>
	public virtual Color? GetColor(ITextRunConstructionContext context)
	{
		if (GetBrush(context) is ISolidColorBrush scb)
		{
			return scb.Color;
		}
		return null;
	}

	#endregion
}

/// <summary>
/// Highlighting brush implementation that takes a frozen brush.
/// </summary>
public sealed class SimpleHighlightingBrush : HighlightingBrush
{
	#region Fields

	private readonly ISolidColorBrush _brush;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new HighlightingBrush with the specified color.
	/// </summary>
	public SimpleHighlightingBrush(Color color) : this(new ImmutableSolidColorBrush(color))
	{
	}

	internal SimpleHighlightingBrush(ISolidColorBrush brush)
	{
		_brush = brush;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		var other = obj as SimpleHighlightingBrush;
		if (other == null)
		{
			return false;
		}
		return _brush.Color.Equals(other._brush.Color);
	}

	/// <inheritdoc />
	public override IBrush GetBrush(ITextRunConstructionContext context)
	{
		return _brush;
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return _brush.Color.GetHashCode();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return _brush.ToString();
	}

	#endregion
}