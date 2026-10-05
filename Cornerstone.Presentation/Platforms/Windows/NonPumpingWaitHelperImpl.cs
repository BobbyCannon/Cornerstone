using System;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Platforms.Windows.Interop;

namespace Cornerstone.Presentation.Platforms.Windows;

internal class NonPumpingWaitHelperImpl : NonPumpingLockHelper.IHelperImpl
{
    public static NonPumpingWaitHelperImpl Instance { get; } = new();
    public int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) =>
        UnmanagedMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll,
            millisecondsTimeout, false);
}