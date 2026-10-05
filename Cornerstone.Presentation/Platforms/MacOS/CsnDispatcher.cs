using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Threading;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.MacOS;

class CsnDispatcher : NativeCallbackBase, ICsnDispatcher
{
    public void Post(ICsnActionCallback cb)
    {
        var callback = cb.CloneReference();
        Dispatcher.UIThread.Post(() =>
        {
            using (callback)
                callback.Run();
        }, DispatcherPriority.Send);
    }
}
