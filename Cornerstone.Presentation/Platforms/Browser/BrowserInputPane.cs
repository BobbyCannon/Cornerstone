using System;
using System.Runtime.InteropServices.JavaScript;
using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Controls.Platform;

namespace Cornerstone.Presentation.Browser;

internal class BrowserInputPane : InputPaneBase
{
    public bool OnGeometryChange(double x, double y, double width, double height)
    {
        var oldState = (OccludedRect, State);

        OccludedRect = new Rect(x, y, width, height);
        State = OccludedRect.Width != 0 ? InputPaneState.Open : InputPaneState.Closed;

        if (oldState != (OccludedRect, State))
        {
            OnStateChanged(new InputPaneStateEventArgs(State, null, OccludedRect));
        }

        return true;
    }
}
