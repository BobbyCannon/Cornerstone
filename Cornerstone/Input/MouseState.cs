#region References

using System;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Input;

public class MouseState : Notifiable<MouseState>
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

	/// <summary>
	/// Update the MouseState with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(MouseState update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			DateTime = update.DateTime;
			Event = update.Event;
			LeftButton = update.LeftButton;
			LeftButtonDoubleClick = update.LeftButtonDoubleClick;
			MiddleButton = update.MiddleButton;
			MiddleButtonDoubleClick = update.MiddleButtonDoubleClick;
			RightButton = update.RightButton;
			RightButtonDoubleClick = update.RightButtonDoubleClick;
			WheelHorizontalDelta = update.WheelHorizontalDelta;
			WheelVerticalDelta = update.WheelVerticalDelta;
			X = update.X;
			XButton1 = update.XButton1;
			XButton1DoubleClick = update.XButton1DoubleClick;
			XButton2 = update.XButton2;
			XButton2DoubleClick = update.XButton2DoubleClick;
			Y = update.Y;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(DateTime)), x => x.DateTime = update.DateTime);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Event)), x => x.Event = update.Event);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(LeftButton)), x => x.LeftButton = update.LeftButton);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(LeftButtonDoubleClick)), x => x.LeftButtonDoubleClick = update.LeftButtonDoubleClick);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MiddleButton)), x => x.MiddleButton = update.MiddleButton);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MiddleButtonDoubleClick)), x => x.MiddleButtonDoubleClick = update.MiddleButtonDoubleClick);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(RightButton)), x => x.RightButton = update.RightButton);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(RightButtonDoubleClick)), x => x.RightButtonDoubleClick = update.RightButtonDoubleClick);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(WheelHorizontalDelta)), x => x.WheelHorizontalDelta = update.WheelHorizontalDelta);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(WheelVerticalDelta)), x => x.WheelVerticalDelta = update.WheelVerticalDelta);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(X)), x => x.X = update.X);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(XButton1)), x => x.XButton1 = update.XButton1);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(XButton1DoubleClick)), x => x.XButton1DoubleClick = update.XButton1DoubleClick);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(XButton2)), x => x.XButton2 = update.XButton2);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(XButton2DoubleClick)), x => x.XButton2DoubleClick = update.XButton2DoubleClick);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Y)), x => x.Y = update.Y);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			MouseState value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}