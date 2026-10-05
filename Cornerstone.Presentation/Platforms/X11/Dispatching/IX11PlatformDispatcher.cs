using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.X11.Dispatching;

interface IX11PlatformDispatcher : IDispatcherImpl
{
    X11EventDispatcher EventDispatcher { get; }
}