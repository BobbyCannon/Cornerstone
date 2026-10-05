using System;
using Cornerstone.Presentation.Input;

namespace Cornerstone.Presentation.X11.Selections.DragDrop;

internal interface IXdndWindow
{
    IntPtr Handle { get; }

    IInputRoot? InputRoot { get; }

    Point PointToClient(PixelPoint point);
}
