#region References

using Avalonia;
using Avalonia.Controls.Primitives;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

[DoNotNotify]
internal class PopupWithCustomPosition : Popup
{
	#region Properties

	public Point Offset
	{
		get => new(HorizontalOffset, VerticalOffset);
		set
		{
			HorizontalOffset = value.X;
			VerticalOffset = value.Y;

			//this.Revalidate(VerticalOffsetProperty);
		}
	}

	#endregion
}