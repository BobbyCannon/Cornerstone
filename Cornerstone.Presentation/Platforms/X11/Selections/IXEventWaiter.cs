using System;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.X11.Selections;

internal interface IXEventWaiter : IDisposable
{
    Task<XEvent?> WaitForEventAsync(Func<XEvent, bool> predicate, TimeSpan timeout);
}
