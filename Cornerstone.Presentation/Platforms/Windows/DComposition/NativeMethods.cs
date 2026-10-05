using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Platforms.Windows.DirectX;
using Cornerstone.Presentation.Platforms.Windows.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.DComposition;

internal class NativeMethods
{
    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateDevice2( /* _In_opt_ */
        IntPtr renderingDevice, /* _In_ */
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid, /* _Outptr_ */ out IntPtr dcompositionDevice);
}
