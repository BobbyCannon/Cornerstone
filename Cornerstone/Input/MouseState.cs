#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Input;

[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class MouseState : CornerstoneObject<MouseState>
{
	#region Properties

	public DateTime DateTime { get; set; }

	public MouseEvent Event { get; set; }

	public bool LeftButton { get; set; }

	public bool LeftButtonDoubleClick { get; set; }

	public bool MiddleButton { get; set; }

	public bool MiddleButtonDoubleClick { get; set; }

	public bool RightButton { get; set; }

	public bool RightButtonDoubleClick { get; set; }

	public int WheelHorizontalDelta { get; set; }

	public bool WheelScrollingDown => WheelVerticalDelta < 0;

	public bool WheelScrollingLeft => WheelHorizontalDelta < 0;

	public bool WheelScrollingRight => WheelHorizontalDelta > 0;

	public bool WheelScrollingUp => WheelVerticalDelta > 0;

	public int WheelVerticalDelta { get; set; }

	public int X { get; set; }

	public bool XButton1 { get; set; }

	public bool XButton1DoubleClick { get; set; }

	public bool XButton2 { get; set; }

	public bool XButton2DoubleClick { get; set; }

	public int Y { get; set; }

	#endregion

	#region Methods

	public bool IsButtonDown(MouseButton button)
	{
		return button switch
		{
			MouseButton.LeftButton => LeftButton,
			MouseButton.MiddleButton => MiddleButton,
			MouseButton.RightButton => RightButton,
			MouseButton.XButton1 => XButton1,
			MouseButton.XButton2 => XButton2,
			_ => false
		};
	}

	public bool IsButtonUp(MouseButton button)
	{
		return !IsButtonDown(button);
	}

	#endregion
}