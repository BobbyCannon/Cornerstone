using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.MessageTypes.Outgoing;

namespace Cornerstone.RemoteLink.Vnc.Controls
{
    public partial class VncView
    {
        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandlePointerEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandlePointerEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandlePointerEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandlePointerEvent(point, e.Delta))
                e.Handled = true;
        }

        private bool HandlePointerEvent(PointerPoint pointerPoint, Vector wheelDelta)
        {
            RfbConnection? connection = Connection;
            if (connection == null)
                return false;

            if (!TryMapViewPointToFramebuffer(pointerPoint.Position, out Position position))
                return false;

            MouseButtons buttonsMask = GetButtonsMask(pointerPoint.Properties);
            MouseButtons wheelMask = GetWheelMask(wheelDelta);

            // For scrolling, set the wheel buttons and remove them quickly after that.
            if (wheelMask != MouseButtons.None)
                connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask | wheelMask));
            connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask));

            return true;
        }

        private MouseButtons GetButtonsMask(PointerPointProperties pointProperties)
        {
            var mask = MouseButtons.None;

            if (pointProperties.IsLeftButtonPressed)
                mask |= MouseButtons.Left;
            if (pointProperties.IsMiddleButtonPressed)
                mask |= MouseButtons.Middle;
            if (pointProperties.IsRightButtonPressed)
                mask |= MouseButtons.Right;

            return mask;
        }

        private MouseButtons GetWheelMask(Vector wheelDelta)
        {
            var mask = MouseButtons.None;

            if (wheelDelta.X > 0)
                mask |= MouseButtons.WheelRight;
            else if (wheelDelta.X < 0)
                mask |= MouseButtons.WheelLeft;

            if (wheelDelta.Y > 0)
                mask |= MouseButtons.WheelUp;
            else if (wheelDelta.Y < 0)
                mask |= MouseButtons.WheelDown;

            return mask;
        }
    }
}
