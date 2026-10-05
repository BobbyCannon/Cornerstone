using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal abstract class NativeCallbackBase : CallbackBase, IMicroComExceptionCallback
    {
        public void RaiseException(Exception e)
        {
            if(Dispatcher.FromThread(Thread.CurrentThread) is { PlatformImpl: DispatcherImpl dispatcherImpl })
            {
                dispatcherImpl.PropagateCallbackException(ExceptionDispatchInfo.Capture(e));
            }
        }
    }
}
