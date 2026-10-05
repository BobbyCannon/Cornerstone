using System;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Utilities
{
    internal class NonPumpingLockHelper
    {
        public interface IHelperImpl
        {
            int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);
        }

        public static IDisposable? Use()
        {
            var impl = PresentationLocator.Current.GetService<IHelperImpl>();
            if (impl == null)
                return null;
            return NonPumpingSyncContext.Use(impl);
        }
    }
}
