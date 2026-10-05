using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Browser;

public class JSObjectPlatformHandle : PlatformHandle
{
    internal const string ElementReferenceDescriptor = "JSObject";

    // GetHashCode returns internal JSHandle.
    internal JSObjectPlatformHandle(JSObject reference) : base(reference.GetHashCode(), ElementReferenceDescriptor)
    {
        Object = reference;
    }

    public JSObject Object { get; }
} 

public class JSObjectControlHandle : JSObjectPlatformHandle, INativeControlHostDestroyableControlHandle
{
    public JSObjectControlHandle(JSObject reference) : base(reference)
    {
    }

    public void Destroy()
    {
        if (Object is { } inProcess && !inProcess.IsDisposed)
        {
            inProcess.Dispose();
        }
    }
}
